namespace KeyRecorder.Core.Models;

public class KeystrokeEvent
{
    public long Id { get; set; }
    public long SequenceId { get; set; }
    public DateTime Timestamp { get; set; }
    public int VirtualKeyCode { get; set; }
    public string KeyName { get; set; } = string.Empty;
    public bool IsKeyDown { get; set; }
    public bool IsShiftPressed { get; set; }
    public bool IsCtrlPressed { get; set; }
    public bool IsAltPressed { get; set; }
    public bool IsWinPressed { get; set; }
    public string? ActiveWindowTitle { get; set; }
    public string? ActiveProcessName { get; set; }
    public bool IsSynced { get; set; }
}
