using System.Collections.Concurrent;
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

    // Debounce buffering for keystrokes - reduces database writes during fast typing
    private readonly ConcurrentQueue<KeystrokeEvent> _keystrokeBuffer = new();
    private Timer? _flushTimer;
    private readonly object _flushLock = new();
    private const int FlushDelayMs = 2000; // 2 second debounce

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

    public Task<long> RecordKeystrokeAsync(KeystrokeEvent keystroke)
    {
        // Assign sequence ID immediately to preserve ordering
        keystroke.SequenceId = Interlocked.Increment(ref _sequenceCounter);

        // Add to buffer instead of immediate database write
        _keystrokeBuffer.Enqueue(keystroke);

        // Reset the debounce timer - flush will happen after 2 seconds of inactivity
        ResetFlushTimer();

        // Return immediately - no blocking database operation
        return Task.FromResult(keystroke.SequenceId);
    }

    private void ResetFlushTimer()
    {
        lock (_flushLock)
        {
            // Dispose existing timer and create new one
            _flushTimer?.Dispose();
            _flushTimer = new Timer(
                _ => _ = FlushBufferAsync(),
                null,
                FlushDelayMs,
                Timeout.Infinite);
        }
    }

    private async Task FlushBufferAsync()
    {
        if (_keystrokeBuffer.IsEmpty)
            return;

        // Collect all buffered keystrokes
        var keystrokesToFlush = new List<KeystrokeEvent>();
        while (_keystrokeBuffer.TryDequeue(out var keystroke))
        {
            keystrokesToFlush.Add(keystroke);
        }

        if (keystrokesToFlush.Count == 0)
            return;

        try
        {
            // Bulk insert all keystrokes in a single transaction
            await _hotDb.BulkInsertKeystrokesAsync(keystrokesToFlush);
            _logger?.LogDebug("Flushed {Count} keystrokes to database", keystrokesToFlush.Count);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error flushing keystroke buffer");
            // Re-queue keystrokes on failure to avoid data loss
            foreach (var keystroke in keystrokesToFlush)
            {
                _keystrokeBuffer.Enqueue(keystroke);
            }
        }
    }

    /// <summary>
    /// Forces an immediate flush of buffered keystrokes to the database.
    /// Call this before closing the application to ensure no data is lost.
    /// </summary>
    public async Task FlushAsync()
    {
        lock (_flushLock)
        {
            _flushTimer?.Dispose();
            _flushTimer = null;
        }
        await FlushBufferAsync();
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
        // Get keystrokes from both hot DB (unsynced) and main DB
        var hotKeystrokes = await _hotDb.GetAllKeystrokesAsync(limit);
        var mainKeystrokes = await _mainDb.GetKeystrokesAsync(limit: limit);

        // Merge and deduplicate by SequenceId (or Timestamp if SequenceId not set)
        var combined = hotKeystrokes
            .Concat(mainKeystrokes)
            .GroupBy(k => k.SequenceId > 0 ? k.SequenceId : k.Timestamp.Ticks)
            .Select(g => g.First())
            .OrderByDescending(k => k.Timestamp)
            .Take(limit)
            .ToList();

        return combined;
    }

    public async Task<List<KeystrokeEvent>> GetKeystrokesByTimeRangeAsync(DateTime startTime, DateTime endTime)
    {
        return await _mainDb.GetKeystrokesAsync(startTime, endTime);
    }

    public async Task<long> GetKeystrokeCountAsync()
    {
        // Sum counts from both hot DB (unsynced) and main DB
        var hotCount = await _hotDb.GetKeystrokeCountAsync();
        var mainCount = await _mainDb.GetKeystrokeCountAsync();
        return hotCount + mainCount;
    }

    public void Dispose()
    {
        if (_disposed) return;

        // Flush any remaining buffered keystrokes before disposing
        lock (_flushLock)
        {
            _flushTimer?.Dispose();
            _flushTimer = null;
        }

        // Synchronously flush remaining keystrokes
        FlushBufferAsync().GetAwaiter().GetResult();

        _hotDb?.Dispose();
        _mainDb?.Dispose();
        _disposed = true;
        GC.SuppressFinalize(this);
    }
}
