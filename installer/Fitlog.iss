#define AppName "Fitlog"
#define AppPublisher "Fitlog"
#ifndef AppVersion
  #define AppVersion "1.3.0"
#endif
#ifndef PublishDir
  #define PublishDir "..\artifacts\publish\win-x64"
#endif
#ifndef BrandingDir
  #define BrandingDir "Branding"
#endif

[Setup]
AppId={{7B4469E9-30BE-4A77-9BB4-4ECA9CC85877}
AppName={#AppName}
AppVersion={#AppVersion}
AppVerName={#AppName} {#AppVersion}
AppPublisher={#AppPublisher}
AppPublisherURL=https://github.com/iraqi-man1/first-c-
AppSupportURL=https://github.com/iraqi-man1/first-c-/issues
AppUpdatesURL=https://github.com/iraqi-man1/first-c-/releases
DefaultDirName={localappdata}\Programs\Fitlog
DefaultGroupName=Fitlog
DisableProgramGroupPage=yes
PrivilegesRequired=lowest
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
UninstallDisplayName={#AppName}
UninstallDisplayIcon={app}\Fitlog.exe
OutputDir=..\artifacts\installer
OutputBaseFilename=Fitlog-Setup-{#AppVersion}-win-x64
SetupIconFile=..\Fitlog\Assets\fitlog.ico
WizardImageFile={#BrandingDir}\FitlogWizard.png
WizardSmallImageFile={#BrandingDir}\FitlogWizardSmall.png
VersionInfoVersion={#AppVersion}
VersionInfoProductName={#AppName}
VersionInfoProductVersion={#AppVersion}
VersionInfoDescription=Fitlog Windows Installer
VersionInfoCompany={#AppPublisher}
Compression=lzma2
SolidCompression=yes
WizardStyle=modern dynamic hidebevels
CloseApplications=yes
CloseApplicationsFilter=Fitlog.exe
RestartApplications=no
SetupLogging=yes
UsePreviousAppDir=yes
UsePreviousTasks=yes
UsePreviousLanguage=yes
ShowLanguageDialog=yes

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"
Name: "arabic"; MessagesFile: "compiler:Languages\Arabic.isl"

[LangOptions]
arabic.RightToLeft=yes

[Messages]
english.WelcomeLabel1=Welcome to Fitlog
english.WelcomeLabel2=Organize your training, meals, and progress in one place.%nYour data stays on this PC.
english.FinishedHeadingLabel=Fitlog is ready
english.FinishedLabel=The update is complete. Choose Launch Fitlog to open your updated app.
arabic.WelcomeLabel1=مرحباً بك في Fitlog
arabic.WelcomeLabel2=نظّم تمارينك ووجباتك وتقدمك بمكان واحد.%nتبقى بياناتك على هذا الجهاز.
arabic.FinishedHeadingLabel=Fitlog جاهز
arabic.FinishedLabel=اكتمل التحديث. اختر تشغيل Fitlog لفتح التطبيق المحدّث.

[CustomMessages]
english.AdditionalShortcuts=Additional shortcuts:
english.CreateDesktopIcon=Create a desktop shortcut
arabic.AdditionalShortcuts=اختصارات إضافية:
arabic.CreateDesktopIcon=إنشاء اختصار على سطح المكتب

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalShortcuts}"; Flags: unchecked

[Files]
Source: "{#PublishDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs; Excludes: "*.pdb"

[Icons]
Name: "{autoprograms}\Fitlog"; Filename: "{app}\Fitlog.exe"; WorkingDir: "{app}"; IconFilename: "{app}\Fitlog.exe"
Name: "{autodesktop}\Fitlog"; Filename: "{app}\Fitlog.exe"; WorkingDir: "{app}"; IconFilename: "{app}\Fitlog.exe"; Tasks: desktopicon

[Run]
Filename: "{app}\Fitlog.exe"; Description: "Launch Fitlog"; Flags: nowait postinstall skipifsilent; WorkingDir: "{app}"
