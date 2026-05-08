#define MyAppName "Virtual Desktop Overlay"
#define MyAppVersion "0.1.1"
#define MyAppPublisher "dimar-hanung"
#define MyAppURL "https://github.com/dimar-hanung/Virtual-Desktop-Name-Overlay"

[Setup]
AppId={{70C5371A-7DE2-4762-A979-E595D739E152}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}
DefaultDirName={userappdata}\VirtualDesktopOverlay
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes
OutputDir=..\dist
OutputBaseFilename=VirtualDesktopOverlaySetup-{#MyAppVersion}
Compression=lzma
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=lowest
UninstallDisplayName={#MyAppName}
VersionInfoVersion={#MyAppVersion}
VersionInfoCompany={#MyAppPublisher}
VersionInfoDescription={#MyAppName} installer

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "startup"; Description: "Start with Windows"; GroupDescription: "Startup:"; Flags: checkedonce
Name: "startmenu"; Description: "Create Start Menu shortcut"; GroupDescription: "Shortcuts:"; Flags: checkedonce

[Files]
Source: "..\VirtualDesktopOverlay.ps1"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\StopVirtualDesktopOverlay.ps1"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\README.md"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\THIRD_PARTY_NOTICES.md"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\modules\VirtualDesktop\DEPENDENCY.txt"; DestDir: "{app}\modules\VirtualDesktop"; Flags: ignoreversion
Source: "..\modules\VirtualDesktop\1.5.11\*"; DestDir: "{app}\modules\VirtualDesktop\1.5.11"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{sys}\WindowsPowerShell\v1.0\powershell.exe"; Parameters: "-NoProfile -STA -WindowStyle Minimized -File ""{app}\VirtualDesktopOverlay.ps1"""; WorkingDir: "{app}"; Tasks: startmenu

[Registry]
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; ValueType: string; ValueName: "VirtualDesktopOverlay"; ValueData: """{sys}\WindowsPowerShell\v1.0\powershell.exe"" -NoProfile -STA -WindowStyle Minimized -File ""{app}\VirtualDesktopOverlay.ps1"""; Flags: uninsdeletevalue; Tasks: startup

[Run]
Filename: "{sys}\WindowsPowerShell\v1.0\powershell.exe"; Parameters: "-NoProfile -STA -WindowStyle Minimized -File ""{app}\VirtualDesktopOverlay.ps1"""; WorkingDir: "{app}"; Description: "Launch {#MyAppName}"; Flags: postinstall nowait skipifsilent runasoriginaluser

[UninstallRun]
Filename: "{sys}\WindowsPowerShell\v1.0\powershell.exe"; Parameters: "-NoProfile -File ""{app}\StopVirtualDesktopOverlay.ps1"" -InstallPath ""{app}"""; Flags: waituntilterminated; RunOnceId: "StopVirtualDesktopOverlay"

[UninstallDelete]
Type: filesandordirs; Name: "{localappdata}\VirtualDesktopOverlay"
