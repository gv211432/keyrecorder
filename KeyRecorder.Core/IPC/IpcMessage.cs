using KeyRecorder.Core.Models;

namespace KeyRecorder.Core.IPC;

public enum IpcMessageType
{
    KeystrokeNotification,
    GetRecentKeystrokes,
    GetRecentKeystrokesResponse,
    PauseRecording,
    ResumeRecording,
    GetStatus,
    GetStatusResponse,
    Shutdown
}

public class IpcMessage
{
    public IpcMessageType Type { get; set; }
    public string? Payload { get; set; }
}

public class StatusResponse
{
    public bool IsRecording { get; set; }
    public long TotalKeystrokes { get; set; }
    public DateTime? LastSyncTime { get; set; }
    public DateTime? LastSnapshotTime { get; set; }
}
