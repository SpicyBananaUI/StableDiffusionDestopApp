# Build Embedded Python Installer
# This creates the installer using embedded Python instead of venv

param(
    [switch]$BuildFrontend = $true,
    [switch]$CleanBuild = $false
)

Write-Host "=== Stable Diffusion Installer Build (Embedded Python) ===" -ForegroundColor Green

# Check if Inno Setup is installed
$innoPath = "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe"
if (-not (Test-Path $innoPath)) {
    Write-Host "ERROR: Inno Setup 6 not found at $innoPath" -ForegroundColor Red
    Write-Host "Please install Inno Setup 6 from https://jrsoftware.org/isinfo.php"
    exit 1
}

# Clean previous build if requested
if ($CleanBuild) {
    Write-Host "Cleaning previous builds..." -ForegroundColor Yellow
    if (Test-Path "installer_output") {
        Remove-Item "installer_output" -Recurse -Force
    }
}

# Build frontend if requested
if ($BuildFrontend) {
    Write-Host "Building frontend..." -ForegroundColor Yellow
    Set-Location "..\myApp"
    dotnet publish -c Release
    if ($LASTEXITCODE -ne 0) {
        Write-Host "Frontend build failed" -ForegroundColor Red
        exit 1
    }
    Set-Location "..\installer"
    Write-Host "Frontend build completed" -ForegroundColor Green
}

Write-Host "Current directory: $(Get-Location)"

# Verify embedded Python exists
if (-not (Test-Path "..\python-embedded\python.exe")) {
    Write-Host "ERROR: Embedded Python not found!" -ForegroundColor Red
    Write-Host "Building embedded Python environment first..." -ForegroundColor Yellow
    
    Write-Host "Current directory: $(Get-Location)"

    # Run the embedded Python build script
    python build_embedded_python.py
    
    Write-Host "Current directory: $(Get-Location)"

    if (-not (Test-Path "..\\python-embedded\\python.exe")) {
        Write-Host "ERROR: Failed to create embedded Python environment!" -ForegroundColor Red
        exit 1
    }
    Write-Host "Embedded Python environment created successfully" -ForegroundColor Green
}

# Verify frontend exists
if (-not (Test-Path "..\myApp\bin\Release\net9.0\publish\myApp.exe")) {
    Write-Host "ERROR: Frontend not found!" -ForegroundColor Red
    Write-Host "Please build the frontend first with: dotnet publish -c Release"
    exit 1
}

# Calculate sizes for info
$embeddedSize = (Get-ChildItem "..\python-embedded" -Recurse | Measure-Object -Sum Length).Sum / 1GB
$backendSize = (Get-ChildItem "..\backend" -Recurse | Where-Object {$_.FullName -notlike "*\models\*" -and $_.FullName -notlike "*webui-venv*"} | Measure-Object -Sum Length).Sum / 1MB

Write-Host ""
Write-Host "Build Summary:" -ForegroundColor Cyan
Write-Host "  Embedded Python: $($embeddedSize.ToString('F1')) GB"
Write-Host "  Backend: $($backendSize.ToString('F1')) MB" 

# Build installer
Write-Host ""
Write-Host "Building installer..." -ForegroundColor Yellow
& $innoPath "installer_embedded.iss"

if ($LASTEXITCODE -eq 0) {
    Write-Host "Installer built successfully!" -ForegroundColor Green
    
    # Show results
    if (Test-Path "installer_output\StableDiffusionDesktopApp_Setup.exe") {
        $installerSize = (Get-Item "installer_output\StableDiffusionDesktopApp_Setup.exe").Length / 1MB
        Write-Host ""
        Write-Host "SUCCESS!" -ForegroundColor Green
        Write-Host "Installer created: installer_output\StableDiffusionDesktopApp_Setup.exe"
        Write-Host "Installer size: $($installerSize.ToString('F1')) MB"
        Write-Host ""
        Write-Host "Ready to distribute!" -ForegroundColor Green
    } else {
        Write-Host "Installer file not found after build" -ForegroundColor Red
        exit 1
    }
} else {
    Write-Host "Installer build failed" -ForegroundColor Red
    exit 1
}
