@echo off
setlocal enabledelayedexpansion

echo ========================================
echo  Stable Diffusion Desktop App
echo ========================================
echo.

:: Set paths
set "CURRENT_DIR=%~dp0"
set "BACKEND_DIR=%CURRENT_DIR%backend"
set "VENV_DIR=%BACKEND_DIR%\webui-venv"
set "PYTHON_EXE=%VENV_DIR%\Scripts\python.exe"
:: Use user's AppData for models to avoid permission issues
set "SD_MODELS_DIR=%LOCALAPPDATA%\StableDiffusion\models\Stable-diffusion"

:: Check if virtual environment exists, create if needed
if not exist "%PYTHON_EXE%" (
    echo Virtual environment not found. Creating new environment...
    echo This will take a few minutes on first run.
    echo.
    
    :: Create virtual environment
    python -m venv "%VENV_DIR%" --upgrade-deps
    if %ERRORLEVEL% neq 0 (
        echo ERROR: Failed to create virtual environment.
        echo Please ensure Python 3.8+ is installed and available in PATH.
        pause
        exit /b 1
    )
    
    :: Install all requirements including PyTorch with specific versions
    echo Installing dependencies with compatible versions...
    if exist "%BACKEND_DIR%\requirements_versions.txt" (
        "%VENV_DIR%\Scripts\pip.exe" install -r "%BACKEND_DIR%\requirements_versions.txt"
        if %ERRORLEVEL% neq 0 (
            echo ERROR: Failed to install requirements. Please check your internet connection.
            pause
            exit /b 1
        )
    )
    
    :: Install PyTorch with CUDA support (compatible with numpy from requirements)
    echo Installing PyTorch with CUDA support...
    "%VENV_DIR%\Scripts\pip.exe" install torch torchvision torchaudio --index-url https://download.pytorch.org/whl/cu121
    if %ERRORLEVEL% neq 0 (
        echo ERROR: Failed to install PyTorch. Trying CPU version...
        "%VENV_DIR%\Scripts\pip.exe" install torch torchvision torchaudio
    )
    
    :: Force reinstall scikit-image to ensure compatibility with current numpy
    echo Ensuring package compatibility...
    "%VENV_DIR%\Scripts\pip.exe" install --force-reinstall --no-deps scikit-image==0.21.0
    
    echo ✓ Virtual environment created successfully!
    echo.
)

:: Check for models
echo Checking for Stable Diffusion models...
if exist "%SD_MODELS_DIR%\*.safetensors" (
    echo Models found! Starting backend...
    goto :start_backend
) else (
    echo No models found in: "%SD_MODELS_DIR%"
    echo.
    echo Would you like to download a model now?
    echo 1^) Download DreamShaper 8 ^(recommended, ~2GB^)
    echo 2^) Download SDXL Base 1.0 ^(larger, ~7GB^) 
    echo 3^) Skip and start without models
    echo.
    set /p "choice=Enter your choice (1-3): "
    
    if "!choice!"=="1" goto :download_dreamshaper
    if "!choice!"=="2" goto :download_sdxl
    if "!choice!"=="3" goto :start_backend
    
    echo Invalid choice. Starting without models...
    goto :start_backend
)

:download_dreamshaper
echo.
echo Downloading DreamShaper 8... Please wait, this may take several minutes.
mkdir "%SD_MODELS_DIR%" 2>nul

powershell.exe -ExecutionPolicy Bypass -Command "Write-Host 'Starting download...'; Invoke-WebRequest -Uri 'https://huggingface.co/Lykon/DreamShaper/resolve/main/DreamShaper_8_pruned.safetensors' -OutFile '%SD_MODELS_DIR%\dreamshaper_8.safetensors' -UseBasicParsing; Write-Host 'Download completed!' -ForegroundColor Green"

if %ERRORLEVEL% equ 0 (
    echo ✓ Model downloaded successfully!
) else (
    echo ✗ Download failed. You can try again or add models manually.
)
goto :start_backend

:download_sdxl
echo.
echo Downloading SDXL Base 1.0... Please wait, this may take a very long time.
mkdir "%SD_MODELS_DIR%" 2>nul

powershell.exe -ExecutionPolicy Bypass -Command "Write-Host 'Starting download...'; Invoke-WebRequest -Uri 'https://huggingface.co/stabilityai/stable-diffusion-xl-base-1.0/resolve/main/sd_xl_base_1.0.safetensors' -OutFile '%SD_MODELS_DIR%\sd_xl_base_1.0.safetensors' -UseBasicParsing; Write-Host 'Download completed!' -ForegroundColor Green"

if %ERRORLEVEL% equ 0 (
    echo ✓ Model downloaded successfully!
) else (
    echo ✗ Download failed. You can try again or add models manually.
)
goto :start_backend

:start_backend
echo.
echo Starting Stable Diffusion backend...
cd /d "%BACKEND_DIR%"
call "%VENV_DIR%\Scripts\activate.bat"
"%PYTHON_EXE%" launch_webui_backend.py --ckpt-dir "%SD_MODELS_DIR%"
pause