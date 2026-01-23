using Microsoft.Data.Sqlite;
using KeyRecorder.Core.Models;

namespace KeyRecorder.Core.Data;

public abstract class BaseDatabase : IDisposable
{
    protected SqliteConnection? _connection;
    protected readonly string _databasePath;
    private bool _disposed;

    protected BaseDatabase(string databasePath)
    {
        _databasePath = databasePath;
    }

    protected async Task InitializeDatabaseAsync()
    {
        var directory = Path.GetDirectoryName(_databasePath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = _databasePath,
            Mode = SqliteOpenMode.ReadWriteCreate,
            Cache = SqliteCacheMode.Shared
        }.ToString();

        _connection = new SqliteConnection(connectionString);
        await _connection.OpenAsync();

        await using var command = _connection.CreateCommand();
        command.CommandText = "PRAGMA journal_mode=WAL;";
        await command.ExecuteNonQueryAsync();

        command.CommandText = DatabaseConstants.CreateTableSql;
        await command.ExecuteNonQueryAsync();
    }

    public async Task<long> InsertKeystrokeAsync(KeystrokeEvent keystroke)
    {
        if (_connection == null)
            throw new InvalidOperationException("Database not initialized");

        await using var command = _connection.CreateCommand();
        command.CommandText = @"
            INSERT INTO KeystrokeEvents
            (SequenceId, Timestamp, VirtualKeyCode, KeyName, IsKeyDown,
             IsShiftPressed, IsCtrlPressed, IsAltPressed, IsWinPressed,
             ActiveWindowTitle, ActiveProcessName, IsSynced)
            VALUES
            (@SequenceId, @Timestamp, @VirtualKeyCode, @KeyName, @IsKeyDown,
             @IsShiftPressed, @IsCtrlPressed, @IsAltPressed, @IsWinPressed,
             @ActiveWindowTitle, @ActiveProcessName, @IsSynced);
            SELECT last_insert_rowid();";

        command.Parameters.AddWithValue("@SequenceId", keystroke.SequenceId);
        command.Parameters.AddWithValue("@Timestamp", keystroke.Timestamp.ToString("O"));
        command.Parameters.AddWithValue("@VirtualKeyCode", keystroke.VirtualKeyCode);
        command.Parameters.AddWithValue("@KeyName", keystroke.KeyName);
        command.Parameters.AddWithValue("@IsKeyDown", keystroke.IsKeyDown ? 1 : 0);
        command.Parameters.AddWithValue("@IsShiftPressed", keystroke.IsShiftPressed ? 1 : 0);
        command.Parameters.AddWithValue("@IsCtrlPressed", keystroke.IsCtrlPressed ? 1 : 0);
        command.Parameters.AddWithValue("@IsAltPressed", keystroke.IsAltPressed ? 1 : 0);
        command.Parameters.AddWithValue("@IsWinPressed", keystroke.IsWinPressed ? 1 : 0);
        command.Parameters.AddWithValue("@ActiveWindowTitle", (object?)keystroke.ActiveWindowTitle ?? DBNull.Value);
        command.Parameters.AddWithValue("@ActiveProcessName", (object?)keystroke.ActiveProcessName ?? DBNull.Value);
        command.Parameters.AddWithValue("@IsSynced", keystroke.IsSynced ? 1 : 0);

        var result = await command.ExecuteScalarAsync();
        return result != null ? Convert.ToInt64(result) : 0;
    }

    public async Task<List<KeystrokeEvent>> GetKeystrokesAsync(DateTime? startTime = null, DateTime? endTime = null, int limit = 1000)
    {
        if (_connection == null)
            throw new InvalidOperationException("Database not initialized");

        var keystrokes = new List<KeystrokeEvent>();
        await using var command = _connection.CreateCommand();

        var conditions = new List<string>();
        if (startTime.HasValue)
        {
            conditions.Add("Timestamp >= @StartTime");
            command.Parameters.AddWithValue("@StartTime", startTime.Value.ToString("O"));
        }
        if (endTime.HasValue)
        {
            conditions.Add("Timestamp <= @EndTime");
            command.Parameters.AddWithValue("@EndTime", endTime.Value.ToString("O"));
        }

        var whereClause = conditions.Any() ? "WHERE " + string.Join(" AND ", conditions) : "";
        command.CommandText = $@"
            SELECT * FROM KeystrokeEvents
            {whereClause}
            ORDER BY Timestamp DESC
            LIMIT @Limit";

        command.Parameters.AddWithValue("@Limit", limit);

        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            keystrokes.Add(MapReaderToKeystroke(reader));
        }

        return keystrokes;
    }

    protected KeystrokeEvent MapReaderToKeystroke(SqliteDataReader reader)
    {
        return new KeystrokeEvent
        {
            Id = reader.GetInt64(0),
            SequenceId = reader.GetInt64(1),
            Timestamp = DateTime.Parse(reader.GetString(2)),
            VirtualKeyCode = reader.GetInt32(3),
            KeyName = reader.GetString(4),
            IsKeyDown = reader.GetInt32(5) == 1,
            IsShiftPressed = reader.GetInt32(6) == 1,
            IsCtrlPressed = reader.GetInt32(7) == 1,
            IsAltPressed = reader.GetInt32(8) == 1,
            IsWinPressed = reader.GetInt32(9) == 1,
            ActiveWindowTitle = reader.IsDBNull(10) ? null : reader.GetString(10),
            ActiveProcessName = reader.IsDBNull(11) ? null : reader.GetString(11),
            IsSynced = reader.GetInt32(12) == 1
        };
    }

    public void Dispose()
    {
        if (_disposed) return;
        _connection?.Dispose();
        _disposed = true;
        GC.SuppressFinalize(this);
    }
}
