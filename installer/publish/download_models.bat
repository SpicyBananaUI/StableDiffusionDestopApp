@echo off
REM Helper script to download common Stable Diffusion models
pushd "%~dp0"

set MODELS_DIR=%~dp0\backend\models\Stable-diffusion
mkdir "%MODELS_DIR%" 2>nul

echo Available models to download:
echo.
echo 1. DreamShaper 8 ^(~2GB^) - High quality, versatile model
echo 2. SDXL Base 1.0 ^(~6.6GB^) - Latest high-resolution model
echo 3. Both models
echo 4. Cancel
echo.
choice /C 1234 /M "Select option"

if %ERRORLEVEL%==1 goto :download_dreamshaper
if %ERRORLEVEL%==2 goto :download_sdxl
if %ERRORLEVEL%==3 goto :download_both
if %ERRORLEVEL%==4 goto :end

:download_dreamshaper
echo Downloading DreamShaper 8...
powershell -Command "Invoke-WebRequest -Uri 'https://huggingface.co/Lykon/DreamShaper/resolve/main/DreamShaper_8_pruned.safetensors' -OutFile '%MODELS_DIR%\dreamshaper_8.safetensors' -UseBasicParsing"
goto :end

:download_sdxl
echo Downloading SDXL Base 1.0 ^(this may take a while^)...
powershell -Command "Invoke-WebRequest -Uri 'https://huggingface.co/stabilityai/stable-diffusion-xl-base-1.0/resolve/main/sd_xl_base_1.0.safetensors' -OutFile '%MODELS_DIR%\sd_xl_base_1.0.safetensors' -UseBasicParsing"
goto :end

:download_both
echo Downloading DreamShaper 8...
powershell -Command "Invoke-WebRequest -Uri 'https://huggingface.co/Lykon/DreamShaper/resolve/main/DreamShaper_8_pruned.safetensors' -OutFile '%MODELS_DIR%\dreamshaper_8.safetensors' -UseBasicParsing"
echo Downloading SDXL Base 1.0 ^(this may take a while^)...
powershell -Command "Invoke-WebRequest -Uri 'https://huggingface.co/stabilityai/stable-diffusion-xl-base-1.0/resolve/main/sd_xl_base_1.0.safetensors' -OutFile '%MODELS_DIR%\sd_xl_base_1.0.safetensors' -UseBasicParsing"

:end
echo Done!
pause
popd
