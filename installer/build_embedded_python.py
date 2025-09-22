# Embedded Python Build Script
# Creates a portable Python installation with all dependencies

import urllib.request
import zipfile
import subprocess
import sys
import os
from pathlib import Path

def download_embedded_python():
    """Download Python embedded distribution"""
    python_version = "3.10.11"  # Known compatible version
    url = f"https://www.python.org/ftp/python/{python_version}/python-{python_version}-embed-amd64.zip"
    
    embedded_dir = Path("python-embedded")
    zip_file = "python-embedded.zip"
    
    if embedded_dir.exists():
        print(f"✓ Python embedded already exists at {embedded_dir}")
        return embedded_dir
    
    print(f"Downloading Python {python_version} embedded...")
    urllib.request.urlretrieve(url, zip_file)
    
    print("Extracting Python embedded...")
    with zipfile.ZipFile(zip_file, 'r') as zip_ref:
        zip_ref.extractall(embedded_dir)
    
    os.remove(zip_file)
    print(f"✓ Python embedded ready at {embedded_dir}")
    return embedded_dir

def setup_pip_in_embedded(python_dir):
    """Enable pip in embedded Python"""
    pth_file = python_dir / f"python{sys.version_info.major}{sys.version_info.minor}._pth"
    
    # Find the actual pth file
    pth_files = list(python_dir.glob("python*._pth"))
    if not pth_files:
        print("⚠ No pth file found, creating one...")
        pth_file = python_dir / "python310._pth"
    else:
        pth_file = pth_files[0]
    
    # Enable site packages
    content = pth_file.read_text() if pth_file.exists() else ""
    if "#import site" in content:
        content = content.replace("#import site", "import site")
        pth_file.write_text(content)
        print("✓ Enabled site packages in embedded Python")
    
    # Download and install pip
    python_exe = python_dir / "python.exe"
    
    print("Installing pip...")
    urllib.request.urlretrieve("https://bootstrap.pypa.io/get-pip.py", "get-pip.py")
    subprocess.check_call([str(python_exe), "get-pip.py"])
    os.remove("get-pip.py")
    print("✓ Pip installed")

def install_requirements(python_dir, requirements_file):
    """Install requirements in embedded Python"""
    python_exe = python_dir / "python.exe"
    pip_exe = python_dir / "Scripts" / "pip.exe"
    
    if not pip_exe.exists():
        pip_exe = python_exe  # Fallback to python -m pip
        pip_args = [str(python_exe), "-m", "pip", "install"]
    else:
        pip_args = [str(pip_exe), "install"]
    
    print(f"Installing requirements from {requirements_file}...")
    
    # Install PyTorch with CUDA first (specific version)
    print("Installing PyTorch with CUDA...")
    subprocess.check_call(pip_args + [
        "torch>=2.6.0", 
        "--index-url", "https://download.pytorch.org/whl/cu124"
    ])
    
    # Install other requirements
    if Path(requirements_file).exists():
        subprocess.check_call(pip_args + ["-r", requirements_file])
    else:
        print(f"⚠ Requirements file not found: {requirements_file}")
        # Install minimal requirements
        minimal_deps = [
            "fastapi", "uvicorn", "gradio", "numpy", "Pillow",
            "transformers", "diffusers", "safetensors", "accelerate"
        ]
        subprocess.check_call(pip_args + minimal_deps)
    
    print("✓ All requirements installed")

def create_launcher_script(python_dir, backend_dir):
    """Create simple launcher script"""
    launcher_script = fr"""@echo off
REM Stable Diffusion Backend Launcher
REM No venv activation needed!

set PYTHON_PATH={python_dir.resolve()}\python.exe
set BACKEND_DIR={backend_dir.resolve()}

echo Starting Stable Diffusion Backend...
echo Python: %PYTHON_PATH%
echo Backend: %BACKEND_DIR%

REM Launch the backend
cd /d "%BACKEND_DIR%"
"%PYTHON_PATH%" launch_webui_backend.py

pause
"""
    
    launcher_file = Path("launch_backend_embedded.bat")
    launcher_file.write_text(launcher_script)
    print(f"✓ Launcher created: {launcher_file}")

def main():
    print("=== Embedded Python Build for Stable Diffusion ===\n")
    
    # Step 1: Download embedded Python
    python_dir = download_embedded_python()
    
    # Step 2: Setup pip
    setup_pip_in_embedded(python_dir)
    
    # Step 3: Install requirements
    backend_dir = Path("backend")
    requirements_file = backend_dir / "requirements_versions.txt"
    install_requirements(python_dir, requirements_file)
    
    # Step 4: Create launcher
    create_launcher_script(python_dir, backend_dir)
    
    # Step 5: Test installation
    python_exe = python_dir / "python.exe"
    print("\nTesting installation...")
    try:
        result = subprocess.run([
            str(python_exe), "-c", 
            "import torch; print('PyTorch:', torch.__version__); print('CUDA:', torch.cuda.is_available())"
        ], capture_output=True, text=True)
        print("Test output:", result.stdout)
        if result.returncode == 0:
            print("✓ Embedded Python installation successful!")
        else:
            print("⚠ Test failed:", result.stderr)
    except Exception as e:
        print(f"⚠ Test error: {e}")
    
    # Calculate size
    import shutil
    size_gb = shutil.disk_usage(python_dir)[1] / (1024**3)
    print(f"\nEmbedded Python size: ~{size_gb:.1f}GB")

if __name__ == "__main__":
    main()