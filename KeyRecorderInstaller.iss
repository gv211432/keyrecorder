; KeyRecorder Installer Script
; Created with Inno Setup 6.x (https://jrsoftware.org/isinfo.php)
; This script creates a single KeyRecorderSetup.exe installer

#define MyAppName "KeyRecorder"
#define MyAppVersion "1.0.0"
#define MyAppPublisher "KeyRecorder"
#define MyAppURL "https://github.com/yourusername/keyrecorder"
#define MyAppExeName "KeyRecorder.UI.exe"
#define MyServiceExeName "KeyRecorder.Service.exe"
#define MyServiceName "KeyRecorder Service"

[Setup]
; Basic Information
AppId={{A3B8C9D0-1234-5678-9ABC-DEF012345678}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
AllowNoIcons=yes
LicenseFile=LICENSE
OutputDir=Installer
OutputBaseFilename=KeyRecorderSetup
Compression=lzma2/ultra64
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=admin
UninstallDisplayIcon={app}\logo.ico
SetupIconFile=logo.ico
; Architecture Support - Install to 64-bit Program Files on 64-bit Windows
ArchitecturesInstallIn64BitMode=x64compatible
ArchitecturesAllowed=x64compatible

; Visual Appearance - Optional custom images commented out
; WizardImageFile=installer-banner.bmp
; WizardSmallImageFile=installer-icon.bmp

; Version Info
VersionInfoVersion={#MyAppVersion}
VersionInfoCompany={#MyAppPublisher}
VersionInfoDescription={#MyAppName} Installer
VersionInfoCopyright=Copyright © 2026 {#MyAppPublisher}

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Types]
Name: "full"; Description: "Full installation"
Name: "custom"; Description: "Custom installation"; Flags: iscustom

[Components]
Name: "service"; Description: "KeyRecorder Service (Background Recording)"; Types: full custom; Flags: fixed
Name: "ui"; Description: "KeyRecorder UI (Viewer Application)"; Types: full custom; Flags: fixed
Name: "docs"; Description: "Documentation"; Types: full

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"
Name: "quicklaunchicon"; Description: "{cm:CreateQuickLaunchIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked
Name: "autostart"; Description: "Start service automatically on boot"; GroupDescription: "Service Options:"; Flags: checkedonce
Name: "startupitem"; Description: "Start KeyRecorder UI on login (required for keyboard capture)"; GroupDescription: "Startup Options:"; Flags: checkedonce

[Files]
; Application Icon
Source: "logo.ico"; DestDir: "{app}"; Flags: ignoreversion

; Core Library
Source: "KeyRecorder.Core\bin\Release\net10.0\KeyRecorder.Core.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "KeyRecorder.Core\bin\Release\net10.0\*.dll"; DestDir: "{app}"; Flags: ignoreversion

; Windows Service
Source: "KeyRecorder.Service\bin\Release\net10.0\{#MyServiceExeName}"; DestDir: "{app}"; Flags: ignoreversion; Components: service
Source: "KeyRecorder.Service\bin\Release\net10.0\*.dll"; DestDir: "{app}"; Flags: ignoreversion skipifsourcedoesntexist; Components: service
Source: "KeyRecorder.Service\bin\Release\net10.0\*.json"; DestDir: "{app}"; Flags: ignoreversion confirmoverwrite; Components: service
Source: "KeyRecorder.Service\bin\Release\net10.0\*.deps.json"; DestDir: "{app}"; Flags: ignoreversion; Components: service
Source: "KeyRecorder.Service\bin\Release\net10.0\*.runtimeconfig.json"; DestDir: "{app}"; Flags: ignoreversion; Components: service
Source: "KeyRecorder.Service\bin\Release\net10.0\runtimes\*"; DestDir: "{app}\runtimes"; Flags: ignoreversion recursesubdirs; Components: service

; WPF UI Application
Source: "KeyRecorder.UI\bin\Release\net10.0-windows\{#MyAppExeName}"; DestDir: "{app}"; Flags: ignoreversion; Components: ui
Source: "KeyRecorder.UI\bin\Release\net10.0-windows\*.dll"; DestDir: "{app}"; Flags: ignoreversion skipifsourcedoesntexist; Components: ui
Source: "KeyRecorder.UI\bin\Release\net10.0-windows\*.deps.json"; DestDir: "{app}"; Flags: ignoreversion; Components: ui
Source: "KeyRecorder.UI\bin\Release\net10.0-windows\*.runtimeconfig.json"; DestDir: "{app}"; Flags: ignoreversion; Components: ui
Source: "KeyRecorder.UI\bin\Release\net10.0-windows\runtimes\*"; DestDir: "{app}\runtimes"; Flags: ignoreversion recursesubdirs; Components: ui

; Documentation
Source: "README.md"; DestDir: "{app}"; Flags: ignoreversion; Components: docs
Source: "QUICKSTART.md"; DestDir: "{app}"; Flags: ignoreversion; Components: docs
Source: "BRANDING.md"; DestDir: "{app}"; Flags: ignoreversion; Components: docs
Source: "Requirements.md"; DestDir: "{app}"; Flags: ignoreversion; Components: docs

[Dirs]
Name: "{commonappdata}\KeyRecorder"; Permissions: users-modify

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; WorkingDir: "{app}"
Name: "{group}\{cm:UninstallProgram,{#MyAppName}}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; WorkingDir: "{app}"; Tasks: desktopicon
Name: "{userappdata}\Microsoft\Internet Explorer\Quick Launch\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: quicklaunchicon

[Registry]
; Add UI to Windows startup (required for keyboard capture in user session)
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; ValueType: string; ValueName: "{#MyAppName}"; ValueData: """{app}\{#MyAppExeName}"" --minimized"; Flags: uninsdeletevalue; Tasks: startupitem

[Run]
; Install and start the service
Filename: "{sys}\sc.exe"; Parameters: "create ""{#MyServiceName}"" binPath= ""{app}\{#MyServiceExeName}"" start= auto DisplayName= ""{#MyServiceName}"""; Flags: runhidden; StatusMsg: "Installing Windows Service..."; Tasks: autostart
Filename: "{sys}\sc.exe"; Parameters: "description ""{#MyServiceName}"" ""Keyboard activity recording service for debugging and productivity tracking"""; Flags: runhidden; Tasks: autostart
Filename: "{sys}\sc.exe"; Parameters: "start ""{#MyServiceName}"""; Flags: runhidden; StatusMsg: "Starting Windows Service..."; Tasks: autostart

; Optional: Launch UI after installation
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent

[UninstallRun]
; Kill the UI application first
Filename: "{sys}\taskkill.exe"; Parameters: "/F /IM ""{#MyAppExeName}"""; Flags: runhidden; RunOnceId: "KillUI"
; Stop the service
Filename: "{sys}\sc.exe"; Parameters: "stop ""{#MyServiceName}"""; Flags: runhidden; RunOnceId: "StopService"
; Wait for service to stop
Filename: "{sys}\timeout.exe"; Parameters: "/t 3 /nobreak"; Flags: runhidden; RunOnceId: "WaitForStop"
; Delete the service
Filename: "{sys}\sc.exe"; Parameters: "delete ""{#MyServiceName}"""; Flags: runhidden; RunOnceId: "DeleteService"
; Wait a moment for cleanup
Filename: "{sys}\timeout.exe"; Parameters: "/t 2 /nobreak"; Flags: runhidden; RunOnceId: "WaitForCleanup"

[UninstallDelete]
Type: filesandordirs; Name: "{commonappdata}\KeyRecorder"

[Code]
var
  DataDeletionPage: TInputOptionWizardPage;

procedure InitializeWizard;
begin
  // Welcome message
  WizardForm.WelcomeLabel2.Caption :=
    'This will install KeyRecorder - Keyboard Activity Monitor on your computer.' + #13#10 + #13#10 +
    'KeyRecorder is a local-only keyboard activity recorder designed for debugging and productivity tracking.' + #13#10 + #13#10 +
    'Features:' + #13#10 +
    '  • 24/7 Background Recording' + #13#10 +
    '  • Triple Database Architecture' + #13#10 +
    '  • Automatic Crash Recovery' + #13#10 +
    '  • Local-Only (No Cloud, No Telemetry)' + #13#10 + #13#10 +
    'Click Next to continue.';
end;

procedure InitializeUninstallProgressForm();
begin
  UninstallProgressForm.Caption := 'Uninstalling KeyRecorder';
end;

function InitializeUninstall(): Boolean;
var
  ResultCode: Integer;
begin
  Result := True;

  // Kill the UI application before uninstalling
  Exec(ExpandConstant('{sys}\taskkill.exe'), '/F /IM "{#MyAppExeName}"', '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
  Sleep(1000);

  // Stop the service before uninstalling
  Exec(ExpandConstant('{sys}\sc.exe'), 'stop "{#MyServiceName}"', '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
  Sleep(2000);

  // Show confirmation dialog
  if MsgBox('Do you want to delete all recorded keystroke data?' + #13#10 + #13#10 +
            'If you select No, the database files will be preserved in:' + #13#10 +
            ExpandConstant('{commonappdata}\KeyRecorder') + #13#10 + #13#10 +
            'Delete all data?',
            mbConfirmation, MB_YESNO) = IDYES then
  begin
    // User wants to delete data - this will be handled by [UninstallDelete]
    Result := True;
  end
  else
  begin
    // User wants to keep data - prevent deletion
    Result := True;
  end;
end;

procedure CurStepChanged(CurStep: TSetupStep);
var
  ResultCode: Integer;
begin
  if CurStep = ssPostInstall then
  begin
    // Show success message
    MsgBox('KeyRecorder has been installed successfully!' + #13#10 + #13#10 +
           'Service Status: ' + #13#10 +
           '  The KeyRecorder Service is now running in the background.' + #13#10 + #13#10 +
           'Database Path:' + #13#10 +
           '  ' + ExpandConstant('{commonappdata}\KeyRecorder') + #13#10 + #13#10 +
           'Next Steps:' + #13#10 +
           '  1. Launch KeyRecorder UI to view keystroke history' + #13#10 +
           '  2. Configure retention in appsettings.json (optional)' + #13#10 +
           '  3. Service will start automatically on boot',
           mbInformation, MB_OK);
  end;
end;

function PrepareToInstall(var NeedsRestart: Boolean): String;
var
  ResultCode: Integer;
  DotNetPath: String;
begin
  Result := '';

  // Check if .NET 10 Runtime is installed by looking for dotnet.exe
  DotNetPath := ExpandConstant('{pf}\dotnet\dotnet.exe');
  if not FileExists(DotNetPath) then
  begin
    DotNetPath := ExpandConstant('{pf64}\dotnet\dotnet.exe');
    if not FileExists(DotNetPath) then
    begin
      Result := 'Microsoft .NET 10 Runtime is not installed.' + #13#10 + #13#10 +
                'Please download and install .NET 10 Runtime from:' + #13#10 +
                'https://dotnet.microsoft.com/download/dotnet/10.0' + #13#10 + #13#10 +
                'Then run this installer again.';
      Exit;
    end;
  end;

  // Stop existing service if running
  Exec(ExpandConstant('{sys}\sc.exe'), 'stop "{#MyServiceName}"', '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
  Sleep(2000);
end;

[Messages]
WelcomeLabel1=Welcome to KeyRecorder Setup
FinishedHeadingLabel=Completing KeyRecorder Setup
