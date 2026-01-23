# Building the KeyRecorder Installer

This guide explains how to create the `KeyRecorderSetup.exe` installer.

## Prerequisites

### 1. Install Inno Setup

**Download and install Inno Setup 6.x:**
- Website: https://jrsoftware.org/isinfo.php
- Direct Download: https://jrsoftware.org/download.php/is.exe
- Install to default location: `C:\Program Files (x86)\Inno Setup 6`

### 2. Verify .NET 10 SDK

Ensure .NET 10 SDK is installed:
```powershell
dotnet --version
# Should show: 10.0.102 or higher
```

## Build Steps

### Option 1: Using the Build Script (Easiest)

Simply run the provided batch script:

```powershell
.\build-installer.bat
```

This will:
1. Build all projects in Release mode
2. Compile the Inno Setup script
3. Create `KeyRecorderSetup.exe` in the `Installer` folder

### Option 2: Manual Build

#### Step 1: Build the Projects

```powershell
# Build all projects in Release mode
dotnet build KeyRecorder.slnx -c Release
```

Verify the build outputs:
- `KeyRecorder.Service\bin\Release\net10.0\KeyRecorder.Service.exe`
- `KeyRecorder.UI\bin\Release\net10.0-windows\KeyRecorder.UI.exe`
- `KeyRecorder.Core\bin\Release\net10.0\KeyRecorder.Core.dll`

#### Step 2: Compile the Installer

**Using Inno Setup GUI:**
1. Open `KeyRecorderInstaller.iss` in Inno Setup Compiler
2. Click **Build** → **Compile**
3. Find the output in the `Installer` folder

**Using Command Line:**
```powershell
"C:\Program Files (x86)\Inno Setup 6\ISCC.exe" KeyRecorderInstaller.iss
```

## Output

After successful build, you'll find:

```
Installer/
└── KeyRecorderSetup.exe    ← This is your distributable installer!
```

**File size:** Approximately 15-25 MB (includes .NET dependencies)

## Installer Features

The `KeyRecorderSetup.exe` installer includes:

### Installation Process
1. ✓ Welcome screen with app description
2. ✓ License agreement
3. ✓ Installation directory selection
4. ✓ Component selection (Service, UI, Docs)
5. ✓ Desktop shortcut option
6. ✓ Automatic service installation
7. ✓ Service auto-start configuration

### What Gets Installed
- **Windows Service** → `C:\Program Files\KeyRecorder\KeyRecorder.Service.exe`
- **WPF UI** → `C:\Program Files\KeyRecorder\KeyRecorder.UI.exe`
- **Core Libraries** → All required DLLs
- **Documentation** → README, guides, etc.
- **Database Folder** → `C:\ProgramData\KeyRecorder\` (created automatically)

### Uninstallation
- ✓ Stops the Windows Service
- ✓ Removes service registration
- ✓ Deletes program files
- ✓ **Asks user** if they want to delete keystroke data
- ✓ Preserves data if user chooses to keep it

## Testing the Installer

### Test Installation

1. **Run as Administrator:**
   ```powershell
   .\Installer\KeyRecorderSetup.exe
   ```

2. **Follow the wizard:**
   - Accept license
   - Choose installation directory
   - Select components
   - Click Install

3. **Verify:**
   ```powershell
   # Check service status
   Get-Service "KeyRecorder Service"

   # Should show Status: Running
   ```

4. **Launch the UI:**
   - Use desktop shortcut, or
   - Run from Start Menu → KeyRecorder

### Test Uninstallation

1. **From Control Panel:**
   - Settings → Apps → KeyRecorder → Uninstall

2. **Or use uninstaller directly:**
   ```powershell
   "C:\Program Files\KeyRecorder\unins000.exe"
   ```

3. **Verify clean removal:**
   ```powershell
   # Service should be gone
   Get-Service "KeyRecorder Service"  # Should error

   # Files should be removed
   Test-Path "C:\Program Files\KeyRecorder"  # Should be False
   ```

## Customization

### Update Version Number

Edit `KeyRecorderInstaller.iss`:
```pascal
#define MyAppVersion "1.0.0"  ← Change this
```

### Change Installation Directory

Edit `KeyRecorderInstaller.iss`:
```pascal
DefaultDirName={autopf}\{#MyAppName}  ← Modify this
```

### Add Custom Icons

Place your custom images in the project root:
- `installer-banner.bmp` (164x314 pixels, 24-bit BMP)
- `installer-icon.bmp` (55x58 pixels, 24-bit BMP)

Or remove these lines from the .iss file to use default Inno Setup images:
```pascal
WizardImageFile=installer-banner.bmp
WizardSmallImageFile=installer-icon.bmp
```

## Distribution

### Single-File Distribution

Simply distribute `KeyRecorderSetup.exe`:
- No dependencies needed (except .NET 10 Runtime on target machine)
- Single .exe file contains everything
- Users just double-click to install

### Recommended Distribution Methods

1. **GitHub Releases:**
   ```
   - Upload KeyRecorderSetup.exe as a release asset
   - Include SHA256 checksum
   - Provide installation instructions
   ```

2. **Direct Download:**
   - Host on website/cloud storage
   - Provide download link in README

3. **Package Managers (Advanced):**
   - Chocolatey package
   - Winget manifest

## Troubleshooting

### Build Error: "Cannot find file"

**Problem:** Inno Setup can't find the built binaries

**Solution:**
```powershell
# Make sure you've built in Release mode
dotnet build KeyRecorder.slnx -c Release

# Verify files exist
dir KeyRecorder.Service\bin\Release\net10.0\*.exe
dir KeyRecorder.UI\bin\Release\net10.0-windows\*.exe
```

### Installer Error: "Service installation failed"

**Problem:** User doesn't have admin rights

**Solution:**
- Installer requires Administrator privileges
- Right-click → "Run as Administrator"

### .NET Runtime Not Found

**Problem:** Target machine doesn't have .NET 10 Runtime

**Solution:**
1. Install .NET 10 Runtime first: https://dotnet.microsoft.com/download/dotnet/10.0
2. Or include runtime in installer (self-contained deployment) - increases size to ~100MB

## Advanced: Self-Contained Deployment

To create an installer that doesn't require .NET Runtime on target machine:

```powershell
# Build as self-contained
dotnet publish KeyRecorder.Service -c Release -r win-x64 --self-contained true
dotnet publish KeyRecorder.UI -c Release -r win-x64 --self-contained true

# Update .iss file to use publish folder instead of bin folder
# Source: "KeyRecorder.Service\bin\Release\net10.0\publish\..."
```

**Pros:** No .NET Runtime required on target machine
**Cons:** Much larger installer (~150+ MB)

## Code Signing (Recommended for Production)

For production distribution, sign your installer:

1. **Obtain a code signing certificate**
2. **Sign the installer:**
   ```powershell
   signtool sign /f MyCert.pfx /p Password /t http://timestamp.digicert.com KeyRecorderSetup.exe
   ```

This prevents Windows SmartScreen warnings.

## Support

For issues or questions:
- Check the main [README.md](README.md)
- Review [QUICKSTART.md](QUICKSTART.md)
- Open an issue on GitHub

---

**Summary:** Run `.\build-installer.bat` to create `KeyRecorderSetup.exe` - that's your single-file installer ready for distribution!
