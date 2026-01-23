# KeyRecorder Installation Guide

## Latest Build Information
- **Installer:** `Installer/KeyRecorderSetup.exe`
- **Size:** 13 MB
- **Build Date:** January 23, 2026
- **SHA256:** `c302a53815a16dd9294b93a52bf35b9ea9f37eb43b7227ab5fad97fb0614ab86`

## What's Included

### Architecture Support
✅ **64-bit Native** - Installs to `C:\Program Files` on x64 Windows
✅ **x64-only** - Optimized for modern 64-bit systems
✅ **No 32-bit hassles** - Clean, straightforward installation

### Smart Service Management
✅ **Auto-detection** - UI automatically detects service status
✅ **Auto-start** - Offers to start service if not running
✅ **UAC handling** - Prompts for admin privileges when needed
✅ **Retry logic** - Intelligent retry if connection fails
✅ **Error recovery** - Clear error messages with actionable solutions

### Fixed Issues
✅ **SQLite native libraries** - All platforms included in installer
✅ **IPC permissions** - Service can communicate with UI
✅ **.NET Runtime detection** - Properly checks for .NET 10
✅ **Service path** - Correct installation path on all architectures

## Installation Steps

### Fresh Installation

1. **Download the installer**
   - File: `KeyRecorderSetup.exe` (13 MB)

2. **Run the installer**
   - Right-click → "Run as administrator" (recommended)
   - Or double-click (will prompt for admin if needed)

3. **Follow the wizard**
   - Accept the MIT License
   - Choose installation directory (default: `C:\Program Files\KeyRecorder`)
   - Select components (all selected by default)
   - Choose whether to auto-start service on boot (recommended)

4. **Complete installation**
   - Installer will:
     - Copy all files
     - Install Windows Service
     - Start the service
     - Create shortcuts

5. **Launch the UI**
   - Check "Launch KeyRecorder" at the end of installation
   - Or use Desktop shortcut
   - Or Start Menu → KeyRecorder

### Upgrading from Previous Version

1. **Uninstall old version**
   - Open "Add or Remove Programs"
   - Find "KeyRecorder"
   - Click "Uninstall"
   - **IMPORTANT:** When asked "Delete all data?", choose **NO** to keep your keystroke history

2. **Install new version**
   - Run `KeyRecorderSetup.exe`
   - Follow normal installation steps
   - Your previous database will be preserved

## Troubleshooting

### Service Won't Start

**Symptom:** UI shows "Service Not Running"

**Solution:**
1. The UI will automatically offer to start the service
2. Click "Yes" when prompted
3. Grant administrator privileges when UAC prompt appears
4. Service will start automatically

**Manual Start:**
```cmd
sc start "KeyRecorder Service"
```

### Connection Error After Service Start

**Symptom:** "Unable to connect via IPC"

**Solution:**
1. The UI will offer to retry
2. Click "Yes" to retry connection
3. Wait 2-3 seconds for service to fully initialize
4. If still failing, try restarting the service

**Manual Restart:**
```cmd
sc stop "KeyRecorder Service"
timeout /t 2
sc start "KeyRecorder Service"
```

### .NET Runtime Not Found

**Symptom:** Installer shows ".NET 10 Runtime is not installed"

**Solution:**
1. Download .NET 10 Runtime from: https://dotnet.microsoft.com/download/dotnet/10.0
2. Install: "Desktop Runtime" (includes everything needed)
3. Run KeyRecorder installer again

**Verify .NET Installation:**
```cmd
dotnet --list-runtimes
```

You should see:
- `Microsoft.NETCore.App 10.0.x`
- `Microsoft.WindowsDesktop.App 10.0.x`

### Service Installed in Wrong Location

**Symptom:** Service path shows `C:\Program Files (x86)`

**This is Fixed:** The new installer (v1.0.0) always installs to the correct location:
- 64-bit Windows → `C:\Program Files\KeyRecorder`
- Proper architecture detection
- No more 32-bit confusion

If you have the old version, uninstall it and install the new one.

## Verifying Installation

