@echo off
setlocal

:: Set the target directory relative to backend
set "TARGET_DIR=%~dp0..\backend\models\Stable-diffusion"

:: Create directory if it doesn't exist
if not exist "%TARGET_DIR%" mkdir "%TARGET_DIR%"

echo Downloading DreamShaper 8 model...
echo This may take a while due to the large file size (2GB)...
echo Progress will be shown below...

@echo off
setlocal

:: Set the target directory relative to backend
set "TARGET_DIR=%~dp0..\backend\models\Stable-diffusion"

:: Create directory if it doesn't exist
if not exist "%TARGET_DIR%" mkdir "%TARGET_DIR%"

echo Downloading DreamShaper 8 model...
echo Target directory: %TARGET_DIR%
echo This may take a while due to the large file size (2GB)...

:: Simple but reliable download using PowerShell
powershell -ExecutionPolicy Bypass -Command ^
"$url = 'https://civitai.com/api/download/models/128713'; " ^
"$output = '%TARGET_DIR%\dreamshaper_8.safetensors'; " ^
"Write-Host 'Starting download to:' $output; " ^
"try { " ^
"  $webClient = New-Object System.Net.WebClient; " ^
"  $webClient.Headers.Add('User-Agent', 'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36'); " ^
"  $webClient.DownloadFile($url, $output); " ^
"  $size = (Get-Item $output).Length; " ^
"  Write-Host 'Downloaded' ([Math]::Round($size / 1MB, 2)) 'MB'; " ^
"  $webClient.Dispose(); " ^
"} catch { " ^
"  Write-Host 'Download failed:' $_.Exception.Message; " ^
"  exit 1; " ^
"}"

if %ERRORLEVEL% NEQ 0 (
    echo Download failed!
    pause
    exit /b 1
)

echo Download completed successfully!
echo Model saved to: %TARGET_DIR%\dreamshaper_8.safetensors
pause

if %ERRORLEVEL% NEQ 0 (
    echo Download failed!
    pause
    exit /b 1
)

echo Model saved to: %TARGET_DIR%\dreamshaper_8.safetensors
echo You can now use this model in the Stable Diffusion application.
pause