# Stable Diffusion Desktop App

This project combines a the FastAPI Python backend from [Stable Diffusion WebUI Forge](https://github.com/automatic1111/stable-diffusion-webui) by AUTOMATIC1111 for running Stable Diffusion with an Avalonia UI frontend.

## Backend Setup

1. Open a terminal in the project directory

2. Run the setup script to create a virtual environment and install dependencies:
```
# On Windows
setup_scripts/setup_sdapi_venv.bat


# On Linux/Mac
# First make the script executable
chmod +x setup_scripts/setup_sdapi_venv.sh
# Then run the setup to create a Python virtual environment with the required packages
./setup_scripts/setup_sdapi_venv.sh
```

3. Start the backend server:
```
# On Windows
setup_scripts/launch_sdapi_server.bat


# On Linux/Mac
# First make the script executable
chmod +x setup_scripts/launch_sdapi_venv.sh
# Then run it
./setup_scripts/launch_sdapi_server.sh
```

## GPU Acceleration Setup (Optional)

The setup scripts attempt to install a (much faster) CUDA-compiled version of torch. If they are unsuccessful, try the following:

1. Make sure you have a CUDA-compatible NVIDIA GPU
2. Install NVIDIA drivers for your GPU
3. Install PyTorch with CUDA support:

```
# Activate your virtual environment first
backend\webui-venv\Scripts\activate  # Windows
source backend/webuivenv/bin/activate  # Linux/Mac

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
- Make sure to update requirements_versions.txt when adding new Python dependencies

## Known Bugs / Issues

- On machines with CUDA support, a CUDA-compiled version of torch must be installed. The setup scripts attempt to install the CUDA version, but if it is not available CPU fallback will be used.
- The frontend will timeout after 100 seconds of generation, even if the ETA is making progress
- Default model selection is not functional; displayed model may not be used unless it is explicitly selected
- When using MPS, quick, repeating calls to the txt2img and interrupt endpoints result in the next image generation failing (black image)
