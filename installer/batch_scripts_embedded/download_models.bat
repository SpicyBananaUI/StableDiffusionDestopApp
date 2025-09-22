@echo off
REM Model Download Script - Embedded Python Version
REM Downloads example models to user AppData (no admin rights needed)

echo ======================================
echo    Stable Diffusion Model Downloader
echo ======================================

set "MODELS_DIR=%~dp0backend\models\Stable-diffusion"
set "VAE_DIR=%~dp0backend\models\VAE"

echo Models will be downloaded to:
echo %MODELS_DIR%
echo.

REM Create directories if they don't exist
if not exist "%MODELS_DIR%" (
    echo Creating models directory...
    mkdir "%MODELS_DIR%" 2>nul
)

if not exist "%VAE_DIR%" (
    echo Creating VAE directory...
    mkdir "%VAE_DIR%" 2>nul
)

echo.
echo Available models to download:
echo 1. DreamShaper 8 (2GB) - Popular community model
echo 2. SD 1.5 Base Model (4GB) - Stable Diffusion v1.5
echo 3. SD 2.1 Base Model (5GB) - Stable Diffusion v2.1  
echo 4. SDXL Base Model (6.6GB) - Stable Diffusion XL
echo 5. Skip downloads (use existing models)
echo.

set /p choice="Enter your choice (1-5): "

if "%choice%"=="1" goto download_dreamshaper
if "%choice%"=="2" goto download_sd15
if "%choice%"=="3" goto download_sd21
if "%choice%"=="4" goto download_sdxl
if "%choice%"=="5" goto skip_downloads
goto invalid_choice

:download_dreamshaper
echo.
echo Downloading DreamShaper 8 (Recommended for testing)...
echo This may take several minutes for a 2GB file...

powershell -ExecutionPolicy Bypass -Command "try { $wc = New-Object System.Net.WebClient; $wc.Headers.Add('User-Agent', 'Mozilla/5.0'); Write-Host 'Downloading from CivitAI...'; $wc.DownloadFile('https://civitai.com/api/download/models/128713', '%MODELS_DIR%\dreamshaper_8.safetensors'); $size = (Get-Item '%MODELS_DIR%\dreamshaper_8.safetensors').Length; Write-Host 'Downloaded' ([Math]::Round($size / 1MB, 2)) 'MB successfully!'; $wc.Dispose() } catch { Write-Host 'Download failed:' $_.Exception.Message; exit 1 }"

if %ERRORLEVEL% NEQ 0 (
    echo Download failed! Please check your internet connection.
    goto end
)

goto download_complete

:download_sd15
echo.
echo Downloading Stable Diffusion v1.5...
powershell -ExecutionPolicy Bypass -Command "try { Invoke-WebRequest -Uri 'https://huggingface.co/runwayml/stable-diffusion-v1-5/resolve/main/v1-5-pruned-emaonly.ckpt' -OutFile '%MODELS_DIR%\sd-v1-5.ckpt' -UseBasicParsing; Write-Host 'Download completed successfully!' } catch { Write-Host 'Download failed:' $_.Exception.Message }"
goto download_complete

:download_sd21
echo.
echo Downloading Stable Diffusion v2.1...
powershell -ExecutionPolicy Bypass -Command "try { Invoke-WebRequest -Uri 'https://huggingface.co/stabilityai/stable-diffusion-2-1/resolve/main/v2-1_768-ema-pruned.ckpt' -OutFile '%MODELS_DIR%\sd-v2-1.ckpt' -UseBasicParsing; Write-Host 'Download completed successfully!' } catch { Write-Host 'Download failed:' $_.Exception.Message }"
goto download_complete

:download_sdxl
echo.
echo Downloading Stable Diffusion XL...
powershell -ExecutionPolicy Bypass -Command "try { Invoke-WebRequest -Uri 'https://huggingface.co/stabilityai/stable-diffusion-xl-base-1.0/resolve/main/sd_xl_base_1.0.safetensors' -OutFile '%MODELS_DIR%\sdxl-base-1.0.safetensors' -UseBasicParsing; Write-Host 'Download completed successfully!' } catch { Write-Host 'Download failed:' $_.Exception.Message }"
goto download_complete

:skip_downloads
echo.
echo Skipping downloads. You can manually place models in:
echo %MODELS_DIR%
goto end

:invalid_choice
echo.
echo Invalid choice. Please run the script again and select 1-5.
goto end

:download_complete
echo.
echo Download completed! Models are saved in:
echo %MODELS_DIR%
echo.
echo You can now launch the Stable Diffusion Desktop App.

:end
echo.
pause