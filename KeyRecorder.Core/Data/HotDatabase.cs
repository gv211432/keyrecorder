using KeyRecorder.Core.Models;

namespace KeyRecorder.Core.Data;

public class HotDatabase : BaseDatabase
{
    public HotDatabase(string basePath)
        : base(Path.Combine(basePath, DatabaseConstants.HotDatabaseFileName))
    {
    }

    public async Task InitializeAsync()
    {
        await InitializeDatabaseAsync();
    }

    public async Task<List<KeystrokeEvent>> GetUnsyncedKeystrokesAsync()
    {
        if (_connection == null)
            throw new InvalidOperationException("Database not initialized");

        var keystrokes = new List<KeystrokeEvent>();
        await using var command = _connection.CreateCommand();
        command.CommandText = @"
            SELECT * FROM KeystrokeEvents
            WHERE IsSynced = 0
            ORDER BY SequenceId ASC";

        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            keystrokes.Add(MapReaderToKeystroke(reader));
        }

        return keystrokes;
    }

    public async Task MarkAsSyncedAsync(IEnumerable<long> keystrokeIds)
    {
        if (_connection == null)
            throw new InvalidOperationException("Database not initialized");

        await using var transaction = await _connection.BeginTransactionAsync();
        try
        {
            await using var command = _connection.CreateCommand();
            command.CommandText = "UPDATE KeystrokeEvents SET IsSynced = 1 WHERE Id = @Id";
            var parameter = command.Parameters.Add("@Id", Microsoft.Data.Sqlite.SqliteType.Integer);

            foreach (var id in keystrokeIds)
            {
                parameter.Value = id;
                await command.ExecuteNonQueryAsync();
            }

            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task PurgeSyncedKeystrokesAsync()
    {
        if (_connection == null)
            throw new InvalidOperationException("Database not initialized");

        await using var command = _connection.CreateCommand();
        command.CommandText = "DELETE FROM KeystrokeEvents WHERE IsSynced = 1";
        await command.ExecuteNonQueryAsync();
    }
}
