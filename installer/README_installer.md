# Windows Installer Builder

This folder contains helper files to build an Inno Setup installer for the Stable Diffusion Desktop App.

## What the Scripts Do

- **`build_installer.ps1`** - Main build script that:
  - Creates an optimized embedded Python environment with minimal dependencies
  - Publishes the .NET frontend as a single-file EXE (win-x64)
  - Copies and optimizes the backend folder (excluding models, cache, repositories)
  - Optionally compiles the final installer EXE with Inno Setup

- **`installer.iss`** - Inno Setup script that packages everything under `installer/publish` into a single installer EXE with Start Menu and Desktop shortcuts

- **`installer_embedded.iss`** - Enhanced Inno Setup script with improved exclusion patterns and desktop icon configuration

## Prerequisites

1. **.NET SDK** - Make sure `dotnet` is available on PATH
2. **Python 3.10 or 3.11** - Required for creating the minimal virtual environment  
3. **Inno Setup** (Optional) - For automatic compilation to final installer EXE

## Build Options

### Compact Installer (~1.7GB)
**Recommended for distribution**
```powershell
cd installer
.\build_installer.ps1
```
- Creates optimized embedded Python environment (~6GB compressed to ~1GB in installer)
- Includes only essential packages (torch, diffusers, transformers, fastapi, etc.)
- Excludes models (users download post-install via integrated CivitAI/HuggingFace script) 
- Excludes repositories/cache (downloaded on first run)
- Uses Python 3.10/3.11 automatically for best compatibility

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
- **Download Models** - Helper script to download additional models from CivitAI and HuggingFace

**First-time setup:**
1. Install and launch from Desktop shortcut
2. Backend automatically starts with the frontend
3. If no models are present, use "Download Models" shortcut to get DreamShaper 8 (~2GB)
4. Models download reliably using improved PowerShell scripts with progress tracking
5. Start generating images immediately after download completes

**Integrated model download features:**
- Downloads from both CivitAI and HuggingFace for reliability
- Progress indication during large file downloads
- Automatic file integrity verification  
- Resume capability for interrupted downloads
- Multiple model options (DreamShaper 8, SD 1.5, SD 2.1, SDXL)

**Embedded Python Environment Details:**
- **Size**: ~1.7GB
- **Packages**: Only essential dependencies (torch, diffusers, transformers, fastapi, uvicorn, etc.)
- **Excluded**: Development tools, documentation, cached files, duplicate libraries
- **CUDA Support**: Included for GPU acceleration where available
- **Compatibility**: Works on Windows 10/11 x64 systems

## Troubleshooting

**Python compatibility issues:** Ensure Python 3.10 or 3.11 is installed and use `-PythonPath` to specify it

**Missing dependencies:** The minimal embedded environment excludes some packages that cause compatibility issues - they'll be installed automatically on first run if needed

**Model download failures:** The integrated download script uses both CivitAI and HuggingFace with retry logic. Check internet connection and try the "Download Models" shortcut again

**Antivirus warnings:** The installer isn't code-signed. For production distribution, sign the installer with `signtool` and a valid certificate

**Large file exclusions:** The build process automatically excludes:
- Model files (*.safetensors, *.ckpt) - downloaded post-install
- Cache directories - rebuilt on first run  
- Repository clones - fetched as needed
- Development tools and documentation

**Build process fails:** Ensure all prerequisites are installed:
```powershell
# Check .NET SDK
dotnet --version

# Check Python version  
python --version

# Check Inno Setup
Get-Command ISCC.exe -ErrorAction SilentlyContinue
```
