# Stable Diffusion Desktop App

This project combines a the FastAPI Python backend from [Stable Diffusion WebUI Forge](https://github.com/automatic1111/stable-diffusion-webui) by AUTOMATIC1111 for running Stable Diffusion with an Avalonia UI frontend.

## Quick Start Options

### Option 1: Windows Installer (Recommended)

#### Use Pre-built Installer
If an installer has already been built, simply run the most recent one:

```powershell
# Navigate to installer output directory
cd installer\installer_output

# Run the most recent installer
.\StableDiffusionDesktopApp-Setup-1.0.0.exe
```

#### Build New Installer
To build a fresh installer for distribution:

```powershell
# Navigate to installer directory
cd installer

# Build compact installer (~1.7GB) - Recommended
.\build_installer.ps1 -CreateMinimalVenv

# Or build with a specific model included (~4GB)  
.\build_installer.ps1 -CreateMinimalVenv -SpecificModels @('dreamshaper')
```

**Prerequisites for building:**
- .NET SDK installed
- Python 3.10 or 3.11 installed
- (Optional) Inno Setup 6 for automatic compilation

**What the build process does:**
1. Creates an embedded Python environment with minimal dependencies
2. Publishes the .NET frontend as a single executable
3. Copies and optimizes the backend files
4. Generates helper scripts for launching and model downloads
5. Compiles everything into a Windows installer

The installer will be created in `installer/installer_output/` and includes:
- Self-contained .NET frontend
- Optimized Python backend with minimal dependencies
- Automatic model download on first run (via CivitAI integration)
- Start Menu and Desktop shortcuts

See [installer/README_installer.md](installer/README_installer.md) for detailed build options.

### Option 2: Manual Setup (Development)
For development or manual installation, follow the backend and frontend setup below.

## Backend Setup

1. Open a terminal in the project directory

2. Run the setup script to create a virtual environment and install dependencies:

#### On Windows
```
setup_scripts/setup_sdapi_venv.bat
```

#### On Linux/Mac
```
# First make the script executable
chmod +x setup_scripts/setup_sdapi_venv.sh
# Then run the setup to create a Python virtual environment with the required packages
./setup_scripts/setup_sdapi_venv.sh
```

3. Start the backend server:
#### On Windows
```
setup_scripts/launch_sdapi_server.bat
```

#### On Linux/Mac
```
# First make the script executable
chmod +x setup_scripts/launch_sdapi_venv.sh
# Then run it
./setup_scripts/launch_sdapi_server.sh
```

## Get models

Models should be placed in /backend/models/\[modelname\]
They can easily be installed in a central location via the [Stability Matrix](https://github.com/LykosAI/StabilityMatrix) project

An example model can also easily be downloaded from [Hugging Face](https://huggingface.co/stabilityai/stable-diffusion-xl-base-1.0/blob/main/sd_xl_base_1.0.safetensors)

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


## Frontend:
### Services/ApiService.cs- Establishes the ApiService class. The following are the functions of the class: 
1. It first establishes the user using HttpClient in order to prevent the API calls from starting a new client every single time, helping with performance.
2. The important part of this program would be “GenerateImage()”. It is responsible for sending a request to the Stable Diffusion Backend API to generate the asked for images based on the asked for prompt by the user. Turns the prompt and setting into JSON data that can be transferred, and then sends a POST request to the backend.
3. Additional implemented features include:
     GetProcessAsync() which is used for the progress bar and knowing how long until the image is fully generated.
     StopGenerationAsync() which allows the user to interrupt the image generation once started. This is useful for people with slower computers because image generation could take upwards of 10 minutes.
     GetAvailableModelsAsync() lets the user see what generation models they have already installed in the backend and then choose what model should be implemented with the image they want for the design using SetModelAsync()
     GetAvailableSamplersAsync() will find what samplers can be used for sampling the image data for the database.
## Backend:
### launch.py- This helps the computer determine whether the UI should be launched with or without GUI, effectively starting it on UI only. Starts the environment and does the following:
1. Imports launch_utils to start preparing the environment. This allows the user to implement premade utilities into the project.
2. Next it records the UI startup time to see how long it takes to start and print the launch mode based on the –-nowebui CLI argument, which, for all of our intents and purposes, will be set to True.(see launch_webui_backend.py READme for more information)
3. Lastly it prepares the environment, sets up testing protocols and launches the actual application onto the desktop.
   
### launch_webui_backend.py- this starts the webui.py function and incorporates all of the necessary parts:
1. Starts off by getting and setting the desired logging information. It makes sure that everything is inputted with the DEBUG function and then translates important information with debug-check option.
2. Tests the device that is implementing the function and makes sure that it functions under the correct conditions. This check runs and if it fails it knows how to launch the program correctly.
3. It finds the right operating system and then DISABLES the webui. This is important, because, even though it is called “webUI.py” it does not actually launch a webui but instead makes it run from the desktop itself.

### webui.py- Using the FastAPI API development system, we implemented the following features into the backend to let the users specify how they want to get their work done:
1. This creates a desktop application which is better than a web application because it is a much lighter load on the computer, and this allows people to have access to image generation.
2. We implemented environmentally sensitive behavior to check the compatibility of our system and downloaded models to have the correct behavior. Using os.getenv, we are able to set up the deployment mode between local and remote. This allows users to offload some of the work to a different system.
3. The second part ties into the first where we allow users to switch between CUDA and CPU. Some personal computers do not have the CUDA extension because it is a proprietary function of Nvidia GPU chips. If we didn’t check for this, some users would be completely unable to use the desktop application.
4. We also have a good way of logging the progress of the image generation with clear information on how each image is made and what goes into the process. This makes it much easier to trace what went wrong in the production process and useful to monitor the behavior of the models.
5. Lastly, we implemented general input validation for the parameters through the Path() function by FastAPI.

### Requirements_versions.txt: This breaks down the version of each of the needed libraries and additions that are required by the program to run.
1. FastAPI: this is the core framework that handles the routing and data validation.
2. Uvicorn: optimizes the fastAPI framework.
3. Torch: this is only required for CPU usage, but it essentially runs neural network compilation.
4. Numpy: general purpose numeric computations.
5. Diffusers: this has pretrained diffusion models that are required by the different models.
6. Transformers: provides tokenized prompts for diffusers.
7. Accelerate: used by hugging face to manage the device placement.
8. Scipy: just helps with the mathematical operations.
9. Pillow: this is used for image manipulation.
10. Pydantic: validates the data and serializes it.
11. pydantic_core: works with pydantic and essentially powers it to work

## Windows Installer

For easy distribution and installation, this project includes a Windows installer builder that creates a complete, portable installation package.
### Building the Installer

**Prerequisites:**
- .NET SDK installed
- Python 3.10 or 3.11 installed  
- (Optional) Inno Setup 6 for automatic compilation

**Recommended build command:**
```powershell
cd installer
.\build_installer.ps1 -CreateMinimalVenv
```

This creates a ~1.7GB installer that includes:
- ✅ Self-contained .NET frontend
- ✅ Optimized Python environment (~6GB compressed with minimal packages)
- ✅ Automatic model download integration (CivitAI and HuggingFace)
- ✅ All necessary runtime dependencies
- ✅ Helper batch scripts for easy launching and model management

**Advanced build options:**
```powershell
# Include specific model (larger but ready to use immediately)
.\build_installer.ps1 -CreateMinimalVenv -SpecificModels @('dreamshaper')

# Force specific Python version  
.\build_installer.ps1 -CreateMinimalVenv -PythonPath "C:\Python310\python.exe"

# Full build with everything (21GB+ - not recommended for distribution)
.\build_installer.ps1 -IncludeModels -IncludeRepositories -IncludeFullVenv
```

**Build Process Overview:**
1. **Python Environment Creation**: Creates optimized embedded Python with only essential packages
2. **Frontend Compilation**: Publishes .NET app as single-file executable  
3. **Backend Optimization**: Copies core backend files, excludes large cached data
4. **Script Generation**: Creates launch scripts and model download helpers
5. **Installer Compilation**: Packages everything with Inno Setup

### Post-Installation Experience

After installation, users get:
- **Desktop shortcut** to launch the app
- **Start Menu shortcuts** for frontend, backend, and model downloads
- **Automatic setup** - models download when first needed via integrated download script
- **No manual configuration** required - everything just works

The installer handles all Python dependencies, virtual environment setup, and provides helper scripts for model management with reliable downloads from CivitAI and HuggingFace.

For detailed installer documentation, see [installer/README_installer.md](installer/README_installer.md).

## Package Setup (macOS)

On macOS, there is an optional package that can be used for app installation in place of the above instructions. It is not yet as polished or reliable as the Windows installer as it was not planned as an Alpha feature.

1. Ensure Python 3.10 is installed.

2. Download and open SDapp.dmg

3. After placing in your Applications folder, run the app myApp via the terminal:
```/Applications/SDapp.app/Contents/MacOS/```

4. This will take a while since it has to download any missing python packages and a ~2GB stable diffusion model.

5. If you ran the app by clicking on it, macOS may kill the process before it has completed setting up the backend and downloading your first model. If so, rerun it. If the port is later occupied (check app_tst % lsof -i :7861), you may need to kill an errant python process

6. Once all setup is complete, the app should launch

7. If adding other models or there is another need to access the /backend/ or /setup_scripts/ folder, they are located at user/Library/Application Support/SDApp. This is now the case when running on macOS even if not using the package installation. (If using the regular installation, the frontend app will copy the backend into Application Support on the first run.)

## Development Notes

- The Python virtual environment should NOT be committed to version control (is in .gitignore)
- Make sure to update requirements_versions.txt when adding new Python dependencies

## Known Bugs / Issues

- Issue: The frontend will timeout after 100 seconds of generation, even if the ETA is making progress
- Bug: Default model selection is not functional; displayed model may not be used unless it is explicitly selected
- Bug: When using MPS, quick, repeating calls to the txt2img and interrupt endpoints result in the next image generation failing (black image)
- Bug: After long periods of time left open, sometimes the backend returns blank images
- Bug: If generate is pressed before a prompt is entered, the next time it is pressed it will send an interrupt api call instead of a generate call, locking the frontend up

## Credits

- The backend and API for this project are derived from [Stable Diffusion WebUI Forge](https://github.com/automatic1111/stable-diffusion-webui) by AUTOMATIC1111, and are licensed under the GNU Affero General Public License v3.0.
- Attribution for third-party code used by that project is preserved in /backend/README.md.
- /backend/launch_webui_backend.py and all code outside of /backend/ are written by members of the [Spicy Banana](https://github.com/SpicyBananaUI) organization.
