@echo off
REM Test execution script for myApp.Tests
REM This script runs the tests with proper configuration

echo Running myApp.Tests...

REM Build the test project
echo Building test project...
dotnet build myApp.Tests.csproj

if %ERRORLEVEL% neq 0 (
    echo Build failed!
    exit /b 1
)

REM Run tests with detailed output
echo Running tests...
dotnet test myApp.Tests.csproj ^
    --verbosity normal ^
    --logger "console;verbosity=detailed" ^
    --collect:"XPlat Code Coverage"

echo Tests completed!
pause
