; Petek Server Installer - Interaktif Yapılandırma ile

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
LicenseFile=
InfoBeforeFile=

[Languages]
Name: "turkish"; MessagesFile: "compiler:Languages\Turkish.isl"
Name: "english"; MessagesFile: "compiler:Default.isl"

[CustomMessages]
turkish.ServerConfig=Sunucu Yapılandırması
turkish.ServerConfigDesc=Sunucu bağlantı ayarlarını yapılandırın
turkish.ServerPort=Sunucu Portu:
turkish.AdminUser=Yönetici Kullanıcı Adı:
turkish.AdminPass=Yönetici Şifresi:
turkish.DatabaseType=Veritabanı Türü:
turkish.InstallService=Windows Servisi olarak kur
turkish.OpenFirewall=Firewall'da portu aç
english.ServerConfig=Server Configuration
english.ServerConfigDesc=Configure server connection settings
english.ServerPort=Server Port:
english.AdminUser=Admin Username:
english.AdminPass=Admin Password:
english.DatabaseType=Database Type:
english.InstallService=Install as Windows Service
english.OpenFirewall=Open port in firewall

[Tasks]
Name: "firewall"; Description: "{cm:OpenFirewall}"; GroupDescription: "Ağ ayarları:"; Flags: checkedonce
Name: "service"; Description: "{cm:InstallService}"; GroupDescription: "Servis ayarları:"; Flags: checkedonce

[Files]
Source: "..\publish\server_full\{#MyAppExeName}"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\publish\server_full\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Dirs]
Name: "{app}\uploads"; Permissions: users-modify
Name: "{app}\logs"; Permissions: users-modify

[Icons]
Name: "{autoprograms}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{autoprograms}\Petek Admin Panel"; Filename: "http://localhost:{code:GetPort}/admin"

[Run]
; Firewall kuralı
Filename: "netsh"; Parameters: "advfirewall firewall add rule name=""Petek Server"" dir=in action=allow protocol=tcp localport={code:GetPort}"; Flags: runhidden; Tasks: firewall
; Windows Service oluştur
Filename: "sc.exe"; Parameters: "create PetekServer binPath= ""{app}\{#MyAppExeName}"" start= auto DisplayName= ""Petek Messenger Server"""; Flags: runhidden; Tasks: service
Filename: "sc.exe"; Parameters: "description PetekServer ""Petek Messenger - Kurumsal mesajlaşma sunucusu"""; Flags: runhidden; Tasks: service
; Servisi başlat
Filename: "net"; Parameters: "start PetekServer"; Flags: runhidden; Tasks: service
; Servis olmadan başlat
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,Petek Server}"; Flags: nowait postinstall skipifsilent; Tasks: not service
; Bağlantı bilgilerini göster
Filename: "notepad.exe"; Parameters: "{app}\CONNECTION_INFO.txt"; Flags: nowait postinstall skipifsilent shellexec

[UninstallRun]
Filename: "net"; Parameters: "stop PetekServer"; Flags: runhidden
Filename: "sc.exe"; Parameters: "delete PetekServer"; Flags: runhidden
Filename: "netsh"; Parameters: "advfirewall firewall delete rule name=""Petek Server"""; Flags: runhidden

[Code]
var
  ConfigPage: TInputQueryWizardPage;
  AdminPage: TInputQueryWizardPage;
  DbCombo: TNewComboBox;

procedure InitializeWizard;
begin
  // Sunucu yapılandırma sayfası
  ConfigPage := CreateInputQueryPage(wpSelectTasks,
    ExpandConstant('{cm:ServerConfig}'),
    ExpandConstant('{cm:ServerConfigDesc}'),
    'Aşağıdaki ayarları yapılandırın:');
  ConfigPage.Add(ExpandConstant('{cm:ServerPort}'), False);
  ConfigPage.Values[0] := '5000';

  // Admin hesabı sayfası
  AdminPage := CreateInputQueryPage(ConfigPage.ID,
    'Yönetici Hesabı',
    'İlk yönetici hesabını oluşturun',
    'Bu bilgiler sunucuya ilk giriş için kullanılacaktır:');
  AdminPage.Add(ExpandConstant('{cm:AdminUser}'), False);
  AdminPage.Add(ExpandConstant('{cm:AdminPass}'), True);
  AdminPage.Values[0] := 'admin';
  AdminPage.Values[1] := '';
end;

function GetPort(Param: String): String;
begin
  Result := ConfigPage.Values[0];
  if Result = '' then
    Result := '5000';
end;

function GetAdminUser: String;
begin
  Result := AdminPage.Values[0];
  if Result = '' then
    Result := 'admin';
end;

function GetAdminPass: String;
begin
  Result := AdminPage.Values[1];
end;

function GetLocalIP: String;
var
  WbemLocator, WbemServices, WbemObjectSet, WbemObject: Variant;
  IPs: Variant;
  i: Integer;
begin
  Result := '127.0.0.1';
  try
    WbemLocator := CreateOleObject('WbemScripting.SWbemLocator');
    WbemServices := WbemLocator.ConnectServer('localhost', 'root\CIMV2');
    WbemObjectSet := WbemServices.ExecQuery(
      'SELECT IPAddress FROM Win32_NetworkAdapterConfiguration WHERE IPEnabled = True');
    for i := 0 to WbemObjectSet.Count - 1 do
    begin
      WbemObject := WbemObjectSet.ItemIndex(i);
      if not VarIsNull(WbemObject.IPAddress) then
      begin
        IPs := WbemObject.IPAddress;
        Result := IPs[0];
        if (Result <> '') and (Result <> '127.0.0.1') then
          Break;
      end;
    end;
  except
  end;
