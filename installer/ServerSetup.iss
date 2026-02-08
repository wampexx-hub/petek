; Petek Server Installer

#define MyAppName "Petek Server"
#define MyAppVersion "1.1.0"
#define MyAppPublisher "Petek"
#define MyAppExeName "Petek.Server.exe"

[Setup]
AppId={{E4C3B2A1-8D9E-4C0F-9F1A-3D2E5B6C7D8E}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes
OutputDir=..\publish\installer
OutputBaseFilename=PetekServerSetup
Compression=lzma2/ultra64
SolidCompression=yes
WizardStyle=modern
UninstallDisplayIcon={app}\{#MyAppExeName}
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
MinVersion=10.0
PrivilegesRequired=admin

[Languages]
Name: "turkish"; MessagesFile: "compiler:Languages\Turkish.isl"
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "firewall"; Description: "Windows Firewall'da 5000 portunu ac"; GroupDescription: "Ag ayarlari:"

[Files]
Source: "..\publish\server\{#MyAppExeName}"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\publish\server\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Dirs]
Name: "{app}\uploads"; Permissions: users-modify

[Icons]
Name: "{autoprograms}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"

[Run]
Filename: "netsh"; Parameters: "advfirewall firewall add rule name=""Petek Server"" dir=in action=allow protocol=tcp localport=5000"; Flags: runhidden; Tasks: firewall
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,Petek Server}"; Flags: nowait postinstall skipifsilent

[UninstallRun]
Filename: "netsh"; Parameters: "advfirewall firewall delete rule name=""Petek Server"""; Flags: runhidden
