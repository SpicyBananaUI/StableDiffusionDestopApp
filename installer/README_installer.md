This folder contains helper files to build an Inno Setup installer for the project.

What the included script does
- `build_installer.ps1` publishes the .NET frontend as a single-file EXE (win-x64), copies the `backend/` folder into `installer/publish`, and creates two small helper batch scripts (`run_frontend.bat`, `run_backend.bat`).
- `installer.iss` is an Inno Setup script that packages everything under `installer/publish` into a single installer EXE and creates Start Menu / Desktop shortcuts.

Usage (Windows PowerShell):

1) Make sure you have the .NET SDK installed and `dotnet` is on PATH.
2) (Optional) Install Inno Setup if you want the script to compile the final EXE automatically.

Run the build helper from the repo root (PowerShell):

```powershell
cd "<path>\StableDiffusionDestopApp\installer"
.\build_installer.ps1
```

If `ISCC.exe` is available on PATH the script will automatically compile the Inno installer and place output in `installer_output` (sibling of the `installer` folder). If ISCC is not found the script leaves the assembled `installer/publish` folder for manual compilation.

Notes & limitations
- The script copies the `backend/` folder as-is. It does not pre-create a Python virtual environment or bundle the Python runtime. On first-run `run_backend.bat` will invoke `backend\setup_scripts\setup_sdapi_venv.bat` (the same setup script already in the repo) to create the venv and install Python dependencies. This keeps the installer small but requires an internet connection and admin/user approval during the first backend setup.
- Code signing is not included. To reduce antivirus flags and for production distribution, sign the resulting installer with your code signing certificate using `signtool`.
- If you prefer bundling a prebuilt virtualenv or embedding Python, tell me and I can update the scripts to do that.
