#!/bin/bash

# Copyright (c) 2025 Spicy Banana
# SPDX-License-Identifier: AGPL-3.0-only


BACKEND_DIR="./backend"
SCRIPT_DIR="$(dirname "$0")"

cd "$SCRIPT_DIR"
cd ".."

# Check if Python is installed
if ! command -v python3.10 &> /dev/null; then
    echo "Python 3.10 is not installed. Please install Python 3.10."
    exit 1
fi

pwd

# Enter the Backend directory
cd "$BACKEND_DIR" || exit

pwd

# Activate the virtual environment
echo "Activating virtual environment..."
source webui-venv/bin/activate

echo "Launching sdapi (webui backend)..."
echo "You can verify functionality by python ./test/basic_test.py"

python3.10 launch_webui_backend.py
