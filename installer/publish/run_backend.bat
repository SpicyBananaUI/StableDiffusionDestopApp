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
