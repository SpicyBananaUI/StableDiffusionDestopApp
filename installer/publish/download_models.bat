@echo off
setlocal enabledelayedexpansion
REM Helper script to download common Stable Diffusion models
pushd "%~dp0"

set "MODELS_DIR=%~dp0backend\models\Stable-diffusion"
echo Creating models directory: %MODELS_DIR%
mkdir "%MODELS_DIR%" 2>nul

echo.
echo ============================================
echo    Stable Diffusion Model Downloader
echo ============================================
echo.
echo Available models to download:
echo.
echo 1. DreamShaper 8 (~2GB) - High quality, versatile model
echo 2. SDXL Base 1.0 (~6.6GB) - Latest high-resolution model  
echo 3. Both models
echo 4. Cancel
echo.
choice /C 1234 /M "Select option"

if !ERRORLEVEL!==1 goto :download_dreamshaper
if !ERRORLEVEL!==2 goto :download_sdxl
if !ERRORLEVEL!==3 goto :download_both
if !ERRORLEVEL!==4 goto :end

:download_dreamshaper
echo.
echo Downloading DreamShaper 8... Please wait, this may take several minutes.
powershell -ExecutionPolicy Bypass -Command "try { $ProgressPreference = 'Continue'; Invoke-WebRequest -Uri 'https://huggingface.co/Lykon/DreamShaper/resolve/main/DreamShaper_8_pruned.safetensors' -OutFile '%MODELS_DIR%\dreamshaper_8.safetensors' -UseBasicParsing; Write-Host 'DreamShaper 8 downloaded successfully!' -ForegroundColor Green; exit 0 } catch { Write-Host 'Download failed: ' $_.Exception.Message -ForegroundColor Red; exit 1 }"
if !ERRORLEVEL!==0 (
    echo ✓ DreamShaper 8 downloaded successfully!
) else (
    echo ✗ Download failed. Please check your internet connection and try again.
)
goto :end

:download_sdxl  
echo.
echo Downloading SDXL Base 1.0... This is a large file and may take a while.
powershell -ExecutionPolicy Bypass -Command "try { $ProgressPreference = 'Continue'; Invoke-WebRequest -Uri 'https://huggingface.co/stabilityai/stable-diffusion-xl-base-1.0/resolve/main/sd_xl_base_1.0.safetensors' -OutFile '%MODELS_DIR%\sd_xl_base_1.0.safetensors' -UseBasicParsing; Write-Host 'SDXL Base 1.0 downloaded successfully!' -ForegroundColor Green; exit 0 } catch { Write-Host 'Download failed: ' $_.Exception.Message -ForegroundColor Red; exit 1 }"
if !ERRORLEVEL!==0 (
    echo ✓ SDXL Base 1.0 downloaded successfully!
) else (
    echo ✗ Download failed. Please check your internet connection and try again.
)
goto :end

:download_both
echo.
echo Downloading both models... This will take a while.
echo.
echo [1/2] Downloading DreamShaper 8...
powershell -ExecutionPolicy Bypass -Command "try { $ProgressPreference = 'Continue'; Invoke-WebRequest -Uri 'https://huggingface.co/Lykon/DreamShaper/resolve/main/DreamShaper_8_pruned.safetensors' -OutFile '%MODELS_DIR%\dreamshaper_8.safetensors' -UseBasicParsing; Write-Host 'DreamShaper 8 downloaded successfully!' -ForegroundColor Green; exit 0 } catch { Write-Host 'Download failed: ' $_.Exception.Message -ForegroundColor Red; exit 1 }"
if !ERRORLEVEL!==0 (
    echo ✓ DreamShaper 8 downloaded successfully!
) else (
    echo ✗ DreamShaper 8 download failed.
)

echo.
echo [2/2] Downloading SDXL Base 1.0...
powershell -ExecutionPolicy Bypass -Command "try { $ProgressPreference = 'Continue'; Invoke-WebRequest -Uri 'https://huggingface.co/stabilityai/stable-diffusion-xl-base-1.0/resolve/main/sd_xl_base_1.0.safetensors' -OutFile '%MODELS_DIR%\sd_xl_base_1.0.safetensors' -UseBasicParsing; Write-Host 'SDXL Base 1.0 downloaded successfully!' -ForegroundColor Green; exit 0 } catch { Write-Host 'Download failed: ' $_.Exception.Message -ForegroundColor Red; exit 1 }"
if !ERRORLEVEL!==0 (
    echo ✓ SDXL Base 1.0 downloaded successfully!
) else (
    echo ✗ SDXL Base 1.0 download failed.
)

:end
echo.
echo Models are saved to: %MODELS_DIR%
echo.
echo After downloading, restart the backend for models to appear in the dropdown.
echo You can also check the downloaded models by looking in the models directory.
echo.
dir "%MODELS_DIR%\*.safetensors" 2>nul && echo Current models: || echo No models found yet.
dir "%MODELS_DIR%\*.safetensors" /b 2>nul
echo.
pause
popd
