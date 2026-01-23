using KeyRecorder.Core.Models;

namespace KeyRecorder.Core.Data;

public class SnapshotDatabase
{
    private readonly string _basePath;

    public SnapshotDatabase(string basePath)
    {
        _basePath = basePath;
    }

    public async Task CreateSnapshotAsync(string sourceDbPath)
    {
        var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
        var snapshotPath = Path.Combine(_basePath, $"{DatabaseConstants.SnapshotDatabasePrefix}{timestamp}.db");

        await Task.Run(() => File.Copy(sourceDbPath, snapshotPath, overwrite: false));
    }

    public async Task<List<string>> GetSnapshotsAsync()
    {
        return await Task.Run(() =>
        {
            if (!Directory.Exists(_basePath))
                return new List<string>();

            return Directory.GetFiles(_basePath, $"{DatabaseConstants.SnapshotDatabasePrefix}*.db")
                .OrderByDescending(f => f)
                .ToList();
        });
    }

    public async Task PruneOldSnapshotsAsync(int maxSnapshots)
    {
        var snapshots = await GetSnapshotsAsync();

        if (snapshots.Count <= maxSnapshots)
            return;

        var snapshotsToDelete = snapshots.Skip(maxSnapshots);

        await Task.Run(() =>
        {
            foreach (var snapshot in snapshotsToDelete)
            {
                try
                {
                    File.Delete(snapshot);
                }
                catch
                {
                    // Ignore errors when deleting old snapshots
                }
            }
        });
    }

    public async Task<bool> RestoreFromSnapshotAsync(string snapshotPath, string targetDbPath)
    {
        try
        {
            await Task.Run(() =>
            {
                if (File.Exists(targetDbPath))
                {
                    var backupPath = targetDbPath + ".corrupt";
                    File.Move(targetDbPath, backupPath, overwrite: true);
                }

                File.Copy(snapshotPath, targetDbPath, overwrite: true);
            });

            return true;
        }
        catch
        {
            return false;
        }
    }
}
