[Setup]
AppId={{5CF91F71-3F8C-4F74-A46A-F1E8CBFD5E3C}
AppName=Stable Diffusion Desktop App
AppVersion=1.0
AppPublisher=Spicy Banana
DefaultDirName={localappdata}\Stable Diffusion Desktop App
DefaultGroupName=Stable Diffusion Desktop App
PrivilegesRequired=lowest
OutputDir=installer_output
OutputBaseFilename=StableDiffusionDesktopApp_Setup
Compression=lzma2/max
SolidCompression=yes
SetupIconFile=
WizardStyle=modern

[Files]
; Embedded Python environment (much smaller than venv!)
Source: "..\python-embedded\*"; DestDir: "{app}\python-embedded"; Flags: ignoreversion recursesubdirs createallsubdirs

; Backend application (excluding large model files completely)
Source: "..\backend\*"; DestDir: "{app}\backend"; Flags: ignoreversion recursesubdirs createallsubdirs; Excludes: "webui-venv,fastAPI,models,__pycache__,*.pyc,*.pyo"

; Include essential code modules that were excluded above
Source: "..\backend\modules\models\*"; DestDir: "{app}\backend\modules\models"; Flags: ignoreversion recursesubdirs createallsubdirs

; Create empty model directories for download script to use
Source: "..\backend\models\*"; DestDir: "{app}\backend\models"; Flags: ignoreversion recursesubdirs createallsubdirs; Excludes: "*.ckpt,*.safetensors,*.pt,*.pth,*.bin"

; Frontend application  
Source: "..\myApp\bin\Release\net9.0\publish\*"; DestDir: "{app}\frontend"; Flags: ignoreversion recursesubdirs createallsubdirs

; Simplified launcher scripts
Source: "batch_scripts_embedded\*"; DestDir: "{app}"; Flags: ignoreversion

[Dirs]

[Icons]
Name: "{group}\Stable Diffusion Desktop App"; Filename: "{app}\launch_app.bat"
Name: "{group}\Launch Backend Only"; Filename: "{app}\launch_backend.bat"
Name: "{group}\Download Models"; Filename: "{app}\download_models.bat"
Name: "{commondesktop}\Stable Diffusion Desktop App"; Filename: "{app}\launch_app.bat"; Tasks: desktopicon

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"

[Run]
; Create models directory and run model download
Filename: "{app}\download_models.bat"; Parameters: ""; WorkingDir: "{app}"; Flags: runascurrentuser postinstall; Description: "Download example models (recommended)"

; Launch the application
Filename: "{app}\launch_app.bat"; Parameters: ""; WorkingDir: "{app}"; Flags: runascurrentuser postinstall nowait; Description: "Launch Stable Diffusion Desktop App"

[Code]
procedure CurStepChanged(CurStep: TSetupStep);
begin
  if CurStep = ssPostInstall then
  begin
    // Create AppData models directory
    CreateDir(ExpandConstant('{userappdata}\StableDiffusion\models\Stable-diffusion'));
    CreateDir(ExpandConstant('{userappdata}\StableDiffusion\models\VAE'));
    CreateDir(ExpandConstant('{userappdata}\StableDiffusion\models\LoRA'));
  end;
end;