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
; The build script places the assembled payload into the `publish` subfolder of this directory.
; Package everything under the `publish` folder into the application directory.
Source: "publish\\*"; DestDir: "{app}"; Flags: recursesubdirs createallsubdirs

[Icons]
; Icons point to the installed Frontend executable and a helper to start the backend.
Name: "{group}\Stable Diffusion Desktop App"; Filename: "{app}\\Frontend\\myApp.exe"
Name: "{group}\Run Backend (first-run will setup venv)"; Filename: "{app}\\run_backend.bat"
Name: "{commondesktop}\Stable Diffusion Desktop App"; Filename: "{app}\\Frontend\\myApp.exe"; Tasks: desktopicon

[Tasks]
Name: desktopicon; Description: "Create a &desktop icon"; GroupDescription: "Additional icons:"; Flags: unchecked

[Run]
; Use cmd.exe to explicitly run the batch file so the working directory and PATH are set as expected.
; `nowait` keeps the installer from blocking while the backend starts. `postinstall` runs this only when the user selects the option to run.
Filename: "cmd.exe"; Parameters: "/C ""{app}\\run_backend.bat"""; WorkingDir: "{app}"; Description: "Start backend after installation"; Flags: nowait postinstall skipifsilent
