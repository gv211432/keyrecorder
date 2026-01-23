<div align="center">
  <img src="KeyRecorder.UI/Assets/logo-name.png" alt="KeyRecorder Logo" width="600"/>

  <h3>Keyboard Activity Monitor for Windows</h3>

  <p>A local-only keyboard activity recorder designed for debugging and productivity tracking.</p>
  <p>Features 24/7 recording, SQLite storage with crash recovery, and real-time visualization.</p>

  <img src="https://img.shields.io/badge/.NET-10.0-blue?style=flat-square&logo=dotnet" alt=".NET 10"/>
  <img src="https://img.shields.io/badge/Platform-Windows-0078D6?style=flat-square&logo=windows" alt="Windows"/>
  <img src="https://img.shields.io/badge/License-Personal%20Use-green?style=flat-square" alt="License"/>
</div>

---

## Features

- **24/7 Continuous Recording**: Runs as a Windows Service with auto-restart
- **Global Keyboard Hook**: Captures all keystrokes system-wide
- **Triple Database Architecture**:
  - Hot Database: Live capture buffer
  - Main Database: Historical storage with retention policies
  - Snapshot Database: Periodic backups for recovery
- **Real-time UI**: WPF application for viewing keystroke history
- **Time-based Grouping**: Keystrokes organized by minute for easy viewing
- **IPC Communication**: Named Pipes for secure service-UI communication
- **Configurable Retention**: 7-365 days retention or keystroke count limits
- **Pause/Resume**: Control recording via UI
- **Local-Only**: No network access, no telemetry, all data stays on your machine

## System Requirements

- Windows 10/11
- .NET 10 Runtime
- Administrator privileges (for service installation)

## Project Structure

```
KeyRecorder/
├── KeyRecorder.Core/          # Shared library
│   ├── Capture/               # Keyboard hook implementation
│   ├── Data/                  # SQLite database layer
│   ├── IPC/                   # Inter-process communication
│   └── Models/                # Data models
├── KeyRecorder.Service/       # Windows Service
└── KeyRecorder.UI/            # WPF User Interface
```

## Installation

### Option 1: Using the Installer (Recommended for End Users)

**Download and run `KeyRecorderSetup.exe`**

1. Download the latest `KeyRecorderSetup.exe` from releases
2. Right-click and select "Run as Administrator"
3. Follow the installation wizard
4. The service will be installed and started automatically
5. Launch KeyRecorder UI from the Start Menu or Desktop

**What the installer does:**
- ✓ Installs Windows Service to `C:\Program Files\KeyRecorder\`
- ✓ Installs WPF UI application
- ✓ Creates database folder at `C:\ProgramData\KeyRecorder\`
- ✓ Registers and starts the Windows Service
- ✓ Creates Start Menu and Desktop shortcuts
- ✓ Provides clean uninstallation

### Option 2: Manual Installation (For Developers)

#### 2a. Build from Source

```powershell
dotnet build KeyRecorder.slnx -c Release
```

#### 2b. Install Service via PowerShell Script

Open PowerShell as Administrator:

```powershell
.\install-service.ps1
```

Or manually:

```powershell
# Navigate to the service directory
cd KeyRecorder.Service\bin\Release\net10.0

# Create the Windows Service
sc.exe create "KeyRecorder Service" binPath= "$PWD\KeyRecorder.Service.exe" start= auto

# Start the service
sc.exe start "KeyRecorder Service"
```

#### 2c. Verify Installation

```powershell
# Check service status
Get-Service "KeyRecorder Service"