### Check Service Status
```cmd
sc query "KeyRecorder Service"
```

Expected output:
```
STATE              : 4  RUNNING
```

### Check Service Path
```cmd
sc qc "KeyRecorder Service"
```

Expected path:
```
BINARY_PATH_NAME   : C:\Program Files\KeyRecorder\KeyRecorder.Service.exe
```

### Check Database
The database is created at:
```
C:\ProgramData\KeyRecorder\
```

Files you should see:
- `hot.db` (temporary buffer)
- `main.db` (main storage)
- `snapshots/` folder

## Features Summary

### Windows Service
- **Name:** KeyRecorder Service
- **Start Type:** Automatic
- **Account:** Local System
- **Description:** Keyboard activity recording service

### WPF UI Application
- **Name:** KeyRecorder.UI.exe
- **Mode:** Desktop application
- **Connection:** IPC (Named Pipes)
- **Auto-reconnect:** Yes

### Database
- **Engine:** SQLite 3
- **Mode:** WAL (Write-Ahead Logging)
- **Location:** `C:\ProgramData\KeyRecorder`
- **Backup:** Automatic snapshots

## Uninstallation

### Standard Uninstall

1. Open "Add or Remove Programs"
2. Find "KeyRecorder"
3. Click "Uninstall"
4. Choose whether to keep database:
   - **Yes** = Delete everything (clean removal)
   - **No** = Keep database for reinstallation

### Manual Cleanup (if needed)

If uninstaller fails or you want complete removal:

```cmd
REM Stop and remove service
sc stop "KeyRecorder Service"
sc delete "KeyRecorder Service"

REM Delete program files
rmdir /s /q "C:\Program Files\KeyRecorder"

REM Delete database (optional)
rmdir /s /q "C:\ProgramData\KeyRecorder"

REM Delete shortcuts
del "%PUBLIC%\Desktop\KeyRecorder.lnk"
rmdir /s /q "%ProgramData%\Microsoft\Windows\Start Menu\Programs\KeyRecorder"
```

## Support

### Log Files
Service logs are written to Windows Event Log:
- **Source:** KeyRecorder Service
- **Log:** Application

View logs:
```cmd
eventvwr.msc
```
Navigate to: Windows Logs → Application → Filter by Source: "KeyRecorder Service"

### Database Integrity
Check database:
```cmd
cd "C:\ProgramData\KeyRecorder"
sqlite3 main.db "PRAGMA integrity_check;"
```

Expected output: `ok`

## Advanced Configuration

### Change Database Location

Edit: `C:\Program Files\KeyRecorder\appsettings.json`

```json
{
  "DatabasePath": "C:\\CustomPath\\KeyRecorder"
}
```

Then restart service:
```cmd
sc stop "KeyRecorder Service"
sc start "KeyRecorder Service"
```

### Change Retention Policy

Edit: `C:\Program Files\KeyRecorder\appsettings.json`

```json
{
  "RetentionDays": 90,
  "SnapshotIntervalHours": 24
}
```

### Disable Auto-Start

```cmd
sc config "KeyRecorder Service" start= demand
```

Re-enable:
```cmd
sc config "KeyRecorder Service" start= auto
```

## Security Notes

### Permissions
- Service runs as **Local System** (high privileges required for keyboard hook)
- Database folder has **Users-Modify** permissions
- IPC pipe allows **Everyone** (local machine only)

### Privacy
- **100% Local** - No cloud, no telemetry, no network activity
- **No encryption** - Database is stored in plain text
- **Access control** - Protect `C:\ProgramData\KeyRecorder` with NTFS permissions if needed

### Responsible Use
This tool is designed for legitimate purposes:
- Personal productivity tracking
- Software development debugging
- Typing pattern analysis
- Accessibility research

**Do NOT use to:**
- Monitor others without consent
- Capture passwords or sensitive data
- Violate privacy laws or regulations

## License
MIT License - See LICENSE file for details

---

**Build Info:**
- Version: 1.0.0
- Compiler: Inno Setup 6.7.0
- Platform: Windows x64
- .NET: 10.0
