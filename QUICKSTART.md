# KeyRecorder - Quick Start Guide

## Installation

### For End Users (1 minute) ⭐ RECOMMENDED

1. **Download** `KeyRecorderSetup.exe` from releases
2. **Right-click** → "Run as Administrator"
3. **Follow** the installation wizard
4. **Done!** The service is running, launch the UI from Start Menu

**That's it!** The installer handles everything automatically.

---

### For Developers (5 minutes)

#### Step 1: Build the Application

Open PowerShell in the project directory:

```powershell
dotnet build KeyRecorder.slnx -c Release
```

#### Step 2: Install as Windows Service

**Run PowerShell as Administrator:**

```powershell
.\install-service.ps1
```

This script will:
- Build the project if not already built
- Create the Windows Service
- Create the database directory (`C:\ProgramData\KeyRecorder`)
- Start the service automatically

#### Step 3: Launch the UI

Navigate to the UI build folder:

```powershell
.\KeyRecorder.UI\bin\Release\net10.0-windows\KeyRecorder.UI.exe
```

Or double-click `KeyRecorder.UI.exe` in Windows Explorer.

---

### Building the Installer

To create your own `KeyRecorderSetup.exe`:

```powershell
.\build-installer.bat
```

Output: `Installer\KeyRecorderSetup.exe`

See [BUILD_INSTALLER.md](BUILD_INSTALLER.md) for details.

## Verification

After installation, verify the service is running:

```powershell
Get-Service "KeyRecorder Service"
```

You should see:
- **Status**: Running
- **StartType**: Automatic

## Usage

### UI Controls

- **Pause/Resume Button**: Control recording on/off
- **Refresh Button**: Manually update the keystroke display
- **Timeline View**: Shows keystrokes grouped by minute
- **Statistics Bar**: Displays total keystrokes, last sync time, and recording status

### Viewing Keystrokes

The UI displays keystrokes in a timeline format:
- Each row represents one minute of activity
- Keystrokes are shown with modifiers (Ctrl, Alt, Shift, Win)
- The display updates automatically every 5 seconds

### Configuration

Edit `KeyRecorder.Service\bin\Release\net10.0\appsettings.json`:

```json
{
  "DatabasePath": "C:\\ProgramData\\KeyRecorder",
  "SyncIntervalMinutes": 5,
  "IntegrityCheckIntervalMinutes": 60,
  "RetentionDays": 7
}
```

After changing configuration, restart the service:

```powershell
Restart-Service "KeyRecorder Service"
```

## Uninstallation

### If Installed via Installer

**Settings → Apps → KeyRecorder → Uninstall**

Or run the uninstaller:
```powershell
"C:\Program Files\KeyRecorder\unins000.exe"
```

You'll be asked if you want to delete your keystroke data.

### If Installed Manually

Run PowerShell as Administrator:

```powershell
.\install-service.ps1 -Uninstall
```

This will:
- Stop the service
- Remove the service registration
- Optionally delete all keystroke data

## Troubleshooting

### Service fails to start

1. Check Event Viewer for errors:
   ```powershell
   Get-EventLog -LogName Application -Source "KeyRecorder Service" -Newest 10
   ```

2. Ensure .NET 10 runtime is installed:
   ```powershell
   dotnet --version
   ```

### UI can't connect

1. Verify service is running:
   ```powershell
   Get-Service "KeyRecorder Service"
   ```

2. If stopped, start it:
   ```powershell
   Start-Service "KeyRecorder Service"
   ```

### No keystrokes appearing

- Wait 5-10 seconds for data to sync
- Click the **Refresh** button
- Check that recording is not paused (status should show "Active")

## Key Features

✓ **Automatic startup** - Service starts on boot
✓ **Crash recovery** - Automatic database snapshots and recovery
✓ **Data retention** - Configurable retention policies (7-365 days)
✓ **Pause/Resume** - Control recording from UI
✓ **Local-only** - No network access, all data stays on your machine
✓ **Secure IPC** - Named Pipes with ACLs for service-UI communication

## Architecture Overview

```
┌─────────────────────────────────────────┐
│   Windows Service (Background)          │
│   - Global Keyboard Hook                │
│   - SQLite Database (Hot/Main/Snapshot) │
│   - IPC Server (Named Pipes)            │
│   - Background Jobs (Sync/Integrity)    │
└──────────────┬──────────────────────────┘
               │ IPC
               │ Named Pipes
┌──────────────┴──────────────────────────┐
│   WPF UI (User Interface)               │
│   - Real-time Keystroke Display         │
│   - Time-based Grouping                 │
│   - Pause/Resume Controls               │
│   - Statistics Dashboard                │
└─────────────────────────────────────────┘
```

## Database Files

Located at `C:\ProgramData\KeyRecorder`:

- **keyrecorder_hot.db** - Live capture buffer (current session)
- **keyrecorder_main.db** - Historical data (with retention applied)
- **keyrecorder_snapshot_*.db** - Periodic backups (last 24 hours)

## Background Jobs

The service automatically runs these maintenance tasks:

1. **Sync** (every 5 minutes): Hot → Main database transfer
2. **Integrity Check** (hourly): Database verification + retention enforcement
3. **Snapshot** (hourly): Backup creation for recovery

## Performance

- **CPU Usage**: <1% when idle
- **Memory**: ~50-100 MB for service
- **Disk I/O**: Minimal (WAL mode for efficient writes)
- **Capture Latency**: <5ms per keystroke

## Security

- ✓ No network access
- ✓ No telemetry
- ✓ Local-only storage
- ✓ Named Pipes with ACLs for IPC
- ✓ User-controlled pause/resume

## Next Steps

1. **Customize retention**: Edit `appsettings.json` to change retention period
2. **Review data**: Use the UI to browse captured keystrokes
3. **Monitor service**: Check Event Viewer for service logs
4. **Export data**: (Future feature) Export to JSON/CSV

## Support

For issues or questions:
1. Check [README.md](README.md) for detailed documentation
2. Review Event Viewer logs for errors
3. Verify .NET 10 runtime is installed
4. Ensure running with Administrator privileges for service operations

---

**Remember**: This tool is for personal debugging and productivity tracking. Always use responsibly and in compliance with local laws.
