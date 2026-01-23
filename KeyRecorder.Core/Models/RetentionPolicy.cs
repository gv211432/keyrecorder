namespace KeyRecorder.Core.Models;

public class RetentionPolicy
{
    public int RetentionDays { get; set; } = 7;
    public long? MaxKeystrokes { get; set; }
    public long? MaxStorageSizeBytes { get; set; }
}
