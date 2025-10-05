@echo off
setlocal

set "BACKEND_DIR=backend"
set "PYTHON_EXE="

echo Checking for Python 3.10...

REM Look for python3.10 directly
where python3.10 >nul 2>&1
if %errorlevel%==0 (
    set "PYTHON_EXE=python"
) else (
    REM Check all installed Python versions and find 3.10
    for /f "delims=" %%P in ('where python 2^>nul') do (
        for /f "tokens=2 delims= " %%V in ('"%%P --version"') do (
            echo Found Python version %%V at %%P
            echo %%V | findstr /b "3.10" >nul
            if not errorlevel 1 (
                set "PYTHON_EXE=%%P"
                goto FoundPython
            )
        )
    )

    echo Python 3.10 is not installed or not in PATH.
    exit /b 1
)

:FoundPython

echo Using: %PYTHON_EXE%
echo.

REM Print current directory
echo Current directory:
cd
echo.

REM Navigate to backend directory
cd "%BACKEND_DIR%" || (
    echo Failed to navigate to %BACKEND_DIR%.
    exit /b 1
)

echo Inside backend directory:
cd
echo.

REM Activate virtual environment
echo Activating virtual environment...
call webui-venv\Scripts\activate.bat
if errorlevel 1 (
    echo Failed to activate virtual environment.
    exit /b 1
)

echo.
echo Checking Python version...
%PYTHON_EXE% -c "import sys; print(sys.executable)"
echo.


echo.
echo Launching sdapi (webui backend)...
echo You can verify functionality by running: %PYTHON_EXE% test\basic_test.py
echo.

%PYTHON_EXE% launch_webui_backend.py %*

endlocal
