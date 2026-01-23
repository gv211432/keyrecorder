namespace KeyRecorder.Core.Models;

public class AppConfiguration
{
    public string DatabasePath { get; set; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
        "KeyRecorder");

    public int SyncIntervalMinutes { get; set; } = 5;
    public int IntegrityCheckIntervalMinutes { get; set; } = 60;
    public int SnapshotIntervalMinutes { get; set; } = 60;
    public int MaxSnapshotsToKeep { get; set; } = 24;

    public RetentionPolicy RetentionPolicy { get; set; } = new();

    public bool EnableGrouping { get; set; } = true;
    public int GroupingThresholdMs { get; set; } = 300;

    public bool IsRecordingPaused { get; set; }
    public bool ExcludeSecureDesktop { get; set; } = true;
}
