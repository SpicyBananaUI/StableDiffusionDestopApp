# Build helper for Inno Setup installer (PowerShell)
# - Publishes the .NET frontend
# - Copies the backend folder
# - Generates small run scripts
# - Optionally compiles the Inno Setup installer if ISCC.exe is installed

param(
    [switch]$CreateMinimalVenv,
    [switch]$IncludeModels,
    [string[]]$SpecificModels = @(),
    [switch]$IncludeRepositories,
    [switch]$IncludeFullVenv,
    [string]$PythonPath = ""
)

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
if ($CreateMinimalVenv) {
    # When creating a minimal venv we'll exclude large artifacts (models, existing venv)
    $excludeDirs = @('models', 'webui-venv')
    Write-Host "Creating minimal venv: excluding 'models' and existing 'webui-venv' while copying backend." -ForegroundColor Yellow
} else {
    # Default exclusions to keep installer size manageable
    $excludeDirs = @()
    if (-not $IncludeModels -and $SpecificModels.Count -eq 0) {
        $excludeDirs += 'models'
        Write-Host "Excluding 'models' directory to reduce installer size. Use -IncludeModels or -SpecificModels to include them." -ForegroundColor Yellow
    }
    if (-not $IncludeFullVenv) {
        # Exclude all virtual environments
        $excludeDirs += @('webui-venv', 'venv', 'env', '.venv')
        Write-Host "Excluding virtual environments - will be created during first run." -ForegroundColor Yellow
    }
    if (-not $IncludeRepositories) {
        $excludeDirs += @('repositories', 'cache', 'safetensors-metadata', '__pycache__')
        Write-Host "Excluding repositories, cache, and temporary files that can be regenerated." -ForegroundColor Yellow
    }
    
    Write-Host "Backend exclusions: $($excludeDirs -join ', ')" -ForegroundColor Cyan
}

