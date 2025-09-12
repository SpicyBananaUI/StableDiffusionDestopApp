# Build helper for Inno Setup installer (PowerShell)
# - Publishes the .NET frontend
# - Copies the backend folder
# - Generates small run scripts
# - Optionally compiles the Inno Setup installer if ISCC.exe is installed

Set-StrictMode -Version Latest
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Definition
$repoRoot = (Resolve-Path "$scriptDir\..\").Path
$publishDir = Join-Path $scriptDir 'publish'
$frontendProj = Join-Path $repoRoot 'myApp\myApp.csproj'

Write-Host "Cleaning publish folder: $publishDir"
if (Test-Path $publishDir) { Remove-Item $publishDir -Recurse -Force }
New-Item -ItemType Directory -Path $publishDir | Out-Null

# 1) Publish frontend (single-file self-contained for win-x64)
Write-Host "Publishing frontend..."
$publishFrontendDir = Join-Path $publishDir 'Frontend'
New-Item -ItemType Directory -Path $publishFrontendDir | Out-Null

# Use the PowerShell call operator to invoke dotnet so paths with spaces are handled correctly
$dotnetExe = 'dotnet'
$publishCmd = @('publish', $frontendProj, '-c', 'Release', '-r', 'win-x64', '/p:PublishSingleFile=true', '/p:PublishTrimmed=true', '-o', $publishFrontendDir)
Write-Host "Running: $dotnetExe $($publishCmd -join ' ')"
& $dotnetExe @publishCmd
if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed with exit code $LASTEXITCODE" }

# 2) Copy backend folder (exclude very large/runtime-specific dirs)
# We exclude model caches, prebuilt virtualenvs and other bulky folders so the installer stays reasonable.
$backendSrc = Join-Path $repoRoot 'backend'
$backendDest = Join-Path $publishDir 'backend'
Write-Host "Copying backend from $backendSrc to $backendDest (excluding large folders like models, cache, webui-venv)"
New-Item -ItemType Directory -Path $backendDest | Out-Null

# Directories to exclude (relative names)
$excludeDirs = @('models', 'cache', 'webui-venv', 'packages_3rdparty', 'repositories', '.git')

# Prefer robocopy for fast, robust copying with exclusions
$robocopy = Get-Command 'robocopy.exe' -ErrorAction SilentlyContinue
if ($robocopy) {
    $xdArgs = $excludeDirs | ForEach-Object { '"' + (Join-Path $backendSrc $_) + '"' } | ForEach-Object { "/XD $_" } | Out-String
    $xdArgs = $xdArgs -replace "\r?\n"," "
    # Use /COPY:DAT (Data, Attributes, Timestamps) instead of /COPYALL to avoid requiring
    # the "Manage auditing and security log" privilege which causes robocopy to fail with exit code 16
    # on non-admin accounts when trying to copy auditing/owner information.
    $cmd = "robocopy.exe `"$backendSrc`" `"$backendDest`" /E /COPY:DAT /R:2 /W:2 /NFL /NDL $xdArgs"
    Write-Host "Running: $cmd"
    iex $cmd
    # Robocopy exit codes: 0-7 are success; 8+ indicates failure
    if ($LASTEXITCODE -ge 8) { throw "robocopy failed with exit code $LASTEXITCODE" }
} else {
    Write-Host "robocopy not found; falling back to Copy-Item (this may include large files)."
    # Fallback naive copy but try to skip common large dirs
    Get-ChildItem -Path $backendSrc -Force | Where-Object { $excludeDirs -notcontains $_.Name } | ForEach-Object {
        $dest = Join-Path $backendDest $_.Name
        if ($_.PSIsContainer) {
            Copy-Item -Path $_.FullName -Destination $dest -Recurse -Force
        } else {
            Copy-Item -Path $_.FullName -Destination $dest -Force
        }
    }
}

# 3) Create run scripts in publish root for shortcuts
$runFrontend = @"
@echo off
REM Runs the published frontend executable
pushd "%~dp0"
start "" "%~dp0\Frontend\myApp.exe"
popd
"@
Set-Content -Path (Join-Path $publishDir 'run_frontend.bat') -Value $runFrontend -Encoding ASCII

$runBackend = @"
@echo off
REM If the virtualenv folder exists, attempt activation; otherwise run the setup script
pushd "%~dp0"
if exist "%~dp0\backend\webui-venv\Scripts\activate.bat" (
    echo Activating existing virtualenv and launching backend...
    call "%~dp0\backend\webui-venv\Scripts\activate.bat"
    call "%~dp0\backend\setup_scripts\launch_sdapi_server.bat"
) else (
    echo No virtualenv found. Running backend setup script (this may take a while)...
    call "%~dp0\backend\setup_scripts\setup_sdapi_venv.bat"
    call "%~dp0\backend\setup_scripts\launch_sdapi_server.bat"
)
popd
"@
Set-Content -Path (Join-Path $publishDir 'run_backend.bat') -Value $runBackend -Encoding ASCII

# 4) Compile installer with Inno Setup if available
$iscc = Get-Command 'ISCC.exe' -ErrorAction SilentlyContinue
$issPath = Join-Path $scriptDir 'installer.iss'
if ($iscc) {
    Write-Host "Found ISCC.exe at $($iscc.Source). Compiling installer..."
    & $iscc.Source $issPath
    if ($LASTEXITCODE -ne 0) { Write-Host "ISCC failed (exit code $LASTEXITCODE)."; exit $LASTEXITCODE }
    Write-Host "Installer compiled. See installer_output in repo root."
} else {
    Write-Host "ISCC.exe not found in PATH. Skipping compilation."
    Write-Host "To compile the installer, install Inno Setup and run:\n   ISCC.exe \"$issPath\""
}

Write-Host "Done. The assembled payload is in: $publishDir"
