# Stable Diffusion Desktop App

This project combines a FastAPI Python backend for running Stable Diffusion with an Avalonia UI frontend.

## Backend Setup

1. Navigate to the backend directory:
```
cd backend/fastAPI
```

2. Run the setup script to create a virtual environment and install dependencies:
```
# On Windows
setup_venv.bat

*Alternative if batch script doesn't work run the following commands*
1. python -m venv venv
2. venv\Scripts\activate
3. pip install -r requirements.txt

# On Linux/Mac
# First make the script executable
chmod +x setup_venv.sh
# Then run it
./setup_venv.sh
```

3. Start the backend server:
```
# On Windows
run_server.bat

*If batch scripts don't work run the following*

uvicorn main:app --reload


# On Linux/Mac
# First make the script executable
chmod +x run_server.sh
# Then run it
./run_server.sh
```

## GPU Acceleration Setup (Optional)

To use GPU acceleration with CUDA (much faster image generation):

1. Make sure you have a CUDA-compatible NVIDIA GPU
2. Install NVIDIA drivers for your GPU
3. Install PyTorch with CUDA support:

```
# Activate your virtual environment first
venv\Scripts\activate  # Windows
source venv/bin/activate  # Linux/Mac

# Then install PyTorch with CUDA support (for CUDA 11.8)
pip uninstall torch
pip install torch==2.2.0+cu118 --extra-index-url https://download.pytorch.org/whl/cu118

# If you have CUDA 12.1 installed, you can use this command instead
# pip install torch==2.2.0+cu121 --extra-index-url https://download.pytorch.org/whl/cu121
```

You can check if CUDA is available in Python with:
```python
import torch
print(f"CUDA available: {torch.cuda.is_available()}")
print(f"CUDA device count: {torch.cuda.device_count()}")
print(f"CUDA version: {torch.version.cuda}")
```

## Frontend Setup

1. Make sure you have .NET 9.0 SDK installed

2. Open myApp in JetBrains Rider

3. Run myApp

## Development Notes

- The Python virtual environment should NOT be committed to version control
- Make sure to update requirements.txt when adding new Python dependencies

## Known Bugs

- Issues with CUDA support not properly working with certain versions of torch/numpy, CUDA is currently unoptimized, CPU utilization is a viable alternative for now.
- Frequent timeouts for long prompts.
