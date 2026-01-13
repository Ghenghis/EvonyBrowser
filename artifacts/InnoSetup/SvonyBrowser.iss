[Setup]
AppName=Svony Browser
AppVersion=7.0.0
AppPublisher=Svony Browser Team
DefaultDirName={autopf}\SvonyBrowser
DefaultGroupName=Svony Browser
OutputDir=.
OutputBaseFilename=SvonyBrowser_v7.0.0_Setup
Compression=lzma2/ultra64
SolidCompression=yes
WizardStyle=modern

[Files]
Source: "files\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs

[Icons]
Name: "{group}\Svony Browser"; Filename: "{app}\SvonyBrowser.exe"
Name: "{commondesktop}\Svony Browser"; Filename: "{app}\SvonyBrowser.exe"

[Run]
Filename: "{app}\SvonyBrowser.exe"; Description: "Launch Svony Browser"; Flags: nowait postinstall skipifsilent
