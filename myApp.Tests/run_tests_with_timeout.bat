@echo off
REM Test runner script with timeout support
REM Usage: run_tests_with_timeout.bat [timeout_seconds]

REM Set default timeout if not provided
if "%1"=="" (
    set TIMEOUT_SECONDS=30
) else (
    set TIMEOUT_SECONDS=%1
)

echo ==========================================
echo Avalonia Headless Test Runner
echo ==========================================
echo Timeout: %TIMEOUT_SECONDS% seconds
echo To override timeout:
echo   - Set environment variable: set TEST_TIMEOUT_SECONDS=60
echo   - Pass as argument: run_tests_with_timeout.bat 60
echo ==========================================
echo.

REM Export timeout for the test configuration
set TEST_TIMEOUT_SECONDS=%TIMEOUT_SECONDS%

REM Change to project directory
cd /d "%~dp0\.."

echo Building test project...
dotnet build myApp.Tests/myApp.Tests.csproj --verbosity quiet

if %ERRORLEVEL% neq 0 (
    echo Build failed. Exiting.
    exit /b 1
)

echo Running tests...
echo.

REM Run tests with timeout
dotnet test myApp.Tests/myApp.Tests.csproj ^
    --verbosity minimal ^
    --logger "console;verbosity=minimal" ^
    --no-build

echo.
echo ==========================================
echo Test run completed with %TIMEOUT_SECONDS% second timeout
echo ==========================================
