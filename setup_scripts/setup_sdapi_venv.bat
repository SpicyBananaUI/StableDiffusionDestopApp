REM Copyright (c) 2025 Spicy Banana
REM SPDX-License-Identifier: AGPL-3.0-only


@echo off
setlocal

set "BACKEND_DIR=backend"
set "VENV_DIR=webui-venv"
set "PYTHON_EXE=python"

echo Creating virtual environment with packages required by sdapi (webui) backend...

REM Check if Python 3.10 is installed
where %PYTHON_EXE% >nul 2>&1
if errorlevel 1 (
    echo Python is not installed or not in PATH. Please install Python 3.10.
    exit /b 1
)

echo Using Python: 
%PYTHON_EXE% --version

REM Navigate to backend directory
if not exist "%BACKEND_DIR%" (
    echo Backend directory "%BACKEND_DIR%" does not exist. Creating it...
    mkdir "%BACKEND_DIR%"
)

cd "%BACKEND_DIR%" || (
    echo Failed to navigate to backend directory.
    exit /b 1
)

REM Check if virtual environment exists
if not exist "%VENV_DIR%\Scripts\activate.bat" (
    echo Creating virtual environment...
    %PYTHON_EXE% -m venv %VENV_DIR%
) else (
    echo Compatible virtual environment already exists.
    exit /b 1
)

REM Activate the virtual environment
call "%VENV_DIR%\Scripts\activate.bat"
if errorlevel 1 (
    echo Failed to activate virtual environment.
    exit /b 1
)

REM Upgrade pip
echo Upgrading pip...
python -m pip install --upgrade pip

REM Installing cuda-compiled torch
pip install torch torchvision torchaudio --index-url https://download.pytorch.org/whl/cu126

REM Install required packages
if exist "requirements_versions.txt" (
    echo Installing required packages...
    pip install -r requirements_versions.txt
) else (
    echo requirements_versions.txt not found! Skipping package installation.
)

REM TODO: Create any directories that are .gitignored

echo.
echo Setup complete!
echo.
echo To activate the virtual environment in the future, run:
echo     .\%BACKEND_DIR%\%VENV_DIR%\Scripts\activate.bat
echo.
echo To run the sdapi server, run:
echo     setup_scripts\launch_sdapi_server.bat
echo.

endlocal
