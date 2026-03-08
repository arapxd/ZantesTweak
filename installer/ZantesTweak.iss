; Zantes Tweak installer script
; Build with: ISCC installer\ZantesTweak.iss

#define MyAppName "Zantes Tweak"
#ifndef MyAppVersion
  #define MyAppVersion "1.0.0"
#endif
#define MyAppPublisher "Zantes"
#define MyAppBrandCredit "Made by Arap"
#define MyAppExeName "ZantesEngine.exe"
#ifndef MyOutputBaseFilename
  #define MyOutputBaseFilename "ZantesTweak-Setup"
#endif
#ifndef MySourceDir
  #define MySourceDir "..\\release\\win-x64"
#endif

[Setup]
AppId={{8A8D68B2-B4C3-4C2B-9F7E-1F783F3C9D22}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppCopyright={#MyAppBrandCredit}
VersionInfoDescription=Zantes Tweak Installer
VersionInfoCompany={#MyAppPublisher}
VersionInfoCopyright={#MyAppBrandCredit}
DefaultDirName={autopf}\\{#MyAppName}
DefaultGroupName={#MyAppName}
AllowNoIcons=yes
OutputDir=..\\release\\installer
OutputBaseFilename={#MyOutputBaseFilename}
Compression=lzma
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=admin
ArchitecturesInstallIn64BitMode=x64compatible
ArchitecturesAllowed=x64compatible
SetupIconFile=..\\ZantesEngine\\appicon.ico
UninstallDisplayIcon={app}\\{#MyAppExeName}
LicenseFile=..\\EULA.txt

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"
Name: "turkish"; MessagesFile: "compiler:Languages\\Turkish.isl"

[Tasks]
Name: "desktopicon"; Description: "Create a desktop icon"; GroupDescription: "Additional icons:"; Flags: unchecked

[Files]
Source: "{#MySourceDir}\\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "..\\LICENSE.txt"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\\EULA.txt"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{group}\\{#MyAppName}"; Filename: "{app}\\{#MyAppExeName}"
Name: "{group}\\Uninstall {#MyAppName}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\\{#MyAppName}"; Filename: "{app}\\{#MyAppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\\{#MyAppExeName}"; Description: "Launch {#MyAppName}"; Flags: nowait postinstall skipifsilent
