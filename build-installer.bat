@echo off
setlocal enabledelayedexpansion
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

REM Check Program Files (x86) - Most common for 32-bit apps
if exist "C:\Program Files (x86)\Inno Setup 6\ISCC.exe" set "INNO_PATH=C:\Program Files (x86)\Inno Setup 6\ISCC.exe"
if exist "C:\Program Files (x86)\Inno Setup 5\ISCC.exe" set "INNO_PATH=C:\Program Files (x86)\Inno Setup 5\ISCC.exe"

REM Check Program Files - For 64-bit installs
if exist "C:\Program Files\Inno Setup 6\ISCC.exe" set "INNO_PATH=C:\Program Files\Inno Setup 6\ISCC.exe"
if exist "C:\Program Files\Inno Setup 5\ISCC.exe" set "INNO_PATH=C:\Program Files\Inno Setup 5\ISCC.exe"

if not defined INNO_PATH (
    echo.
    echo ============================================================
    echo   WARNING: Inno Setup not found on your system!
    echo ============================================================
    echo.
    echo Inno Setup is required to build the installer.
    echo.
    echo What would you like to do?
    echo.
    echo   1 - Download and install automatically ^(Recommended^)
    echo   2 - Download manually from website
    echo   3 - Cancel build
    echo.
    set /p "INSTALL_CHOICE=Enter your choice [1, 2, or 3]: "

    if "!INSTALL_CHOICE!"=="1" goto :auto_install_inno
    if "!INSTALL_CHOICE!"=="2" goto :manual_install_inno
    goto :install_cancelled
)

goto :compile_installer

:auto_install_inno
echo.
echo ============================================================
echo   AUTOMATIC INSTALLATION
echo ============================================================
echo.
echo Downloading Inno Setup 6 from official website...
echo Source: https://jrsoftware.org/download.php/is.exe
echo.

REM Download Inno Setup installer
set "INNO_INSTALLER=%TEMP%\innosetup-installer.exe"
powershell -Command "& {[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12; Invoke-WebRequest -Uri 'https://jrsoftware.org/download.php/is.exe' -OutFile '%INNO_INSTALLER%' -UseBasicParsing}"

if %ERRORLEVEL% neq 0 (
    echo.
    echo ERROR: Failed to download Inno Setup installer
    echo Please check your internet connection or try manual installation.
    echo.
    pause
    exit /b 1
)

echo Download complete!
echo.
echo ============================================================
echo   INSTALLATION OPTIONS
echo ============================================================
echo.
echo How would you like to install Inno Setup?
echo.
echo   1 - Silent installation ^(automatic, no user interaction^)
echo   2 - GUI installation ^(shows installation wizard^)
echo.
set /p "INSTALL_MODE=Enter your choice [1 or 2]: "
echo.

if "!INSTALL_MODE!"=="1" (
    echo Running silent installation...
    echo.
    "%INNO_INSTALLER%" /VERYSILENT /SUPPRESSMSGBOXES /NORESTART /SP-
) else (
    echo Launching installation wizard...
    echo IMPORTANT: Please install to the default location for automatic detection.
    echo.
    pause
    "%INNO_INSTALLER%"
)

if %ERRORLEVEL% neq 0 (
    echo.
    echo ERROR: Inno Setup installation failed or was cancelled.
    echo.
    echo Please try running the build script again, or install manually from:
    echo https://jrsoftware.org/isdl.php
    echo.
    del "%INNO_INSTALLER%" 2>nul
    pause
    exit /b 1
)

echo.
echo ============================================================
echo   Installation Complete!
echo ============================================================
echo.
echo Cleaning up temporary files...
del "%INNO_INSTALLER%" 2>nul

REM Re-detect Inno Setup path after installation
echo Detecting Inno Setup installation...
set "INNO_PATH="
if exist "C:\Program Files (x86)\Inno Setup 6\ISCC.exe" set "INNO_PATH=C:\Program Files (x86)\Inno Setup 6\ISCC.exe"
if exist "C:\Program Files\Inno Setup 6\ISCC.exe" set "INNO_PATH=C:\Program Files\Inno Setup 6\ISCC.exe"

if not defined INNO_PATH (
    echo.
    echo ERROR: Cannot find Inno Setup after installation.
    echo Expected location: C:\Program Files (x86)\Inno Setup 6\ISCC.exe
    echo.
    echo Please restart this script or check your installation.
    echo.
    pause
    exit /b 1
)

echo SUCCESS: Found Inno Setup at: !INNO_PATH!
echo.
echo Continuing with installer build...
echo.
goto :compile_installer

:manual_install_inno
echo.
echo ============================================================
echo   MANUAL INSTALLATION INSTRUCTIONS
echo ============================================================
echo.
echo Please follow these steps:
echo.
echo 1. Download Inno Setup 6 from the website
echo    URL: https://jrsoftware.org/isdl.php
echo    Direct: https://jrsoftware.org/download.php/is.exe
echo.
echo 2. Run the downloaded installer
echo.
echo 3. Install to the default location:
echo    C:\Program Files (x86)\Inno Setup 6
echo.
echo 4. After installation is complete, run this script again:
echo    .\build-installer.bat
echo.
echo ============================================================
echo.
echo Opening download page in your browser now...
start https://jrsoftware.org/isdl.php
echo.
echo Press any key to exit this script...
pause >nul
exit /b 1

:install_cancelled
echo.
echo ============================================================
echo   Build Cancelled
echo ============================================================
echo.
echo You can run this script again anytime:
echo    .\build-installer.bat
echo.
pause
exit /b 1

:compile_installer
echo.
echo ============================================================
echo   Compiling Installer
echo ============================================================
echo.
echo Using Inno Setup compiler: %INNO_PATH%
echo Compiling: KeyRecorderInstaller.iss
echo.

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
