BACKEND_DIR="./backend"
SCRIPT_DIR="$(dirname "$0")"

cd "$SCRIPT_DIR"
cd ".."

echo "Entering the backend directory to download the Dreamchaper model..."
cd "$BACKEND_DIR" || exit

#!/usr/bin/env bash
set -euo pipefail

# Target directory
TARGET_DIR="./models/Stable-diffusion/sd"

# File URL
URL="https://huggingface.co/Lykon/DreamShaper/resolve/main/DreamShaper_8_pruned.safetensors"

# Ensure the target directory exists
mkdir -p "$TARGET_DIR"

# Only download if there are no .safetensors files
if ! ls "$TARGET_DIR"/*.safetensors >/dev/null 2>&1; then
    echo "No .safetensors files found in $TARGET_DIR, downloading model..."
    curl -L -o "$TARGET_DIR/DreamShaper_8_pruned.safetensors" "$URL"
    echo "Downloaded DreamShaper_8_pruned.safetensors to $TARGET_DIR"
else
    echo "At least one model already exists in $TARGET_DIR, skipping download."
fi

echo "Exiting default model download script."