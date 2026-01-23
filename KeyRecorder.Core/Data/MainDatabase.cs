using KeyRecorder.Core.Models;

namespace KeyRecorder.Core.Data;

public class MainDatabase : BaseDatabase
{
    public MainDatabase(string basePath)
        : base(Path.Combine(basePath, DatabaseConstants.MainDatabaseFileName))
    {
    }

    public async Task InitializeAsync()
    {
        await InitializeDatabaseAsync();
    }

    public async Task BulkInsertKeystrokesAsync(IEnumerable<KeystrokeEvent> keystrokes)
    {
        if (_connection == null)
            throw new InvalidOperationException("Database not initialized");

        await using var transaction = await _connection.BeginTransactionAsync();
        try
        {
            foreach (var keystroke in keystrokes)
            {
                await InsertKeystrokeAsync(keystroke);
            }

            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<bool> PerformIntegrityCheckAsync()
    {
        if (_connection == null)
            throw new InvalidOperationException("Database not initialized");

        await using var command = _connection.CreateCommand();
        command.CommandText = "PRAGMA integrity_check;";

        var result = await command.ExecuteScalarAsync();
        return result?.ToString() == "ok";
    }

    public async Task<long> GetKeystrokeCountAsync()
    {
        if (_connection == null)
            throw new InvalidOperationException("Database not initialized");

        await using var command = _connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM KeystrokeEvents";

        var result = await command.ExecuteScalarAsync();
        return result != null ? Convert.ToInt64(result) : 0;
    }

    public async Task PruneOldKeystrokesAsync(DateTime cutoffDate)
    {
        if (_connection == null)
            throw new InvalidOperationException("Database not initialized");

        await using var command = _connection.CreateCommand();
        command.CommandText = "DELETE FROM KeystrokeEvents WHERE Timestamp < @CutoffDate";
        command.Parameters.AddWithValue("@CutoffDate", cutoffDate.ToString("O"));

        await command.ExecuteNonQueryAsync();
    }

    public async Task PruneExcessKeystrokesAsync(long maxCount)
    {
        if (_connection == null)
            throw new InvalidOperationException("Database not initialized");

        await using var command = _connection.CreateCommand();
        command.CommandText = @"
            DELETE FROM KeystrokeEvents
            WHERE Id NOT IN (
                SELECT Id FROM KeystrokeEvents
                ORDER BY Timestamp DESC
                LIMIT @MaxCount
            )";
        command.Parameters.AddWithValue("@MaxCount", maxCount);

        await command.ExecuteNonQueryAsync();
    }
}
