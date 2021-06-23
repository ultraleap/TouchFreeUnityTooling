; Script generated by the Inno Setup Script Wizard.
; SEE THE DOCUMENTATION FOR DETAILS ON CREATING INNO SETUP SCRIPT FILES!

#define CompanyURL "https://ultraleap.com"
#define ProductName "TouchFree"
#define Publisher "Ultraleap Inc."
#define ReleaseVersion "2.0.0-beta1"
#define ServiceUIExeName "TouchFreeServiceUI.exe"
#define ServiceUIName "TouchFree Service Settings"
#define TouchFreeAppExeName "TouchFree_Application.exe"
#define TouchFreeAppName "TouchFree Application"
#define TrayAppExeName "ServiceUITray.exe"
#define TrayAppName "TouchFree Service Control Panel"
#define WrapperExeName "ServiceWrapper.exe"

[Setup]
; NOTE: The value of AppId uniquely identifies this application. Do not use the same AppId value in installers for other applications.
; (To generate a new GUID, click Tools | Generate GUID inside the IDE.)
AppId={{FE678A79-DE6B-4FC4-8160-F2D036CE27D2}
AppName={#ProductName}
AppVersion={#ReleaseVersion}
AppVerName={#ProductName}
AppPublisher={#Publisher}
AppPublisherURL={#CompanyURL}
AppSupportURL={#CompanyURL}
AppUpdatesURL={#CompanyURL}
CreateUninstallRegKey=yes
DefaultDirName={autopf64}\Ultraleap\{#ProductName}
DisableProgramGroupPage=yes
LicenseFile={#SourcePath}..\..\LICENSE
SetupIconFile={#SourcePath}..\..\TouchFree_Icon.ico
; Uncomment the following line to run in non administrative install mode (install for current user only.)
;PrivilegesRequired=lowest
OutputDir="{#SourcePath}..\..\Installer_Build"
OutputBaseFilename={#ProductName}_{#ReleaseVersion}_Installer
Compression=lzma
SolidCompression=yes
VersionInfoCompany={#Publisher}
VersionInfoProductName={#ProductName}
UpdateUninstallLogAppName=no
VersionInfoVersion=1.0.0.4
WizardStyle=modern
PrivilegesRequired=admin

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: TouchFree_Application; Description: "Install the TouchFree Application";

[Files]
Source: "{#SourcePath}..\..\Service_Package\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "{#SourcePath}..\..\TouchFree_Build\*"; DestDir: "{app}\TouchFree"; Tasks: TouchFree_Application; Flags: ignoreversion recursesubdirs createallsubdirs
; NOTE: Don't use "Flags: ignoreversion" on any shared system files

[Icons]
Name: "{autoprograms}\{#TouchFreeAppName}"; Filename: "{app}\TouchFree\{#TouchFreeAppExeName}"; Tasks: TouchFree_Application;
Name: "{autoprograms}\{#ServiceUIName}"; Filename: "{app}\ServiceUI\{#ServiceUIExeName}";
Name: "{autostartup}\{#TrayAppName}"; Filename: "{app}\Tray\{#TrayAppExeName}";

[Registry]
Root: HKA64; Subkey: "Software\Ultraleap"; Flags: uninsdeletekeyifempty
Root: HKA64; Subkey: "Software\Ultraleap\TouchFree"; Flags: uninsdeletekeyifempty
Root: HKA64; Subkey: "Software\Ultraleap\TouchFree\Service"; Flags: uninsdeletekey
Root: HKA64; Subkey: "Software\Ultraleap\TouchFree\Service\Settings"; ValueType: string; ValueName: "WrapperExePath"; ValueData: "{app}\Wrapper\{#WrapperExeName}"

[Run]
Filename: "{app}\ServiceUI\{#ServiceUIExeName}"; Description: "{cm:LaunchProgram,{#StringChange(ServiceUIName, '&', '&&')}}"; Tasks: not TouchFree_Application; Flags: runascurrentuser nowait postinstall skipifsilent
Filename: "{app}\TouchFree\{#TouchFreeAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(TouchFreeAppName, '&', '&&')}}"; Tasks: TouchFree_Application; Flags: runascurrentuser nowait postinstall skipifsilent
Filename: "{app}\Tray\{#TrayAppExeName}"; Flags: runhidden nowait;
Filename: "{app}\Wrapper\{#WrapperExeName}"; Parameters: "install"; Flags: runhidden
Filename: "net.exe"; Parameters: "start ""TouchFree Service"""; Flags: runhidden

[UninstallRun]
Filename: "{cmd}"; Parameters: "/C taskkill /im ServiceUITray.exe /f /t"; RunOnceId: "StopTrayIconApp"; Flags: runhidden
Filename: "net.exe"; Parameters: "stop ""TouchFree Service"""; RunOnceId: "StopService"; Flags: runhidden
Filename: "{app}\Wrapper\{#WrapperExeName}"; Parameters: "uninstall"; RunOnceId: "UninstallService"; Flags: runhidden

[Code]
function GetWrapperPath: string;
var
  wrapperExePath: string;
  wrapperRegistryPath: String;
begin
  Result := '';
  wrapperRegistryPath := ExpandConstant('Software\Ultraleap\TouchFree\Service\Settings');
  wrapperExePath := '';
  if not RegQueryStringValue(HKLM64, wrapperRegistryPath, 'WrapperExePath', wrapperExePath) then
    RegQueryStringValue(HKCU64, wrapperRegistryPath, 'WrapperExePath', wrapperExePath);
  Result := wrapperExePath;
end;

function PrepareToInstall(var NeedsRestart: Boolean): String;
var
  ResultCode: integer;
  WrapperPath: string;
begin
  WrapperPath := GetWrapperPath();

  Log(WrapperPath);

  if CompareText(WrapperPath, '') > 0 then
  begin
    Exec('cmd', '/C taskkill /im ServiceUITray.exe /f /t', '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
    Exec('net', 'stop "TouchFree Service"', '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
    Exec(ExpandConstant(WrapperPath), 'uninstall', '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
  end;

  // Proceed Setup
  Result := '';
end;
