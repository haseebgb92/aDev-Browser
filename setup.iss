[Setup]
AppId={{5F773BC4-3A41-4770-9844-4DE5D65A206A}
AppName=aDevBrowser
AppVersion=1.1
AppPublisher=advertpreneur
AppPublisherURL=https://elitewebtechnology.org
DefaultDirName={autopf}\aDevBrowser
DefaultGroupName=aDevBrowser
DisableProgramGroupPage=yes
OutputBaseFilename=aDevBrowser_Setup_v1.1
Compression=lzma2/max
SolidCompression=yes
ArchitecturesAllowed=x64
ArchitecturesInstallIn64BitMode=x64
SetupIconFile=DevBrowser\icon.ico
UninstallDisplayIcon={app}\DevBrowser.exe
OutputDir=Output

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
Source: "DevBrowser\bin\Release\net8.0-windows\win-x64\publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{autoprograms}\aDevBrowser"; Filename: "{app}\DevBrowser.exe"
Name: "{autodesktop}\aDevBrowser"; Filename: "{app}\DevBrowser.exe"; Tasks: desktopicon

[Run]
Filename: "{app}\DevBrowser.exe"; Description: "{cm:LaunchProgram,aDevBrowser}"; Flags: nowait postinstall skipifsilent
