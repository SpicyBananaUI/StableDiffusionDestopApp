@echo off
REM Enhanced backend launcher with optional model downloading
pushd "%~dp0"

REM Check if models directory exists and is empty or missing
set MODELS_DIR=%~dp0\backend\models
set NEEDS_MODELS=0
if not exist "%MODELS_DIR%" (
    set NEEDS_MODELS=1
) else (
    dir /b "%MODELS_DIR%\Stable-diffusion\*.safetensors" >nul 2>&1 || set NEEDS_MODELS=1
)

REM If no models found, ask user if they want to download a basic model
if %NEEDS_MODELS%==1 (
    echo.
    echo No Stable Diffusion models found in the models directory.
    echo.
    echo Would you like to download a basic model? This will download approximately 2GB.
    echo Recommended model: DreamShaper 8 ^(high quality, versatile^)
    echo.
    choice /C YN /M "Download DreamShaper 8 model? (Y/N)"
    if !ERRORLEVEL!==1 (
        echo Downloading DreamShaper 8 model...
        mkdir "%MODELS_DIR%\Stable-diffusion" 2>nul
        powershell -Command "& { Invoke-WebRequest -Uri 'https://huggingface.co/Lykon/DreamShaper/resolve/main/DreamShaper_8_pruned.safetensors' -OutFile '%MODELS_DIR%\Stable-diffusion\dreamshaper_8.safetensors' -UseBasicParsing; Write-Host 'Model download completed!' }"
        if !ERRORLEVEL! NEQ 0 (
            echo Failed to download model. You can manually download models later.
            timeout /t 3 >nul
        )
    ) else (
        echo Skipping model download. You can download models manually later.
        echo See README.md for model installation instructions.
        timeout /t 3 >nul
    )
)

REM Continue with normal backend startup
if exist "%~dp0\backend\webui-venv\Scripts\activate.bat" (
    echo Activating existing virtualenv and launching backend...
    call "%~dp0\backend\webui-venv\Scripts\activate.bat"
    call "%~dp0\backend\setup_scripts\launch_sdapi_server.bat"
) else (
    echo No virtualenv found. Running backend setup script ^(this may take a while^)...
    call "%~dp0\backend\setup_scripts\setup_sdapi_venv.bat"
    call "%~dp0\backend\setup_scripts\launch_sdapi_server.bat"
)
popd
