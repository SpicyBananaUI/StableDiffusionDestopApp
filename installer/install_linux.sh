#!/bin/bash

# Linux Installer for Stable Diffusion Desktop App
# Fully automated version

set -e  # Exit on error

INSTALL_DIR="/opt/sdapp"
APP_NAME="SDApp"
GIT_REPO="https://github.com/SpicyBananaUI/StableDiffusionDestopApp.git"

echo "=== Stable Diffusion Desktop App - Linux Installer ==="
echo ""

# Check if running as root
if [ "$EUID" -ne 0 ]; then 
    echo "This installer requires root privileges."
    echo "Please run with sudo: sudo $0"
    exit 1
fi

# Get the actual user (non-root)
ACTUAL_USER="${SUDO_USER:-$USER}"
ACTUAL_HOME=$(eval echo ~$ACTUAL_USER)
echo "Installing for user: $ACTUAL_USER"
echo "Installation directory: $INSTALL_DIR"
echo ""

# Check for dependencies
echo "Checking dependencies..."
for cmd in git rsync; do
    if ! command -v $cmd &> /dev/null; then
        echo "ERROR: $cmd not found."
        exit 1
    fi
done

# Check for Python 3.10+
PYTHON_CMD=""
for py_version in python3.10 python3.11 python3; do
    if command -v $py_version &> /dev/null; then
        PYTHON_CMD=$py_version
        break
    fi
done
if [ -z "$PYTHON_CMD" ]; then
    echo "ERROR: Python 3.10 or higher not found."
    exit 1
fi
echo "Found Python: $($PYTHON_CMD --version)"

if ! $PYTHON_CMD -m pip --version &> /dev/null; then
    echo "ERROR: pip not found for $PYTHON_CMD"
    exit 1
fi

# Check for ICU library (required for .NET)
if ! ldconfig -p | grep -q libicu; then
    echo "ERROR: ICU library (libicu) not found."
    echo ""
    echo "This is required for .NET applications. Install it with:"
    echo "  Ubuntu/Debian: sudo apt install libicu-dev"
    echo "  Fedora/RHEL:   sudo dnf install libicu"
    echo "  Arch Linux:    sudo pacman -S icu"
    exit 1
fi

# Check for X11 libraries (required for Avalonia GUI)
MISSING_LIBS=()
for lib in libICE libSM libX11 libXext libXrandr libXcursor libXi; do
    if ! ldconfig -p | grep -q "$lib"; then
        MISSING_LIBS+=("$lib")
    fi
done

