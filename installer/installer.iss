[Setup]
AppName=Stable Diffusion Desktop App
AppVersion=1.0.0
DefaultDirName={pf}\Stable Diffusion Desktop App
DefaultGroupName=Stable Diffusion Desktop App
OutputDir=..\installer_output
OutputBaseFilename=StableDiffusionDesktopApp-Setup-1.0.0
Compression=lzma
SolidCompression=yes
AllowNoIcons=yes

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Files]
; All files collected into the installer/publish folder by the build script
Source: "*"; DestDir: "{app}"; Flags: recursesubdirs createallsubdirs

[Icons]
Name: "{group}\Stable Diffusion Desktop App"; Filename: "{app}\Frontend\myApp.exe"
Name: "{group}\Run Backend (first-run will setup venv)"; Filename: "{app}\run_backend.bat"
Name: "{commondesktop}\Stable Diffusion Desktop App"; Filename: "{app}\Frontend\myApp.exe"; Tasks: desktopicon

[Tasks]
Name: desktopicon; Description: "Create a &desktop icon"; GroupDescription: "Additional icons:"; Flags: unchecked

[Run]
Filename: "{app}\run_backend.bat"; Description: "Start backend after installation"; Flags: nowait postinstall skipifsilent
