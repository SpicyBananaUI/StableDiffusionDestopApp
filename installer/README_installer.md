# Windows Installer Builder

This folder contains helper files to build an Inno Setup installer for the Stable Diffusion Desktop App.

## What the Scripts Do

- **`build_installer.ps1`** - Main build script that:
  - Publishes the .NET frontend as a single-file EXE (win-x64)
  - Copies and optimizes the backend folder 
  - Creates a minimal Python virtual environment
  - Generates helper batch scripts (`run_frontend.bat`, `run_backend.bat`, `download_models.bat`)
  - Optionally compiles the final installer EXE

- **`installer.iss`** - Inno Setup script that packages everything under `installer/publish` into a single installer EXE with Start Menu and Desktop shortcuts

## Prerequisites

1. **.NET SDK** - Make sure `dotnet` is available on PATH
2. **Python 3.10 or 3.11** - Required for creating the minimal virtual environment  
3. **Inno Setup** (Optional) - For automatic compilation to final installer EXE

## Build Options

### Ultra-Compact Installer (~2.3GB)
**Recommended for distribution**
```powershell
cd installer
.\build_installer.ps1 -CreateMinimalVenv
```
- Creates optimized Python environment with only essential packages
- Excludes models (users download post-install) 
- Excludes repositories/cache (downloaded on first run)
- Uses Python 3.10/3.11 automatically for best compatibility

### Include Specific Model (~4-5GB)
```powershell
cd installer  
.\build_installer.ps1 -CreateMinimalVenv -SpecificModels @('dreamshaper')
```
- Includes DreamShaper model for immediate use
- Still creates minimal Python environment

### Specify Python Version
```powershell
cd installer
.\build_installer.ps1 -CreateMinimalVenv -PythonPath "C:\Python310\python.exe"
```
- Forces use of specific Python installation
- Useful if auto-detection selects wrong version

### Full Build (Not Recommended - 21GB+)
```powershell  
cd installer
.\build_installer.ps1 -IncludeModels -IncludeRepositories -IncludeFullVenv
```
- Includes everything - may exceed Inno Setup size limits

## Manual Compilation

If `ISCC.exe` is not on PATH, the script will prepare files but not compile. To compile manually:

1. Install [Inno Setup](https://jrsoftware.org/isdl.php)
2. Right-click on `installer.iss` → "Compile"
3. Or run: `"C:\Program Files (x86)\Inno Setup 6\ISCC.exe" installer.iss`

The compiled installer will be placed in `../installer_output/`

## Post-Install User Experience  

The installer creates shortcuts for:
- **Stable Diffusion Desktop App** - Launches the frontend
- **Run Backend** - Starts the backend server (with model download prompts if needed)
- **Download Models** - Helper script to download additional models

On first backend run, users will be prompted to download models if none are included in the installer.

## Build Output Structure

```
installer/publish/
├── Frontend/
│   └── myApp.exe                 # .NET desktop app  
├── backend/                      # Python backend
│   ├── webui-venv/              # Minimal virtual environment
│   ├── (core backend files)     # Excluding models, cache, repositories  
├── run_frontend.bat             # Launch frontend
├── run_backend.bat              # Launch backend (with setup)
└── download_models.bat          # Download additional models
```

## Advanced Options

All available build flags:
- `-CreateMinimalVenv` - Create optimized Python environment (recommended)
- `-IncludeModels` - Include all models in build
- `-SpecificModels @('model1', 'model2')` - Include only specified models
- `-IncludeRepositories` - Include git repositories and cache
- `-IncludeFullVenv` - Include existing full virtual environment  
- `-PythonPath "path"` - Use specific Python executable

## Troubleshooting

**Build too large (>21GB):** Use `-CreateMinimalVenv` flag

**Python compatibility issues:** Ensure Python 3.10 or 3.11 is installed and use `-PythonPath` to specify it

**Missing dependencies:** The minimal venv excludes some packages that cause compatibility issues - they'll be installed on first run if needed

**Antivirus warnings:** The installer isn't code-signed. For production use, sign with `signtool` and a valid certificate
