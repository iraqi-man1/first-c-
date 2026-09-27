#define AppName "Fitlog"
#define AppPublisher "Fitlog"
#ifndef AppVersion
  #define AppVersion "1.0.0"
#endif
#ifndef PublishDir
  #define PublishDir "..\artifacts\publish\win-x64"
#endif

[Setup]
AppId={{7B4469E9-30BE-4A77-9BB4-4ECA9CC85877}
AppName={#AppName}
AppVersion={#AppVersion}
AppVerName={#AppName} {#AppVersion}
AppPublisher={#AppPublisher}
DefaultDirName={localappdata}\Programs\Fitlog
DefaultGroupName=Fitlog
DisableProgramGroupPage=yes
PrivilegesRequired=lowest
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
OutputDir=..\artifacts\installer
OutputBaseFilename=Fitlog-{#AppVersion}-Setup
SetupIconFile=..\Fitlog\Assets\fitlog.ico
UninstallDisplayIcon={app}\Fitlog.exe
VersionInfoVersion={#AppVersion}
VersionInfoProductName={#AppName}
VersionInfoProductVersion={#AppVersion}
VersionInfoDescription=Fitlog Setup
VersionInfoCompany={#AppPublisher}
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
CloseApplications=yes
RestartApplications=no
UsePreviousAppDir=yes
UsePreviousTasks=yes

[Tasks]
Name: "desktopicon"; Description: "Create a desktop shortcut"; GroupDescription: "Additional shortcuts:"; Flags: unchecked

[Files]
Source: "{#PublishDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs; Excludes: "*.pdb"

[Icons]
Name: "{autoprograms}\Fitlog"; Filename: "{app}\Fitlog.exe"; IconFilename: "{app}\Fitlog.exe"
Name: "{autodesktop}\Fitlog"; Filename: "{app}\Fitlog.exe"; IconFilename: "{app}\Fitlog.exe"; Tasks: desktopicon

[Run]
Filename: "{app}\Fitlog.exe"; Description: "Launch Fitlog"; Flags: nowait postinstall skipifsilent
