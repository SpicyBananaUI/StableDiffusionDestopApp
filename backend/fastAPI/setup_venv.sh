#!/bin/bash
echo "Creating virtual environment for Stable Diffusion FastAPI Backend..."

# Check if Python is installed
if ! command -v python3 &> /dev/null; then
    echo "Python is not installed. Please install Python 3.8 or newer."
    exit 1
fi

# Create venv directory if it doesn't exist
if [ ! -d "venv" ]; then
    echo "Creating virtual environment..."
    python3 -m venv venv
else
    echo "Virtual environment already exists."
fi

# Activate the virtual environment
echo "Activating virtual environment..."
source venv/bin/activate

# Install or upgrade pip
echo "Upgrading pip..."
pip install --upgrade pip

# Install the required packages
echo "Installing required packages..."
pip install -r requirements.txt

# Create an outputImages directory if it doesn't exist
if [ ! -d "outputImages" ]; then
    echo "Creating outputImages directory..."
    mkdir -p outputImages
fi

echo ""
echo "Setup complete!"
echo ""
echo "To activate the virtual environment, run:"
echo "    source venv/bin/activate"
echo ""
echo "To run the FastAPI server, run:"
echo "    uvicorn main:app --reload"
echo ""