end;

procedure GenerateConfig;
var
  Port, AdminUser, AdminPass, IP: String;
  ConfigLines: TArrayOfString;
  InfoLines: TArrayOfString;
begin
  Port := GetPort('');
  AdminUser := GetAdminUser;
  AdminPass := GetAdminPass;
  IP := GetLocalIP;

  // appsettings.json oluştur
  SetArrayLength(ConfigLines, 35);
  ConfigLines[0] := '{';
  ConfigLines[1] := '  "Logging": {';
  ConfigLines[2] := '    "LogLevel": {';
  ConfigLines[3] := '      "Default": "Information",';
  ConfigLines[4] := '      "Microsoft.AspNetCore": "Warning"';
  ConfigLines[5] := '    }';
  ConfigLines[6] := '  },';
  ConfigLines[7] := '  "AllowedHosts": "*",';
  ConfigLines[8] := '  "Database": {';
  ConfigLines[9] := '    "Type": "SQLite"';
  ConfigLines[10] := '  },';
  ConfigLines[11] := '  "ConnectionStrings": {';
  ConfigLines[12] := '    "SQLite": "Data Source=petek.db"';
  ConfigLines[13] := '  },';
  ConfigLines[14] := '  "SSO": {';
  ConfigLines[15] := '    "Enabled": false,';
  ConfigLines[16] := '    "WindowsAuth": false';
  ConfigLines[17] := '  },';
  ConfigLines[18] := '  "Cors": {';
  ConfigLines[19] := '    "Origins": [';
  ConfigLines[20] := '      "http://localhost:' + Port + '",';
  ConfigLines[21] := '      "http://' + IP + ':' + Port + '"';
  ConfigLines[22] := '    ]';
  ConfigLines[23] := '  },';
  ConfigLines[24] := '  "FileStorage": {';
  ConfigLines[25] := '    "Path": "' + ExpandConstant('{app}') + '\\uploads",';
  ConfigLines[26] := '    "MaxFileSizeBytes": 104857600';
  ConfigLines[27] := '  },';
  ConfigLines[28] := '  "Kestrel": {';
  ConfigLines[29] := '    "Endpoints": {';
  ConfigLines[30] := '      "Http": { "Url": "http://0.0.0.0:' + Port + '" }';
  ConfigLines[31] := '    }';
  ConfigLines[32] := '  },';
  ConfigLines[33] := '  "AdminSetup": { "Username": "' + AdminUser + '", "Password": "' + AdminPass + '" }';
  ConfigLines[34] := '}';
  SaveStringsToFile(ExpandConstant('{app}') + '\appsettings.json', ConfigLines, False);

  // CONNECTION_INFO.txt oluştur
  SetArrayLength(InfoLines, 22);
  InfoLines[0] := '=============================================';
  InfoLines[1] := 'PETEK MESSENGER SUNUCU BAGLANTI BILGILERI';
  InfoLines[2] := '=============================================';
  InfoLines[3] := '';
  InfoLines[4] := 'Sunucu Adı : ' + GetComputerNameString;
  InfoLines[5] := 'Port       : ' + Port;
  InfoLines[6] := '';
  InfoLines[7] := 'ISTEMCI BAGLANTI ADRESLERI:';
  InfoLines[8] := '';
  InfoLines[9] := '  http://' + IP + ':' + Port;
  InfoLines[10] := '  http://' + GetComputerNameString + ':' + Port;
  InfoLines[11] := '  http://localhost:' + Port;
  InfoLines[12] := '';
  InfoLines[13] := 'ENDPOINTLER:';
  InfoLines[14] := '  API        : http://' + IP + ':' + Port + '/api';
  InfoLines[15] := '  SignalR Hub: http://' + IP + ':' + Port + '/hubs/message';
  InfoLines[16] := '  Admin Panel: http://' + IP + ':' + Port + '/admin';
  InfoLines[17] := '  Saglik     : http://' + IP + ':' + Port + '/health';
  InfoLines[18] := '';
  InfoLines[19] := 'YONETICI HESABI:';
  InfoLines[20] := '  Kullanici  : ' + AdminUser;
  InfoLines[21] := '  Sifre      : (kurulumda belirlediginiz sifre)';
  SaveStringsToFile(ExpandConstant('{app}') + '\CONNECTION_INFO.txt', InfoLines, False);
end;

procedure CurStepChanged(CurStep: TSetupStep);
begin
  if CurStep = ssPostInstall then
  begin
    GenerateConfig;
  end;
end;

function NextButtonClick(CurPageID: Integer): Boolean;
begin
  Result := True;
  // Admin şifre kontrolü
  if CurPageID = AdminPage.ID then
  begin
    if (AdminPage.Values[1] <> '') and (Length(AdminPage.Values[1]) < 8) then
    begin
      MsgBox('Şifre en az 8 karakter olmalıdır.', mbError, MB_OK);
      Result := False;
    end;
  end;
end;
