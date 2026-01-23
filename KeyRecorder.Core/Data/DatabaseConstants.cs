namespace KeyRecorder.Core.Data;

public static class DatabaseConstants
{
    public const string HotDatabaseFileName = "keyrecorder_hot.db";
    public const string MainDatabaseFileName = "keyrecorder_main.db";
    public const string SnapshotDatabasePrefix = "keyrecorder_snapshot_";

    public const string TableName = "KeystrokeEvents";

    public const string CreateTableSql = @"
        CREATE TABLE IF NOT EXISTS KeystrokeEvents (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            SequenceId INTEGER NOT NULL,
            Timestamp TEXT NOT NULL,
            VirtualKeyCode INTEGER NOT NULL,
            KeyName TEXT NOT NULL,
            IsKeyDown INTEGER NOT NULL,
            IsShiftPressed INTEGER NOT NULL,
            IsCtrlPressed INTEGER NOT NULL,
            IsAltPressed INTEGER NOT NULL,
            IsWinPressed INTEGER NOT NULL,
            ActiveWindowTitle TEXT,
            ActiveProcessName TEXT,
            IsSynced INTEGER NOT NULL DEFAULT 0
        );

        CREATE INDEX IF NOT EXISTS idx_timestamp ON KeystrokeEvents(Timestamp);
        CREATE INDEX IF NOT EXISTS idx_sequence ON KeystrokeEvents(SequenceId);
        CREATE INDEX IF NOT EXISTS idx_synced ON KeystrokeEvents(IsSynced);
    ";
}
