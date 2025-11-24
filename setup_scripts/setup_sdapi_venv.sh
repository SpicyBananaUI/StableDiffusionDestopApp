#!/bin/bash
BACKEND_DIR="./backend"
SCRIPT_DIR="$(dirname "$0")"

cd "$SCRIPT_DIR"
cd ".."

echo "Creating virtual environment with packages required by sdapi (webui) backend..."

# Add common paths to PATH to ensure we can find python
export PATH="/usr/local/bin:/opt/homebrew/bin:/Library/Frameworks/Python.framework/Versions/3.10/bin:$PATH"

# Check if Python is installed
PYTHON_CMD="python3.10"
if ! command -v $PYTHON_CMD &> /dev/null; then
    # Try absolute paths
    if [ -f "/usr/local/bin/python3.10" ]; then
        PYTHON_CMD="/usr/local/bin/python3.10"
    elif [ -f "/opt/homebrew/bin/python3.10" ]; then
        PYTHON_CMD="/opt/homebrew/bin/python3.10"
    elif [ -f "/Library/Frameworks/Python.framework/Versions/3.10/bin/python3.10" ]; then
        PYTHON_CMD="/Library/Frameworks/Python.framework/Versions/3.10/bin/python3.10"
    else
        echo "Python 3.10 is not installed or not found in PATH."
        echo "Please install Python 3.10 from python.org"
        exit 1
    fi
fi

echo "Found Python 3.10 at: $(command -v $PYTHON_CMD || echo $PYTHON_CMD)"

# Enter the Backend directory
cd "$BACKEND_DIR" || exit

# Create venv directory if it doesn't exist
if [ ! -d "webui-venv" ]; then
    echo "Creating virtual environment..."
    "$PYTHON_CMD" -m venv webui-venv
else
    echo "Compatible virtual environment already exists."
fi

# Activate the virtual environment
echo "Activating virtual environment..."
source webui-venv/bin/activate

# Install or upgrade pip
echo "Upgrading pip..."
pip install --upgrade pip

# Install the required packages
# Install the required packages
echo "Installing required packages..."

if [[ "$OSTYPE" == "darwin"* ]]; then
    echo "Detected macOS. Installing macOS-specific dependencies..."
    
    # Create a temporary requirements file for macOS
    # Remove cuda-specific versions and let pip resolve compatible versions
    grep -v "torchvision" requirements_versions.txt > requirements_mac.txt
    
    # Install torch and torchvision for macOS (CPU/MPS)
    pip install torch torchvision
    
    # Install other requirements
    pip install -r requirements_mac.txt
    
    rm requirements_mac.txt
else
    pip install -r requirements_versions.txt
fi

# Install the translation layer
echo "Installing translation layer..."
cd ..
pip install -e .


echo ""
echo "Setup complete!"
echo ""
echo "To activate the virtual environment, run:"
echo "    source venv/bin/activate"
echo ""
echo "To run the sdapi server, run:"
echo "    chmod +x ./setup_scripts/launch_sdapi_server.sh"
echo "    ./setup_scripts/launch_sdapi_server.sh"
echo ""
