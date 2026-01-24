# KeyRecorder Troubleshooting Guide

## Current Issues and Fixes

### 1. "Unable to connect to KeyRecorder service via IPC" Error

**Root Cause:** The Event Viewer shows the service crashed on startup with this error:
```
System.IO.FileNotFoundException: Could not load file or assembly 'KeyRecorder.Core, Version=1.0.0.0'
```

Even though the DLL exists in `C:\Program Files\KeyRecorder\`, the service failed to load it properly.

**Fix:**

**Option A: Restart the Service**
```powershell
Restart-Service "KeyRecorder Service"
```

**Option B: Via Services App**
1. Press `Win + R`
2. Type `services.msc` and press Enter
3. Find "KeyRecorder Service" in the list
4. Right-click → Restart

**Option C: Reinstall (if restart doesn't work)**
1. Uninstall KeyRecorder via Settings → Apps
2. Rebuild the installer: `.\build-installer.bat`
3. Reinstall using the new `Installer\KeyRecorderSetup.exe`

---

### 2. Desktop Icon Missing Logo

**Root Cause:** The installer is using the .exe's embedded icon instead of logo.png. Windows shortcuts require .ico format, not .png.

**Fix:**

You need to create an `app.ico` file from your logo.png:

**Using Online Converter (Easiest):**
1. Go to https://convertio.co/png-ico/
2. Upload `KeyRecorder.UI\Assets\logo.png`
3. Convert to ICO format (256x256)
4. Save as `KeyRecorder.UI\Assets\app.ico`

**Using PowerShell (if you have the skills):**
```powershell
# This requires .NET image libraries
Add-Type -AssemblyName System.Drawing
$img = [System.Drawing.Image]::FromFile("KeyRecorder.UI\Assets\logo.png")
$icon = [System.Drawing.Icon]::FromHandle($img.GetHicon())
$stream = [System.IO.FileStream]::new("KeyRecorder.UI\Assets\app.ico", [System.IO.FileMode]::Create)
$icon.Save($stream)
$stream.Close()
```

Then update the Inno Setup installer to use this icon (see step 3 below).

---

### 3. No System Tray Icon

**Root Cause:** The WPF UI application doesn't have a system tray icon implementation.

**Status:** This is a feature that needs to be added to the application code.

**What it needs:**
- Add a `NotifyIcon` to MainWindow
- Icon in system tray when app is running
- Right-click menu with: Show/Hide, Pause/Resume, Exit
- Click to show/hide main window

This would require code changes to the WPF application (see below for implementation).

---

## Detailed Fixes

### Fix 1: Add .ico File to Project

After creating `app.ico`:

1. Update `KeyRecorderInstaller.iss` line 29:
```pascal
; Change from:
; SetupIconFile=KeyRecorder.UI\Assets\logo.png

; To:
SetupIconFile=KeyRecorder.UI\Assets\app.ico
```

2. Update the desktop icon in `KeyRecorderInstaller.iss` line 98:
```pascal
; Change from:
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; WorkingDir: "{app}"; Tasks: desktopicon

; To:
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; IconFilename: "{app}\app.ico"; WorkingDir: "{app}"; Tasks: desktopicon
```

3. Add app.ico to the installer files section in `KeyRecorderInstaller.iss` (after line 84):
```pascal
; Add this line:
Source: "KeyRecorder.UI\Assets\app.ico"; DestDir: "{app}"; Flags: ignoreversion; Components: ui
```

### Fix 2: Add System Tray Support (Optional Enhancement)

This requires modifying the WPF application. Here's what needs to be added:

**1. Add NuGet Package:**
```xml
<!-- Add to KeyRecorder.UI.csproj -->
<PackageReference Include="Hardcodet.NotifyIcon.Wpf" Version="1.1.0" />
```

**2. Add System Tray Icon to MainWindow.xaml:**
```xml
<!-- Add inside <Window> tag -->
<Window.Resources>
    <ContextMenu x:Key="TrayMenu">
        <MenuItem Header="Show Window" Click="ShowWindow_Click" />
        <MenuItem Header="Pause Recording" x:Name="PauseMenuItem" Click="PauseRecording_Click" />
        <Separator />
        <MenuItem Header="Exit" Click="ExitApp_Click" />
    </ContextMenu>
</Window.Resources>

<!-- Add this before </Window> closing tag -->
<tb:TaskbarIcon x:Name="TrayIcon"
                IconSource="Assets/logo.ico"
                ToolTipText="KeyRecorder - Running"
                ContextMenu="{StaticResource TrayMenu}"
                TrayLeftMouseDown="TrayIcon_Click" />
```

**3. Add event handlers in MainWindow.xaml.cs:**
```csharp
private void TrayIcon_Click(object sender, RoutedEventArgs e)
{
    ShowMainWindow();
}

private void ShowWindow_Click(object sender, RoutedEventArgs e)
{
    ShowMainWindow();
}

private void ShowMainWindow()
{
    this.Show();
    this.WindowState = WindowState.Normal;
    this.Activate();
}

protected override void OnStateChanged(EventArgs e)
{
    if (WindowState == WindowState.Minimized)
    {
        this.Hide();
        TrayIcon.ShowBalloonTip("KeyRecorder", "Minimized to system tray", BalloonIcon.Info);
    }
    base.OnStateChanged(e);
}

private async void ExitApp_Click(object sender, RoutedEventArgs e)
{
    TrayIcon.Dispose();
    Application.Current.Shutdown();
}
```

---

## Verification Steps

After applying fixes:

### 1. Verify Service is Running
```powershell
Get-Service "KeyRecorder Service"
# Should show: Status = Running

# Check for errors
Get-EventLog -LogName Application -Source "KeyRecorder*" -Newest 5
# Should NOT show FileNotFoundException errors
```

### 2. Verify IPC Connection
- Launch KeyRecorder UI
- Should connect without errors
- Should show keystroke timeline

### 3. Verify Desktop Icon
- Check desktop shortcut
- Should show your logo, not generic .exe icon

### 4. Verify System Tray (if implemented)
- Launch KeyRecorder UI
- Should see icon in system tray
- Right-click should show menu
- Minimize window should hide to tray

---

## Quick Fix Commands

### Restart Service
```powershell
Restart-Service "KeyRecorder Service"
```

### Check Service Status
```powershell
Get-Service "KeyRecorder Service" | Format-List
```

### View Recent Errors
```powershell
Get-EventLog -LogName Application -Newest 10 | Where-Object {$_.Message -like '*KeyRecorder*'}
```

### Rebuild and Reinstall
```powershell
# From keyrecorder root directory
.\build-installer.bat

# Then run:
.\Installer\KeyRecorderSetup.exe
```

---

## Known Issues

1. **Service shows "Running" but isn't working**
   - This happens when the background worker crashes but the service host stays running
   - Solution: Restart the service

2. **DLL not found errors**
   - Usually means incomplete installation or missing runtime files
   - Solution: Rebuild and reinstall with latest build

3. **Permission denied errors**
   - Service needs to run as SYSTEM
   - UI needs normal user permissions
   - Check Event Viewer for specific permission errors

---

## Getting Help

If issues persist:
1. Check Event Viewer: Windows Logs → Application
2. Look for .NET Runtime or KeyRecorder Service errors
3. Check service logs (if implemented)
4. Verify all DLLs exist in `C:\Program Files\KeyRecorder\`
5. Verify .NET 10 Runtime is installed: `dotnet --version`

---

**Last Updated:** 2026-01-23