# View service logs
Get-EventLog -LogName Application -Source "KeyRecorder Service" -Newest 10
```

### Building Your Own Installer

See [BUILD_INSTALLER.md](BUILD_INSTALLER.md) for complete instructions on creating `KeyRecorderSetup.exe`.

**Quick build:**
```powershell
.\build-installer.bat
```

Output: `Installer\KeyRecorderSetup.exe`

## Usage

### Starting the UI

1. Run `KeyRecorder.UI.exe` from the build output folder
2. The UI will automatically connect to the service
3. View real-time keystroke history organized by minute
4. Use Pause/Resume button to control recording
5. Click Refresh to update the display

### Configuration

Edit `appsettings.json` in the service directory:

```json
{
  "DatabasePath": "C:\\ProgramData\\KeyRecorder",
  "SyncIntervalMinutes": 5,
  "IntegrityCheckIntervalMinutes": 60,
  "RetentionDays": 7
}
```

Configuration options:
- **DatabasePath**: Location for SQLite database files (default: `C:\ProgramData\KeyRecorder`)
- **SyncIntervalMinutes**: How often to sync hot → main database (default: 5)
- **IntegrityCheckIntervalMinutes**: How often to check database integrity (default: 60)
- **RetentionDays**: How many days of history to keep (default: 7, max: 365)

### Database Files

The system maintains three SQLite files in the configured database path:

- `keyrecorder_hot.db` - Current live capture buffer
- `keyrecorder_main.db` - Historical keystroke data
- `keyrecorder_snapshot_YYYYMMDD_HHMMSS.db` - Periodic snapshots for recovery

## Background Maintenance

The service automatically performs:

1. **Hot → Main Sync** (every 5 minutes)
   - Transfers captured keystrokes to main database
   - Purges synced data from hot file

2. **Integrity Check** (every 60 minutes)
   - Verifies database consistency
   - Automatically recovers from latest snapshot if corruption detected
   - Applies retention policies (prunes old data)

3. **Snapshot Creation** (every 60 minutes)
   - Creates timestamped backup of main database
   - Keeps last 24 snapshots by default

## Uninstallation

### If Installed via Installer

1. **Settings → Apps → KeyRecorder → Uninstall**
2. Or run: `C:\Program Files\KeyRecorder\unins000.exe`
3. Choose whether to delete keystroke data when prompted

### If Installed Manually

```powershell
# Using PowerShell script
.\install-service.ps1 -Uninstall

# Or manually
Stop-Service "KeyRecorder Service"
sc.exe delete "KeyRecorder Service"

# Delete database files (optional)
Remove-Item "C:\ProgramData\KeyRecorder" -Recurse -Force
```

## Security & Privacy

- **Local-Only**: No network access, no cloud sync, no telemetry
- **Named Pipes with ACLs**: Secure IPC between service and UI
- **User Control**: Pause/resume functionality
- **Restricted Privileges**: Service runs with minimal required permissions
- **Data Isolation**: All data stored locally in configured directory

## Troubleshooting

### Service Won't Start

1. Check Event Viewer for errors:
   ```powershell
   Get-EventLog -LogName Application -Source "KeyRecorder Service" -Newest 10
   ```

2. Verify database path is accessible:
   ```powershell
   Test-Path "C:\ProgramData\KeyRecorder"
   ```

3. Ensure .NET 10 runtime is installed:
   ```powershell
   dotnet --version
   ```

### UI Can't Connect to Service

1. Verify service is running:
   ```powershell
   Get-Service "KeyRecorder Service"
   ```

2. Check if Named Pipe exists:
   ```powershell
   [System.IO.Directory]::GetFiles("\\.\\pipe\\") | Where-Object { $_ -like "*KeyRecorder*" }
   ```

3. Run UI as Administrator (temporarily for testing)

### Database Corruption

The system automatically detects and recovers from corruption using snapshots. If manual recovery is needed:

1. Stop the service
2. Backup existing databases
3. Restore from a snapshot:
   ```powershell
   Copy-Item "C:\ProgramData\KeyRecorder\keyrecorder_snapshot_*.db" `
             "C:\ProgramData\KeyRecorder\keyrecorder_main.db"
   ```
4. Start the service

## Architecture

### Components

1. **KeyboardHook**: Low-level Windows keyboard hook (WH_KEYBOARD_LL)
2. **DatabaseManager**: Orchestrates three SQLite databases with WAL mode
3. **IpcServer/IpcClient**: Named Pipes for service-UI communication
4. **KeyRecorderWorker**: Background service with timed maintenance jobs
5. **MainWindow**: WPF UI with real-time keystroke display

### Data Flow

```
Keyboard Input → KeyboardHook → Hot Database (every keystroke)
                                      ↓ (every 5 min)
                                 Main Database ← Retention Policies
                                      ↓ (every 60 min)
                                 Snapshot Files ← Keep last 24
                                      ↓
                                   UI Display
```

## Development

### Build from Source

```powershell
# Clone or navigate to repository
cd keyrecorder

# Restore packages
dotnet restore KeyRecorder.slnx

# Build solution
dotnet build KeyRecorder.slnx

# Run service (for testing)
dotnet run --project KeyRecorder.Service

# Run UI
dotnet run --project KeyRecorder.UI
```

### Testing

For development/testing, you can run the service as a console application instead of installing it as a Windows Service. The service will output logs to the console.

## License

This software is provided for personal use and debugging purposes. Not intended for employee surveillance or unauthorized monitoring.

## Disclaimer

Use of this software must comply with local laws and regulations. Always obtain consent before monitoring keyboard activity. The authors are not responsible for misuse of this software.
