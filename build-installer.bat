@echo off
REM ============================================================
REM   KeyRecorder Installer Build Script
REM   Builds the complete installer: KeyRecorderSetup.exe
REM ============================================================

echo.
echo ============================================================
echo           KeyRecorder Installer Build Script
echo         Building KeyRecorderSetup.exe installer...
echo ============================================================
echo.

REM Check if running from correct directory
if not exist "KeyRecorder.slnx" (
    echo ERROR: Please run this script from the keyrecorder root directory
    pause
    exit /b 1
)

REM Step 1: Clean previous builds
echo [1/4] Cleaning previous builds...
if exist "Installer" rmdir /s /q "Installer"
dotnet clean KeyRecorder.slnx -c Release >nul 2>&1

REM Step 2: Build all projects in Release mode
echo [2/4] Building projects in Release mode...
dotnet build KeyRecorder.slnx -c Release
if %ERRORLEVEL% neq 0 (
    echo.
    echo ERROR: Build failed. Please fix the build errors and try again.
    pause
    exit /b 1
)

REM Step 3: Verify build outputs
echo [3/4] Verifying build outputs...
if not exist "KeyRecorder.Service\bin\Release\net10.0\KeyRecorder.Service.exe" (
    echo ERROR: Service executable not found after build
    pause
    exit /b 1
)
if not exist "KeyRecorder.UI\bin\Release\net10.0-windows\KeyRecorder.UI.exe" (
    echo ERROR: UI executable not found after build
    pause
    exit /b 1
)

REM Step 4: Compile installer with Inno Setup
echo [4/4] Compiling installer with Inno Setup...

REM Try common Inno Setup installation paths
set "INNO_PATH="
if exist "C:\Program Files (x86)\Inno Setup 6\ISCC.exe" (
    set "INNO_PATH=C:\Program Files (x86)\Inno Setup 6\ISCC.exe"
)
if exist "C:\Program Files\Inno Setup 6\ISCC.exe" (
    set "INNO_PATH=C:\Program Files\Inno Setup 6\ISCC.exe"
)

if not defined INNO_PATH (
    echo.
    echo ERROR: Inno Setup not found!
    echo.
    echo Please install Inno Setup 6 from: https://jrsoftware.org/isinfo.php
    echo Expected location: C:\Program Files (x86)\Inno Setup 6\ISCC.exe
    echo.
    pause
    exit /b 1
)

"%INNO_PATH%" KeyRecorderInstaller.iss
if %ERRORLEVEL% neq 0 (
    echo.
    echo ERROR: Installer compilation failed
    pause
    exit /b 1
)

REM Success!
echo.
echo ============================================================
echo              BUILD COMPLETED SUCCESSFULLY!
echo ============================================================
echo.
echo Installer created: Installer\KeyRecorderSetup.exe
echo.
echo File Details:
dir Installer\KeyRecorderSetup.exe | find "KeyRecorderSetup.exe"
echo.
echo Next Steps:
echo   1. Test the installer by running: Installer\KeyRecorderSetup.exe
echo   2. Distribute KeyRecorderSetup.exe to users
echo   3. Users can install by double-clicking (requires admin rights)
echo.
pause
