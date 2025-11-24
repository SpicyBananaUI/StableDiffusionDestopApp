#!/bin/bash

# Script to copy backend and setup_scripts to Application Support directory
# Replicates BackendManager.EnsureBackendFromBundleMac() functionality

set -e  # Exit on error

APP_NAME="SDApp"
APP_SUPPORT_DIR="$HOME/Library/Application Support/$APP_NAME"
MARKER_FILE="$APP_SUPPORT_DIR/.backend_installed"

# Determine source base directory
# If an argument is provided, use it; otherwise, try to detect from script location
if [ -n "$1" ]; then
    BASE_DIR="$1"
    # Convert to absolute path
    BASE_DIR="$(cd "$BASE_DIR" && pwd)"
else
    # Assume script is in base directory (for development)
    SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
    # If script is in setup_scripts, go up one level to get base
    if [[ "$SCRIPT_DIR" == *"setup_scripts" ]]; then
        BASE_DIR="$(cd "$SCRIPT_DIR/.." && pwd)"
    else
        BASE_DIR="$SCRIPT_DIR"
    fi
fi

echo "Base directory: $BASE_DIR"

SOURCE_BACKEND="$BASE_DIR/backend"
SOURCE_SCRIPTS="$BASE_DIR/setup_scripts"
SOURCE_PYPROJECT="$BASE_DIR/pyproject.toml"
SOURCE_TRANSLATION="$BASE_DIR/translation_layer"

# In production, resources are in Contents/
if [ ! -d "$SOURCE_BACKEND" ] && [ -d "$BASE_DIR/Contents/backend" ]; then
    SOURCE_BACKEND="$BASE_DIR/Contents/backend"
fi

if [ ! -d "$SOURCE_SCRIPTS" ] && [ -d "$BASE_DIR/Contents/setup_scripts" ]; then
    SOURCE_SCRIPTS="$BASE_DIR/Contents/setup_scripts"
fi

if [ ! -f "$SOURCE_PYPROJECT" ] && [ -f "$BASE_DIR/Contents/pyproject.toml" ]; then
    SOURCE_PYPROJECT="$BASE_DIR/Contents/pyproject.toml"
fi

if [ ! -d "$SOURCE_TRANSLATION" ] && [ -d "$BASE_DIR/Contents/translation_layer" ]; then
    SOURCE_TRANSLATION="$BASE_DIR/Contents/translation_layer"
fi

echo "Looking for backend at: $SOURCE_BACKEND"
echo "Looking for scripts at: $SOURCE_SCRIPTS"

TARGET_BACKEND="$APP_SUPPORT_DIR/backend"
TARGET_SCRIPTS="$APP_SUPPORT_DIR/setup_scripts"

# Check if running on macOS
if [[ "$OSTYPE" != "darwin"* ]]; then
    echo "Not macOS, skipping backend copy to support directory."
    exit 0
fi

# Check if already copied
if [ -f "$MARKER_FILE" ]; then
    echo "Backend already exists in Application Support, skipping copy."
    exit 0
fi

# Verify source directories exist
if [ ! -d "$SOURCE_BACKEND" ]; then
    echo "Error: Source backend directory not found: $SOURCE_BACKEND"
    exit 1
fi

if [ ! -d "$SOURCE_SCRIPTS" ]; then
    echo "Error: Source setup_scripts directory not found: $SOURCE_SCRIPTS"
    exit 1
fi

if [ ! -f "$SOURCE_PYPROJECT" ]; then
    echo "Error: Source pyproject.toml not found: $SOURCE_PYPROJECT"
    exit 1
fi

if [ ! -d "$SOURCE_TRANSLATION" ]; then
    echo "Error: Source translation_layer directory not found: $SOURCE_TRANSLATION"
    exit 1
fi

# Create Application Support directory if it doesn't exist
mkdir -p "$APP_SUPPORT_DIR"

# Copy directories recursively
echo "Copying backend directory..."
cp -R "$SOURCE_BACKEND" "$TARGET_BACKEND"

echo "Copying setup_scripts directory..."
cp -R "$SOURCE_SCRIPTS" "$TARGET_SCRIPTS"

echo "Copying pyproject.toml..."
cp "$SOURCE_PYPROJECT" "$APP_SUPPORT_DIR/"

echo "Copying translation_layer directory..."
cp -R "$SOURCE_TRANSLATION" "$APP_SUPPORT_DIR/"

# Create marker file with timestamp (ISO 8601 format)
TIMESTAMP=$(date -u +"%Y-%m-%dT%H:%M:%S%z")
echo "$TIMESTAMP" > "$MARKER_FILE"

echo "Backend copied successfully. Marker created at $MARKER_FILE"

# Run setup script to create virtual environment
if [ -f "$TARGET_SCRIPTS/setup_sdapi_venv.sh" ]; then
    echo "Running setup_sdapi_venv.sh..."
    chmod +x "$TARGET_SCRIPTS/setup_sdapi_venv.sh"
    cd "$APP_SUPPORT_DIR"
    "$TARGET_SCRIPTS/setup_sdapi_venv.sh"
else
    echo "Warning: setup_sdapi_venv.sh not found in copied scripts directory."
fi

echo "Setup complete!"

