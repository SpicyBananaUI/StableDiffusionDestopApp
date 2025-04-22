#!/bin/bash

# Copyright (c) 2025 Spicy Banana
# SPDX-License-Identifier: AGPL-3.0-only


BACKEND_DIR="./backend"

echo "Creating virtual environment with packages required by sdapi (webui) backend..."

# Check if Python is installed
if ! command -v python3.10 &> /dev/null; then
    echo "Python 3.10 is not installed. Please install Python 3.10."
    exit 1
fi

# Enter the Backend directory
cd "$BACKEND_DIR" || exit

# Create venv directory if it doesn't exist
if [ ! -d "webui-venv" ]; then
    echo "Creating virtual environment..."
    python3 -m venv webui-venv
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
echo "Installing required packages..."
pip install -r requirements_versions.txt


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
