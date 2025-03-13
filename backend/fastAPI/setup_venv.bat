@echo off
echo Creating virtual environment for Stable Diffusion FastAPI Backend...

REM Check if Python is installed
python --version >nul 2>&1
if %errorlevel% neq 0 (
    echo Python is not installed or not in PATH. Please install Python 3.8 or newer.
    exit /b 1
)

REM Create venv directory if it doesn't exist
if not exist venv (
    echo Creating virtual environment...
    python -m venv venv
) else (
    echo Virtual environment already exists.
)

REM Activate the virtual environment
echo Activating virtual environment...
call venv\Scripts\activate.bat

REM Install or upgrade pip
echo Upgrading pip...
python -m pip install --upgrade pip

REM Explicitly install NumPy first (to ensure it's properly available)
echo Installing NumPy...
pip install numpy>=1.20.0

REM Install the required packages
echo Installing required packages...
pip install -r requirements.txt

REM Verify NumPy installation
python -c "import numpy; print(f'NumPy {numpy.__version__} is installed correctly')" || echo "NumPy installation failed!"

REM Create an outputImages directory if it doesn't exist
if not exist outputImages (
    echo Creating outputImages directory...
    mkdir outputImages
)

echo.
echo Setup complete! 
echo.
echo To activate the virtual environment, run:
echo     call venv\Scripts\activate
echo.
echo To run the FastAPI server, run:
echo     uvicorn main:app --reload
echo.
echo For GPU acceleration, install PyTorch with CUDA support:
echo     pip install torch==2.2.0+cu118 --extra-index-url https://download.pytorch.org/whl/cu118
echo.
echo Press any key to exit...
pause >nul
