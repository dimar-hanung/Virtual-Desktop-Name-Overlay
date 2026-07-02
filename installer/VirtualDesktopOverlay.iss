#define MyAppName "Virtual Desktop Overlay"
#define MyAppVersion "0.3.2"
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
CloseApplications=yes
CloseApplicationsFilter=VirtualDesktopOverlay.exe

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "startup"; Description: "Start with Windows"; GroupDescription: "Startup:"; Flags: checkedonce
Name: "startmenu"; Description: "Create Start Menu shortcut"; GroupDescription: "Shortcuts:"; Flags: checkedonce

[Files]
Source: "..\publish\win-x64\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "..\README.md"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\THIRD_PARTY_NOTICES.md"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\VirtualDesktopOverlay.exe"; WorkingDir: "{app}"; Tasks: startmenu

[Registry]
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; ValueType: string; ValueName: "VirtualDesktopOverlay"; ValueData: """{app}\VirtualDesktopOverlay.exe"""; Flags: uninsdeletevalue; Tasks: startup

[Run]
Filename: "{app}\VirtualDesktopOverlay.exe"; WorkingDir: "{app}"; Description: "Launch {#MyAppName}"; Flags: postinstall nowait skipifsilent runasoriginaluser

[UninstallDelete]
Type: filesandordirs; Name: "{localappdata}\VirtualDesktopOverlay"