# Prefer robocopy for fast, robust copying with exclusions
$robocopy = Get-Command 'robocopy.exe' -ErrorAction SilentlyContinue
if ($robocopy) {
    $xdArgs = $excludeDirs | ForEach-Object { '"' + (Join-Path $backendSrc $_) + '"' } | ForEach-Object { "/XD $_" } | Out-String
    $xdArgs = $xdArgs -replace "\r?\n"," "
    
    # Add recursive exclusions for nested venv directories
    if (-not $IncludeFullVenv) {
        $xdArgs += " /XD venv /XD env /XD .venv"
    }
    
    # Exclude specific file patterns that are typically large and not needed
    $xfArgs = "/XF *.log *.tmp *.bak *.pyc *.pyo"
    
    # Use /COPY:DAT (Data, Attributes, Timestamps) instead of /COPYALL to avoid requiring
    # the "Manage auditing and security log" privilege which causes robocopy to fail with exit code 16
    # on non-admin accounts when trying to copy auditing/owner information.
    $cmd = "robocopy.exe `"$backendSrc`" `"$backendDest`" /E /COPY:DAT /R:2 /W:2 /NFL /NDL $xdArgs $xfArgs"
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

# Handle specific model inclusion if requested
if ($SpecificModels.Count -gt 0) {
    $modelsSrc = Join-Path $backendSrc 'models'
    $modelsDest = Join-Path $backendDest 'models'
    if (Test-Path $modelsSrc) {
        Write-Host "Copying specific models: $($SpecificModels -join ', ')" -ForegroundColor Cyan
        New-Item -ItemType Directory -Path $modelsDest -Force | Out-Null
        
        # Copy the directory structure first
        Get-ChildItem -Path $modelsSrc -Directory | ForEach-Object {
            $subDest = Join-Path $modelsDest $_.Name
            New-Item -ItemType Directory -Path $subDest -Force | Out-Null
        }
        
        # Copy specific model files
        foreach ($modelPattern in $SpecificModels) {
            $matchingFiles = @(Get-ChildItem -Path $modelsSrc -Recurse -File | Where-Object { $_.Name -like "*$modelPattern*" })
            if ($matchingFiles.Count -eq 0) {
                Write-Host "Warning: No models found matching pattern '$modelPattern'" -ForegroundColor Yellow
            } else {
                foreach ($file in $matchingFiles) {
                    $relativePath = $file.FullName.Substring($modelsSrc.Length + 1)
                    $destFile = Join-Path $modelsDest $relativePath
                    $destDir = Split-Path $destFile -Parent
                    New-Item -ItemType Directory -Path $destDir -Force | Out-Null
                    Copy-Item -Path $file.FullName -Destination $destFile -Force
                    $sizeMB = [math]::Round($file.Length / 1MB, 2)
                    Write-Host "  Copied: $($file.Name) ($sizeMB MB)" -ForegroundColor Green
                }
            }
        }
    }
}

# If requested, build a minimal venv inside the publish/backend folder using pipreqs
if ($CreateMinimalVenv) {
    Write-Host "Starting minimal venv creation..." -ForegroundColor Cyan

    # Find python executable and check version compatibility
    $pythonCmd = $null
    
    if ($PythonPath) {
        # Use explicitly provided Python path
        if (Test-Path $PythonPath) {
            $pythonCmd = $PythonPath
            Write-Host "Using explicitly specified Python: $PythonPath" -ForegroundColor Cyan
        } else {
            Write-Host "Error: Specified Python path not found: $PythonPath" -ForegroundColor Red
            Write-Host "Please check the path and try again." -ForegroundColor Red
            exit 1
        }
    } else {
        # Auto-detect Python, preferring 3.10.x and 3.11.x versions
        $pythonCandidates = @()
        
        # Check for Python launcher (py.exe) which can list installed versions
        $pyCmd = Get-Command 'py' -ErrorAction SilentlyContinue
        if ($pyCmd) {
            Write-Host "Scanning for Python installations using py launcher..." -ForegroundColor Cyan
            try {
                $pyList = & py -0p 2>$null | Where-Object { $_ -match "(\d+\.\d+)" }
                foreach ($line in $pyList) {
                    if ($line -match "(\d+\.\d+).*?(\S+python\.exe)") {
                        $version = $matches[1]
                        $exePath = $matches[2]
                        if (Test-Path $exePath) {
                            $pythonCandidates += @{ Version = $version; Path = $exePath }
                            Write-Host "  Found Python $version at: $exePath" -ForegroundColor Gray
                        }
                    }
                }
            } catch {
                Write-Host "py launcher failed, falling back to PATH search" -ForegroundColor Yellow
            }
        }
        
        # Fallback: check common Python commands on PATH
        if ($pythonCandidates.Count -eq 0) {
            $pathCandidates = @('python', 'python3', 'python3.10', 'python3.11', 'python3.12')
            foreach ($cmd in $pathCandidates) {
                $found = Get-Command $cmd -ErrorAction SilentlyContinue
                if ($found) {
                    try {
                        $version = & $found.Source -c "import sys; print(f'{sys.version_info.major}.{sys.version_info.minor}')" 2>$null
                        if ($version -match "^\d+\.\d+$") {
                            $pythonCandidates += @{ Version = $version; Path = $found.Source }
                            Write-Host "  Found Python $version at: $($found.Source)" -ForegroundColor Gray
                        }
                    } catch { }
                }
            }
        }
        
        if ($pythonCandidates.Count -eq 0) {
            Write-Host "Error: No Python executable found. Required to create minimal venv." -ForegroundColor Red
            Write-Host "Please install Python or specify a path with -PythonPath parameter." -ForegroundColor Red
            exit 1
        }
        
        # Prefer versions in order: 3.10.x, 3.11.x, 3.12.x, then others
        $preferredVersions = @("3.10", "3.11", "3.12")
        $selectedPython = $null
        
        foreach ($preferredVer in $preferredVersions) {
            $candidate = $pythonCandidates | Where-Object { $_.Version -like "$preferredVer*" } | Select-Object -First 1
            if ($candidate) {
                $selectedPython = $candidate
                Write-Host "Selected preferred Python $($candidate.Version) at: $($candidate.Path)" -ForegroundColor Green
                break
            }
        }
        
        # If no preferred version found, use the first available
        if (-not $selectedPython) {
            $selectedPython = $pythonCandidates[0]
            Write-Host "Using available Python $($selectedPython.Version) at: $($selectedPython.Path)" -ForegroundColor Yellow
        }
        
        $pythonCmd = $selectedPython.Path
    }

    # Check Python version - many packages don't support 3.13 yet
    $pythonVersion = & $pythonCmd -c "import sys; print(f'{sys.version_info.major}.{sys.version_info.minor}')"
    Write-Host "Detected Python version: $pythonVersion" -ForegroundColor Cyan
    
    if ([version]$pythonVersion -ge [version]"3.13") {
        Write-Host "Warning: Python 3.13+ detected. Many packages may not have compatible wheels yet." -ForegroundColor Yellow
        Write-Host "Falling back to copying existing venv for better compatibility." -ForegroundColor Yellow
        
        # Copy existing venv as fallback
        $existingVenv = Join-Path $backendSrc 'webui-venv'
        if (Test-Path $existingVenv) {
            $venvPath = Join-Path $backendDest 'webui-venv'
            Write-Host "Copying existing venv from: $existingVenv" -ForegroundColor Cyan
            & robocopy.exe $existingVenv $venvPath /E /COPY:DAT /R:2 /W:2 /NFL /NDL
            if ($LASTEXITCODE -le 7) {
                Write-Host "Existing venv copied successfully (compatibility fallback)." -ForegroundColor Green
                return
            }
        }
        
        Write-Host "No existing venv found. Attempting minimal venv creation anyway..." -ForegroundColor Yellow
    }

    $reqPath = Join-Path $backendDest 'requirements_auto.txt'

    # Ensure pipreqs is available (try to install if missing)
    $pipreqsCmd = (Get-Command 'pipreqs' -ErrorAction SilentlyContinue).Source
    if (-not $pipreqsCmd) {
        Write-Host "'pipreqs' not found. Attempting to install it into the current Python..." -ForegroundColor Yellow
        & $pythonCmd -m pip install --user pipreqs
        if ($LASTEXITCODE -ne 0) {
            Write-Host "Failed to install pipreqs. Falling back to existing requirements files if present." -ForegroundColor Yellow
            $pipreqsCmd = $null
        } else {
            $pipreqsCmd = (Get-Command 'pipreqs' -ErrorAction SilentlyContinue).Source
        }
    }

    $generated = $false
    if ($pipreqsCmd) {
        Write-Host "Running pipreqs to generate minimal requirements at: $reqPath" -ForegroundColor Cyan
        # Run pipreqs against the copied backend dest (which already excludes large folders)
        # and explicitly ignore these common heavy directories just in case.
        $ignoreList = 'models','webui-venv','cache','packages_3rdparty','repositories','.git','safetensors-metadata','installer_output'
        $ignoreArgs = $ignoreList | ForEach-Object { "--ignore $_" } | Out-String
        $ignoreArgs = $ignoreArgs -replace "\r?\n"," "
        try {
            & $pipreqsCmd $backendDest --force --savepath $reqPath $ignoreArgs
            if ($LASTEXITCODE -eq 0 -and (Test-Path $reqPath)) { $generated = $true }
        } catch {
            Write-Host "pipreqs failed: $_" -ForegroundColor Yellow
            $generated = $false
        }
    }

    if (-not $generated) {
        # Fallbacks: check common requirements files in backend
        $candidate1 = Join-Path $backendSrc 'requirements.txt'
        $candidate2 = Join-Path $repoRoot 'backend\requirements_versions.txt'
        $candidateReqs = @($candidate1, $candidate2)
        foreach ($c in $candidateReqs) {
            if (Test-Path $c) {
                Copy-Item -Path $c -Destination $reqPath -Force
                Write-Host "Copied existing requirements file from: $c" -ForegroundColor Yellow
                $generated = $true
                break
            }
        }
    }

    if (-not $generated) {
        Write-Host "Could not generate a requirements file automatically. Aborting minimal venv creation." -ForegroundColor Red
        Write-Host "Either install 'pipreqs' or provide a requirements.txt in the backend folder." -ForegroundColor Red
        exit 1
    }

    # Create venv and install
    $venvPath = Join-Path $backendDest 'webui-venv'
    Write-Host "Creating venv at: $venvPath" -ForegroundColor Cyan
    & $pythonCmd -m venv $venvPath
    if ($LASTEXITCODE -ne 0) {
        Write-Host "Failed to create venv at $venvPath" -ForegroundColor Red
        exit 1
    }

    $pipExe = Join-Path $venvPath 'Scripts\pip.exe'
    if (-not (Test-Path $pipExe)) {
        Write-Host "pip not found in created venv at $pipExe" -ForegroundColor Red
        exit 1
    }

    Write-Host "Upgrading pip, setuptools and wheel in venv..." -ForegroundColor Cyan
    & $pipExe install --upgrade pip setuptools wheel
    if ($LASTEXITCODE -ne 0) { Write-Host "Warning: pip/setuptools/wheel upgrade failed but continuing." -ForegroundColor Yellow }

    # Before installing, generate a sanitized requirements file to relax strict pins for packages
    # that often require compilation from source (e.g. Pillow). This helps pip find newer binary wheels.
    $sanitizedReqPath = Join-Path $backendDest 'requirements_sanitized.txt'
    $replacements = @()
    # Exclude packages that commonly have Python version compatibility issues
    $excludedPackages = @('tensorflow', 'tensorflow-gpu', 'tensorboard', 'cuda-toolkit', 'cudatoolkit', 'torch-audio', 'torchaudio')
    
    # Add Python 3.13 specific exclusions
    if ([version]$pythonVersion -ge [version]"3.13") {
        $excludedPackages += @('facexlib', 'basicsr', 'gfpgan', 'realesrgan', 'filterpy')
        Write-Host "Added Python 3.13 compatibility exclusions: facexlib, basicsr, gfpgan, realesrgan, filterpy" -ForegroundColor Yellow
    }
    
    if (Test-Path $reqPath) {
        Get-Content $reqPath | ForEach-Object {
            $line = $_.Trim()
            if ($line -match '^(Pillow)(==)([0-9]+\.[0-9]+\.[0-9]+)') {
                $pkg = $matches[1]
                $ver = $matches[3]
                $new = "$pkg>=$ver"
                $replacements += "Replaced '$line' -> '$new'"
                $new
            } elseif ($line -match '^([^=><\s#]+)') {
                $packageName = $matches[1].ToLower()
                $shouldExclude = $excludedPackages | Where-Object { $packageName -like "*$_*" -or $packageName -eq $_ } | Select-Object -First 1
                if ($shouldExclude) {
                    $replacements += "Excluded: $line (compatibility/size optimization)"
                    # Skip this line by returning nothing
                } else {
                    $line
                }
            } else {
                $line
            }
        } | Where-Object { $_ } | Set-Content -Path $sanitizedReqPath -Encoding ASCII
        if ($replacements.Count -gt 0) {
            Write-Host "Sanitized requirements file created at: $sanitizedReqPath" -ForegroundColor Cyan
            $replacements | ForEach-Object { Write-Host "  $_" -ForegroundColor Yellow }
        } else {
            # No replacements made; use original file
            Copy-Item -Path $reqPath -Destination $sanitizedReqPath -Force
        }
    } else {
        Write-Host "Warning: expected requirements file not found at $reqPath" -ForegroundColor Yellow
    }

    # Try installing with prefer-binary first (helps avoid source builds like Pillow)
    Write-Host "Installing minimal requirements (prefer binary wheels) from $sanitizedReqPath into venv..." -ForegroundColor Cyan
    & $pipExe install --prefer-binary -r $sanitizedReqPath
    if ($LASTEXITCODE -ne 0) {
        Write-Host "Prefer-binary pip install failed. Attempting to create a wheelhouse of binary wheels and install from it..." -ForegroundColor Yellow
        $wheelDir = Join-Path $backendDest 'wheelhouse'
        if (-not (Test-Path $wheelDir)) { New-Item -ItemType Directory -Path $wheelDir | Out-Null }

        # Create a relaxed requirements file to convert strict pins (==) to >= where appropriate.
        # This helps 'pip download --only-binary' find available wheels for newer versions.
        $relaxedReqPath = Join-Path $backendDest 'requirements_relaxed.txt'
        Get-Content $sanitizedReqPath | ForEach-Object {
            $line = $_.Trim()
            if ($line -match '^(?:-e\s+|git\+|http:|https:|file:|\\s*$)') {
                # Preserve editable/VCS/URL lines and blank lines
                $line
            } elseif ($line -match '^([^#\s][^=<>!~\s]+)\s*==\s*([0-9].*)$') {
                # Convert 'pkg==1.2.3' -> 'pkg>=1.2.3'
                "$($matches[1])>=$($matches[2])"
            } else {
                $line
            }
        } | Set-Content -Path $relaxedReqPath -Encoding ASCII

        # Try to download only binary wheels for requirements using the relaxed pins
        & $pipExe download --only-binary=:all: -r $relaxedReqPath -d $wheelDir
        if ($LASTEXITCODE -eq 0 -and (Get-ChildItem -Path $wheelDir -File -ErrorAction SilentlyContinue).Count -gt 0) {
            Write-Host "Installing from wheelhouse..." -ForegroundColor Cyan
            & $pipExe install --no-index --find-links $wheelDir -r $sanitizedReqPath
        } else {
            Write-Host "Wheelhouse download did not produce usable wheels or failed." -ForegroundColor Yellow
        }

        if ($LASTEXITCODE -ne 0) {
            Write-Host "pip install (wheelhouse) failed for minimal requirements. See output above." -ForegroundColor Red
            # Fallback: if the source backend has an existing webui-venv, copy it into the publish folder
            $sourceVenv = Join-Path $backendSrc 'webui-venv'
            if (Test-Path $sourceVenv) {
                Write-Host "Attempting fallback: copying existing venv from source: $sourceVenv" -ForegroundColor Yellow
                # Remove the failed venv we created
                if (Test-Path $venvPath) { Remove-Item -Path $venvPath -Recurse -Force -ErrorAction SilentlyContinue }
                $robocopy = Get-Command 'robocopy.exe' -ErrorAction SilentlyContinue
                if ($robocopy) {
                    $cmd = "robocopy.exe `"$sourceVenv`" `"$venvPath`" /E /COPY:DAT /R:2 /W:2 /NFL /NDL"
                    Write-Host "Running: $cmd"
                    iex $cmd
                } else {
                    Copy-Item -Path $sourceVenv -Destination $venvPath -Recurse -Force
                }
                if (Test-Path $venvPath) {
                    Write-Host "Fallback venv copied successfully. The installer will include the existing venv (may be large)." -ForegroundColor Yellow
                } else {
                    Write-Host "Fallback venv copy failed. Please inspect the build output and consider using a different Python version, installing build tools, or providing a prebuilt venv." -ForegroundColor Red
                    exit 1
                }
            } else {
                Write-Host "No existing venv found to fallback to. Please inspect $reqPath and try installing manually into the venv, or provide a prebuilt venv at backend\webui-venv." -ForegroundColor Red
                exit 1
            }
        } else {
            Remove-Item -Path $reqPath -Force -ErrorAction SilentlyContinue
            Write-Host "Minimal venv created successfully (from wheelhouse)." -ForegroundColor Green
        }
    } else {
        Remove-Item -Path $reqPath -Force -ErrorAction SilentlyContinue
        Write-Host "Minimal venv created successfully (prefer-binary succeeded)." -ForegroundColor Green
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
"@
Set-Content -Path (Join-Path $publishDir 'run_backend.bat') -Value $runBackend -Encoding ASCII

# Create a model download helper script
$downloadModels = @"
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
"@
Set-Content -Path (Join-Path $publishDir 'download_models.bat') -Value $downloadModels -Encoding ASCII

# 4) Compile installer with Inno Setup if available
# smoke-check: ensure frontend was published successfully before attempting to compile installer
$frontendExe = Join-Path $publishFrontendDir 'myApp.exe'
if (-not (Test-Path $frontendExe)) {
    Write-Host "Error: expected frontend executable not found at: $frontendExe" -ForegroundColor Red
    Write-Host "Did 'dotnet publish' succeed? Aborting before creating installer." -ForegroundColor Red
    exit 1
}

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

# Report payload size breakdown (frontend, backend, venv)
function Get-DirSizeBytes($path) {
    if (-not (Test-Path $path)) { return 0 }
    $items = Get-ChildItem -Path $path -Recurse -Force -ErrorAction SilentlyContinue
    return ($items | Where-Object { -not $_.PSIsContainer } | Measure-Object -Property Length -Sum).Sum
}

$frontendSize = Get-DirSizeBytes (Join-Path $publishDir 'Frontend')
$backendSize = Get-DirSizeBytes (Join-Path $publishDir 'backend')
$venvSize = Get-DirSizeBytes (Join-Path $publishDir 'backend\webui-venv')
$totalSize = $frontendSize + $backendSize

Write-Host "Payload size summary:" -ForegroundColor Cyan
Write-Host ("  Frontend: {0:N2} MB" -f ($frontendSize / 1MB))
Write-Host ("  Backend:  {0:N2} MB" -f ($backendSize / 1MB))
if ($venvSize -gt 0) { Write-Host ("  Venv:     {0:N2} MB" -f ($venvSize / 1MB)) }
Write-Host ("  Total:    {0:N2} MB" -f ($totalSize / 1MB))

# Provide optimization suggestions based on current size
if ($totalSize -gt 21GB) {
    Write-Host "WARNING: Payload exceeds 21 GB. Inno Setup may not be able to create a single-file installer." -ForegroundColor Red
    Write-Host "Try these optimization flags:" -ForegroundColor Yellow
    Write-Host "  .\build_installer.ps1 -CreateMinimalVenv -SpecificModels @('dreamshaper')" -ForegroundColor Yellow
} elseif ($totalSize -gt 10GB) {
    Write-Host "Notice: Payload is large (>10GB). Consider using optimization flags for smaller installer:" -ForegroundColor Yellow
    Write-Host "  .\build_installer.ps1 -CreateMinimalVenv  # Excludes models, creates minimal venv" -ForegroundColor Yellow
} elseif ($totalSize -lt 1GB) {
    Write-Host "Excellent! Compact installer size. Models can be downloaded post-install using download_models.bat" -ForegroundColor Green
    Write-Host "This installer size is perfect for distribution and will work with all Inno Setup configurations." -ForegroundColor Green
} else {
    Write-Host "Good installer size. Well balanced between features and download size." -ForegroundColor Green
}

# Show what was actually included/excluded for transparency
Write-Host "`nBuild Configuration Summary:" -ForegroundColor Cyan
Write-Host ("  Models included: {0}" -f $(if ($IncludeModels) { "All models" } elseif ($SpecificModels.Count -gt 0) { $SpecificModels -join ", " } else { "None (download post-install)" }))
Write-Host ("  Virtual environments: {0}" -f $(if ($IncludeFullVenv) { "Included existing venv" } elseif ($CreateMinimalVenv) { "Created minimal venv" } else { "Excluded (create on first run)" }))
Write-Host ("  Repositories/cache: {0}" -f $(if ($IncludeRepositories) { "Included" } else { "Excluded (download on first run)" }))

if ($totalSize -lt 5GB) {
    Write-Host "`nRecommendation: This build size is excellent for distribution! 🎉" -ForegroundColor Green
}

Write-Host "`nUsage Examples:" -ForegroundColor Magenta
Write-Host "  # Ultra-compact installer with Python 3.10:" -ForegroundColor Gray
Write-Host "  .\build_installer.ps1 -CreateMinimalVenv -PythonPath `"C:\Python310\python.exe`"" -ForegroundColor Gray
Write-Host "  # Or let the script auto-detect Python 3.10:" -ForegroundColor Gray  
Write-Host "  .\build_installer.ps1 -CreateMinimalVenv" -ForegroundColor Gray
Write-Host "  # Include specific model:" -ForegroundColor Gray
Write-Host "  .\build_installer.ps1 -CreateMinimalVenv -SpecificModels @('dreamshaper')" -ForegroundColor Gray
