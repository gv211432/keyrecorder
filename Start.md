# Build
dotnet build KeyRecorder.slnx -c Release

# Install (as Administrator)
.\install-service.ps1

# Run UI
.\KeyRecorder.UI\bin\Release\net10.0-windows\KeyRecorder.UI.exe
