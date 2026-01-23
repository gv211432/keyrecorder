# ╔═══════════════════════════════════════════════════════════╗
# ║              KeyRecorder Service Installer                ║
# ║         Keyboard Activity Monitor for Windows             ║
# ╚═══════════════════════════════════════════════════════════╝
#
# Run this script as Administrator
#
# Theme Colors: #0085d8 (Blue), #e61f47 (Red), #0d0f10 (Dark)

param(
    [switch]$Uninstall
)

# Display banner
Write-Host ""
Write-Host "╔═══════════════════════════════════════════════════════════╗" -ForegroundColor Cyan
Write-Host "║              KeyRecorder Service Installer                ║" -ForegroundColor Cyan
Write-Host "║         Keyboard Activity Monitor for Windows             ║" -ForegroundColor Cyan
Write-Host "╚═══════════════════════════════════════════════════════════╝" -ForegroundColor Cyan
Write-Host ""

$ServiceName = "KeyRecorder Service"
$ServiceDisplayName = "KeyRecorder Service"
$ServiceDescription = "Keyboard activity recording service for debugging and productivity tracking"

# Check if running as Administrator
$currentPrincipal = New-Object Security.Principal.WindowsPrincipal([Security.Principal.WindowsIdentity]::GetCurrent())
$isAdmin = $currentPrincipal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)

if (-not $isAdmin) {
    Write-Error "This script must be run as Administrator"
    Write-Host "Please right-click PowerShell and select 'Run as Administrator'"
    exit 1
}

if ($Uninstall) {
    Write-Host "Uninstalling $ServiceName..." -ForegroundColor Yellow

    # Stop service if running
    $service = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue
    if ($service) {
        if ($service.Status -eq 'Running') {
            Write-Host "Stopping service..."
            Stop-Service -Name $ServiceName -Force
            Start-Sleep -Seconds 2
        }

        Write-Host "Removing service..."
        sc.exe delete $ServiceName

        if ($LASTEXITCODE -eq 0) {
            Write-Host "Service uninstalled successfully!" -ForegroundColor Green
        } else {
            Write-Error "Failed to uninstall service. Exit code: $LASTEXITCODE"
            exit 1
        }
    } else {
        Write-Host "Service not found. Nothing to uninstall." -ForegroundColor Yellow
    }

    # Ask if user wants to delete data
    $deleteData = Read-Host "Do you want to delete all keystroke data? (y/n)"
    if ($deleteData -eq 'y' -or $deleteData -eq 'Y') {
        $dataPath = "C:\ProgramData\KeyRecorder"
        if (Test-Path $dataPath) {
            Write-Host "Deleting data from $dataPath..."
            Remove-Item $dataPath -Recurse -Force
            Write-Host "Data deleted." -ForegroundColor Green
        }
    }

    exit 0
}

# Installation
Write-Host "Installing $ServiceName..." -ForegroundColor Cyan

# Find the service executable
$scriptPath = Split-Path -Parent $MyInvocation.MyCommand.Path
$serviceExePath = Join-Path $scriptPath "KeyRecorder.Service\bin\Release\net10.0\KeyRecorder.Service.exe"

# Check if built
if (-not (Test-Path $serviceExePath)) {
    Write-Host "Service executable not found at: $serviceExePath" -ForegroundColor Red
    Write-Host "Building the solution..." -ForegroundColor Yellow

    Push-Location $scriptPath
    try {
        dotnet build KeyRecorder.slnx -c Release

        if ($LASTEXITCODE -ne 0) {
            Write-Error "Build failed. Please fix build errors and try again."
            exit 1
        }
    } finally {
        Pop-Location
    }
}

# Verify executable exists after build
if (-not (Test-Path $serviceExePath)) {
    Write-Error "Service executable still not found after build: $serviceExePath"
    exit 1
}

Write-Host "Service executable: $serviceExePath" -ForegroundColor Gray

# Check if service already exists
$existingService = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue
if ($existingService) {
    Write-Host "Service already exists. Stopping and removing old service..." -ForegroundColor Yellow

    if ($existingService.Status -eq 'Running') {
        Stop-Service -Name $ServiceName -Force
        Start-Sleep -Seconds 2
    }

    sc.exe delete $ServiceName
    Start-Sleep -Seconds 2
}

# Create database directory
$databasePath = "C:\ProgramData\KeyRecorder"
if (-not (Test-Path $databasePath)) {
    Write-Host "Creating database directory: $databasePath"
    New-Item -ItemType Directory -Path $databasePath -Force | Out-Null
}

# Install service
Write-Host "Creating Windows Service..."
$result = sc.exe create $ServiceName binPath= $serviceExePath start= auto DisplayName= $ServiceDisplayName

if ($LASTEXITCODE -ne 0) {
    Write-Error "Failed to create service. Exit code: $LASTEXITCODE"
    exit 1
}

# Set service description
sc.exe description $ServiceName $ServiceDescription | Out-Null

# Start service
Write-Host "Starting service..."
Start-Service -Name $ServiceName

Start-Sleep -Seconds 2

# Verify service is running
$service = Get-Service -Name $ServiceName
if ($service.Status -eq 'Running') {
    Write-Host ""
    Write-Host "╔═══════════════════════════════════════════════════════════╗" -ForegroundColor Green
    Write-Host "║           ✓ Installation Completed Successfully!         ║" -ForegroundColor Green
    Write-Host "╚═══════════════════════════════════════════════════════════╝" -ForegroundColor Green
    Write-Host ""
    Write-Host "Service Status:" -ForegroundColor Cyan
    Write-Host "  Name:       $($service.Name)" -ForegroundColor White
    Write-Host "  Status:     $($service.Status)" -ForegroundColor Green
    Write-Host "  Start Type: $($service.StartType)" -ForegroundColor White
    Write-Host ""
    Write-Host "Database Path:" -ForegroundColor Cyan
    Write-Host "  $databasePath" -ForegroundColor White
    Write-Host ""
    Write-Host "Next Steps:" -ForegroundColor Yellow
    Write-Host "  1. Launch KeyRecorder.UI.exe to view keystroke history" -ForegroundColor White
    Write-Host "  2. Configure retention in appsettings.json (optional)" -ForegroundColor White
    Write-Host "  3. Service will start automatically on boot" -ForegroundColor White
    Write-Host ""
    Write-Host "To uninstall: .\install-service.ps1 -Uninstall" -ForegroundColor DarkGray
    Write-Host ""
} else {
    Write-Host ""
    Write-Host "⚠ Warning: Service created but not running" -ForegroundColor Yellow
    Write-Host "Status: $($service.Status)" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "Check Event Viewer for errors:" -ForegroundColor Cyan
    Write-Host "  Get-EventLog -LogName Application -Source '$ServiceName' -Newest 10" -ForegroundColor White
    Write-Host ""
}