if [ ${#MISSING_LIBS[@]} -ne 0 ]; then
    echo "ERROR: Missing X11 libraries required for GUI: ${MISSING_LIBS[*]}"
    echo ""
    echo "Install them with:"
    echo "  Ubuntu/Debian: sudo apt install libice6 libsm6 libx11-6 libxext6 libxrandr2 libxcursor1 libxi6"
    echo "  Fedora/RHEL:   sudo dnf install libICE libSM libX11 libXext libXrandr libXcursor libXi"
    echo "  Arch Linux:    sudo pacman -S libice libsm libx11 libxext libxrandr libxcursor libxi"
    exit 1
fi

echo "All dependencies found!"
echo ""

# Detect architecture
ARCH=$(uname -m)
if [ "$ARCH" = "x86_64" ]; then
    BINARY_ARCH="x64"
elif [ "$ARCH" = "aarch64" ]; then
    BINARY_ARCH="arm64"
else
    echo "ERROR: Unsupported architecture: $ARCH"
    exit 1
fi
echo "Detected architecture: $ARCH (using $BINARY_ARCH binaries)"
echo ""


# Clone repository from specific branch
TEMP_DIR=$(mktemp -d /tmp/sdapp-install-XXXX)
echo "Cloning repository from GitHub (branch: linux-installer) to $TEMP_DIR..."
if ! git clone --branch linux-installer --single-branch "$GIT_REPO" "$TEMP_DIR"; then
    echo "ERROR: Failed to clone repository from branch linux-installer"
    rm -rf "$TEMP_DIR"
    exit 1
fi
echo "Repository cloned successfully from linux-installer branch"


# Locate pre-built binaries
BINARY_FILE="$TEMP_DIR/installer/linux_binaries/sdapp-linux-${BINARY_ARCH}.tar.gz"
if [ ! -f "$BINARY_FILE" ]; then
    echo "ERROR: Pre-built binaries not found at: $BINARY_FILE"
    rm -rf "$TEMP_DIR"
    exit 1
fi
echo "Found pre-built binaries for $BINARY_ARCH"

# Create installation directory
mkdir -p "$INSTALL_DIR/app"
mkdir -p "$INSTALL_DIR/backend"
mkdir -p "$INSTALL_DIR/setup_scripts"

# Extract frontend binaries
echo "Installing frontend binaries..."
tar -xzf "$BINARY_FILE" -C "$INSTALL_DIR/app/"
chmod +x "$INSTALL_DIR/app/myApp"

# Copy backend files (excluding venv, cache, models, etc.)
echo "Installing backend files..."
rsync -a --progress \
    --exclude='webui-venv' \
    --exclude='venv' \
    --exclude='__pycache__' \
    --exclude='*.pyc' \
    --exclude='*.pyo' \
    --exclude='/cache' \
    --exclude='/models' \
    --exclude='/outputs' \
    --exclude='/extensions' \
    --exclude='/embeddings' \
    --exclude='/log' \
    --exclude='/config_states' \
    --exclude='.git' \
    "$TEMP_DIR/backend/" "$INSTALL_DIR/backend/"

mkdir -p "$INSTALL_DIR/backend/models" "$INSTALL_DIR/backend/outputs" "$INSTALL_DIR/backend/cache"

# Copy setup scripts
cp -R "$TEMP_DIR/setup_scripts/"* "$INSTALL_DIR/setup_scripts/"
chmod +x "$INSTALL_DIR/setup_scripts/"*.sh

# Copy translation layer
cp "$TEMP_DIR/pyproject.toml" "$INSTALL_DIR/"
cp -R "$TEMP_DIR/translation_layer" "$INSTALL_DIR/"

# Copy icon
if [ -f "$TEMP_DIR/installer/desktopIcon.png" ]; then
    cp "$TEMP_DIR/installer/desktopIcon.png" "$INSTALL_DIR/icon.png"
fi

# Create launcher script
cat > "$INSTALL_DIR/sdapp-launcher.sh" << 'EOF'
#!/bin/bash
INSTALL_DIR="/opt/sdapp"
APP_DIR="$INSTALL_DIR/app"
BACKEND_DIR="$INSTALL_DIR/backend"
VENV_DIR="$BACKEND_DIR/webui-venv"

if [ ! -d "$VENV_DIR" ]; then
    cd "$INSTALL_DIR"
    ./setup_scripts/setup_sdapi_venv.sh
fi

cd "$APP_DIR"
./myApp
EOF
chmod +x "$INSTALL_DIR/sdapp-launcher.sh"

# Create system-wide launcher
ln -sf "$INSTALL_DIR/sdapp-launcher.sh" /usr/local/bin/sdapp

# Create desktop entry
DESKTOP_FILE="/usr/share/applications/sdapp.desktop"
cat > "$DESKTOP_FILE" << DESKTOP_EOF
[Desktop Entry]
Version=1.0
Type=Application
Name=Stable Diffusion Desktop App
Comment=AI Image Generation with Stable Diffusion
Exec=/opt/sdapp/sdapp-launcher.sh
Icon=/opt/sdapp/icon.png
Terminal=false
Categories=Graphics;
DESKTOP_EOF

# Set ownership to actual user
chown -R "$ACTUAL_USER":"$ACTUAL_USER" "$INSTALL_DIR"

# Clean up temporary directory
rm -rf "$TEMP_DIR"

# Run initial setup as actual user
cd "$INSTALL_DIR"
sudo -u "$ACTUAL_USER" ./setup_scripts/setup_sdapi_venv.sh

echo ""
echo "=== Installation Complete ==="
echo "Application installed to: $INSTALL_DIR"
echo "Run 'sdapp' from terminal or via your applications menu."
echo "Models should be placed in: $INSTALL_DIR/backend/models/"
echo ""
echo "For more information, visit: https://github.com/SpicyBananaUI/StableDiffusionDestopApp"
echo ""
