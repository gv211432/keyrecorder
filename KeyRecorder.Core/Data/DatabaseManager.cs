using KeyRecorder.Core.Models;
using Microsoft.Extensions.Logging;

namespace KeyRecorder.Core.Data;

public class DatabaseManager : IDisposable
{
    private readonly HotDatabase _hotDb;
    private readonly MainDatabase _mainDb;
    private readonly SnapshotDatabase _snapshotDb;
    private readonly ILogger<DatabaseManager>? _logger;
    private readonly string _basePath;
    private long _sequenceCounter;
    private bool _disposed;

    public DatabaseManager(string basePath, ILogger<DatabaseManager>? logger = null)
    {
        _basePath = basePath;
        _logger = logger;
        _hotDb = new HotDatabase(basePath);
        _mainDb = new MainDatabase(basePath);
        _snapshotDb = new SnapshotDatabase(basePath);
    }

    public async Task InitializeAsync()
    {
        await _hotDb.InitializeAsync();
        await _mainDb.InitializeAsync();
        _logger?.LogInformation("Database initialized at {Path}", _basePath);
    }

    public async Task<long> RecordKeystrokeAsync(KeystrokeEvent keystroke)
    {
        keystroke.SequenceId = Interlocked.Increment(ref _sequenceCounter);
        return await _hotDb.InsertKeystrokeAsync(keystroke);
    }

    public async Task SyncHotToMainAsync()
    {
        try
        {
            _logger?.LogInformation("Starting Hot → Main sync");

            var unsyncedKeystrokes = await _hotDb.GetUnsyncedKeystrokesAsync();

            if (unsyncedKeystrokes.Count == 0)
            {
                _logger?.LogInformation("No unsynced keystrokes to sync");
                return;
            }

            await _mainDb.BulkInsertKeystrokesAsync(unsyncedKeystrokes);

            var syncedIds = unsyncedKeystrokes.Select(k => k.Id).ToList();
            await _hotDb.MarkAsSyncedAsync(syncedIds);

            _logger?.LogInformation("Synced {Count} keystrokes to main database", unsyncedKeystrokes.Count);

            await _hotDb.PurgeSyncedKeystrokesAsync();
            _logger?.LogInformation("Purged synced keystrokes from hot database");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error during Hot → Main sync");
            throw;
        }
    }

    public async Task<bool> PerformIntegrityCheckAsync()
    {
        try
        {
            _logger?.LogInformation("Performing integrity check");
            var isValid = await _mainDb.PerformIntegrityCheckAsync();

            if (!isValid)
            {
                _logger?.LogWarning("Integrity check failed, attempting recovery");
                await AttemptRecoveryAsync();
            }

            return isValid;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error during integrity check");
            return false;
        }
    }

    private async Task AttemptRecoveryAsync()
    {
        var snapshots = await _snapshotDb.GetSnapshotsAsync();

        if (snapshots.Count == 0)
        {
            _logger?.LogWarning("No snapshots available for recovery");
            return;
        }

        var mainDbPath = Path.Combine(_basePath, DatabaseConstants.MainDatabaseFileName);

        foreach (var snapshot in snapshots)
        {
            _logger?.LogInformation("Attempting to restore from snapshot {Snapshot}", snapshot);

            if (await _snapshotDb.RestoreFromSnapshotAsync(snapshot, mainDbPath))
            {
                _logger?.LogInformation("Successfully restored from snapshot");
                await _mainDb.InitializeAsync();
                return;
            }
        }

        _logger?.LogError("Failed to restore from any snapshot");
    }

    public async Task CreateSnapshotAsync()
    {
        try
        {
            _logger?.LogInformation("Creating snapshot");
            var mainDbPath = Path.Combine(_basePath, DatabaseConstants.MainDatabaseFileName);
            await _snapshotDb.CreateSnapshotAsync(mainDbPath);
            _logger?.LogInformation("Snapshot created successfully");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error creating snapshot");
            throw;
        }
    }

    public async Task PruneSnapshotsAsync(int maxSnapshots)
    {
        try
        {
            _logger?.LogInformation("Pruning old snapshots, keeping {Max}", maxSnapshots);
            await _snapshotDb.PruneOldSnapshotsAsync(maxSnapshots);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error pruning snapshots");
        }
    }

    public async Task ApplyRetentionPolicyAsync(RetentionPolicy policy)
    {
        try
        {
            _logger?.LogInformation("Applying retention policy");

            if (policy.RetentionDays > 0)
            {
                var cutoffDate = DateTime.UtcNow.AddDays(-policy.RetentionDays);
                await _mainDb.PruneOldKeystrokesAsync(cutoffDate);
                _logger?.LogInformation("Pruned keystrokes older than {Days} days", policy.RetentionDays);
            }

            if (policy.MaxKeystrokes.HasValue)
            {
                await _mainDb.PruneExcessKeystrokesAsync(policy.MaxKeystrokes.Value);
                _logger?.LogInformation("Pruned keystrokes exceeding limit of {Max}", policy.MaxKeystrokes.Value);
            }

            var count = await _mainDb.GetKeystrokeCountAsync();
            _logger?.LogInformation("Current keystroke count: {Count}", count);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error applying retention policy");
        }
    }

    public async Task<List<KeystrokeEvent>> GetRecentKeystrokesAsync(int limit = 1000)
    {
        return await _mainDb.GetKeystrokesAsync(limit: limit);
    }

    public async Task<List<KeystrokeEvent>> GetKeystrokesByTimeRangeAsync(DateTime startTime, DateTime endTime)
    {
        return await _mainDb.GetKeystrokesAsync(startTime, endTime);
    }

    public async Task<long> GetKeystrokeCountAsync()
    {
        return await _mainDb.GetKeystrokeCountAsync();
    }

    public void Dispose()
    {
        if (_disposed) return;
        _hotDb?.Dispose();
        _mainDb?.Dispose();
        _disposed = true;
        GC.SuppressFinalize(this);
    }
}
