; Inno Setup script for IsoLaunch
#ifndef AppVersion
  #define AppVersion "1.0.0"
#endif

#ifndef PublishDir
  #define PublishDir "..\artifacts\publish\win-x64"
#endif

#ifndef OutputDir
  #define OutputDir "..\dist"
#endif

[Setup]
AppId={{8E9F4A2B-7C1D-4E5A-9B3F-1D2E3F4A5B6C}
AppName=IsoLaunch
AppVersion={#AppVersion}
AppVerName=IsoLaunch {#AppVersion}
AppPublisher=IsoLaunch
DefaultDirName={autopf}\IsoLaunch
DefaultGroupName=IsoLaunch
AllowNoIcons=yes
PrivilegesRequired=lowest
PrivilegesRequiredOverridesAllowed=dialog
OutputDir={#OutputDir}
OutputBaseFilename=IsoLaunch-Setup-{#AppVersion}-win-x64
Compression=lzma2/ultra64
SolidCompression=yes
WizardStyle=modern
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
SetupIconFile=..\IsoLaunch.App\Assets\app.ico
UninstallDisplayIcon={app}\IsoLaunch.App.exe

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
Source: "{#PublishDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\IsoLaunch"; Filename: "{app}\IsoLaunch.App.exe"
Name: "{group}\{cm:UninstallProgram,IsoLaunch}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\IsoLaunch"; Filename: "{app}\IsoLaunch.App.exe"; Tasks: desktopicon

[Run]
Filename: "{app}\IsoLaunch.App.exe"; Description: "{cm:LaunchProgram,IsoLaunch}"; Flags: nowait postinstall skipifsilent
