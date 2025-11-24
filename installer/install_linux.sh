#!/bin/bash

# Linux Installer for Stable Diffusion Desktop App
# This script clones the project from git and installs to /opt/sdapp

set -e  # Exit on error

INSTALL_DIR="/opt/sdapp"
APP_NAME="SDApp"
GIT_REPO="https://github.com/SpicyBananaUI/StableDiffusionDestopApp.git"
TEMP_DIR="/tmp/sdapp-install-$$"

echo "=== Stable Diffusion Desktop App - Linux Installer ==="
echo ""

# Check if running as root
if [ "$EUID" -ne 0 ]; then 
    echo "This installer requires root privileges."
    echo "Please run with sudo: sudo $0"
    exit 1
fi

# Get the actual user (not root)
ACTUAL_USER="${SUDO_USER:-$USER}"
ACTUAL_HOME=$(eval echo ~$ACTUAL_USER)

echo "Installing for user: $ACTUAL_USER"
echo "Installation directory: $INSTALL_DIR"
echo ""

# Check for required dependencies
echo "Checking dependencies..."

# Check for git
if ! command -v git &> /dev/null; then
    echo "ERROR: git not found."
    echo "Please install git: sudo apt install git (Ubuntu/Debian) or sudo dnf install git (Fedora)"
    exit 1
fi

# Check for wget or curl
DOWNLOAD_CMD=""
if command -v wget &> /dev/null; then
    DOWNLOAD_CMD="wget"
elif command -v curl &> /dev/null; then
    DOWNLOAD_CMD="curl"
else
    echo "ERROR: Neither wget nor curl found."
    echo "Please install wget: sudo apt install wget (Ubuntu/Debian) or sudo dnf install wget (Fedora)"
    exit 1
fi

echo "Found download tool: $DOWNLOAD_CMD"

# Check for Python 3.10
PYTHON_CMD=""
for py_version in python3.10 python3.11 python3; do
    if command -v $py_version &> /dev/null; then
        PYTHON_CMD=$py_version
        break
    fi
done

if [ -z "$PYTHON_CMD" ]; then
    echo "ERROR: Python 3.10 or 3.11 not found."
    echo "Please install Python 3.10 or 3.11"
    exit 1
fi

PYTHON_VERSION=$($PYTHON_CMD --version)
echo "Found Python: $PYTHON_VERSION"

# Check for pip
if ! $PYTHON_CMD -m pip --version &> /dev/null; then
    echo "ERROR: pip not found."
    echo "Please install pip for $PYTHON_CMD"
    exit 1
fi

# Check for rsync
if ! command -v rsync &> /dev/null; then
    echo "ERROR: rsync not found."
    echo "Please install rsync: sudo apt install rsync (Ubuntu/Debian) or sudo dnf install rsync (Fedora)"
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
    echo "Supported architectures: x86_64, aarch64"
    exit 1
fi

echo "Detected architecture: $ARCH (using $BINARY_ARCH binaries)"
echo ""

# Clone the repository
echo "Cloning repository from GitHub..."
rm -rf "$TEMP_DIR"
mkdir -p "$TEMP_DIR"
sudo -u $ACTUAL_USER git clone "$GIT_REPO" "$TEMP_DIR"

if [ $? -ne 0 ]; then
    echo "ERROR: Failed to clone repository"
    rm -rf "$TEMP_DIR"
    exit 1
fi

echo "Repository cloned successfully"

# Get the latest release tag
echo ""
echo "Fetching latest release information..."
cd "$TEMP_DIR"
LATEST_TAG=$(git describe --tags --abbrev=0 2>/dev/null || echo "main")
echo "Using version: $LATEST_TAG"

# Download pre-built binaries
echo ""
echo "Downloading pre-built .NET binaries..."
BINARY_URL="https://github.com/SpicyBananaUI/StableDiffusionDestopApp/releases/download/${LATEST_TAG}/sdapp-linux-${BINARY_ARCH}.tar.gz"
BINARY_FILE="$TEMP_DIR/sdapp-linux-${BINARY_ARCH}.tar.gz"

if [ "$DOWNLOAD_CMD" = "wget" ]; then
    sudo -u $ACTUAL_USER wget -O "$BINARY_FILE" "$BINARY_URL"
else
    sudo -u $ACTUAL_USER curl -L -o "$BINARY_FILE" "$BINARY_URL"
fi

if [ $? -ne 0 ]; then
    echo "ERROR: Failed to download pre-built binaries from $BINARY_URL"
    echo ""
    echo "This could mean:"
    echo "  1. No release has been published yet for version $LATEST_TAG"
    echo "  2. The release doesn't include binaries for $BINARY_ARCH"
    echo ""
    echo "Please check: https://github.com/SpicyBananaUI/StableDiffusionDestopApp/releases"
    rm -rf "$TEMP_DIR"
    exit 1
fi

echo "Binaries downloaded successfully"

# Create installation directory
echo ""
echo "Creating installation directory..."
mkdir -p "$INSTALL_DIR"

# Extract and install .NET application
echo "Installing application files..."

# Extract .NET frontend
mkdir -p "$INSTALL_DIR/app"
echo "Extracting binaries..."
tar -xzf "$BINARY_FILE" -C "$INSTALL_DIR/app/"
chmod +x "$INSTALL_DIR/app/myApp"

# Copy backend
echo "Copying backend..."
cd "$TEMP_DIR/backend"

rsync -a --progress \
    --exclude='webui-venv' \
    --exclude='venv' \
    --exclude='__pycache__' \
    --exclude='*.pyc' \
    --exclude='*.pyo' \
    --exclude='/cache' \
    --exclude='/models' \
    --exclude='/outputs' \
    --exclude='.git' \
    --exclude='/extensions' \
    --exclude='/embeddings' \
    --exclude='/log' \
    --exclude='/config_states' \
    . "$INSTALL_DIR/backend/"

# Create necessary directories
mkdir -p "$INSTALL_DIR/backend/models"
mkdir -p "$INSTALL_DIR/backend/outputs"
mkdir -p "$INSTALL_DIR/backend/cache"

echo "Backend files copied"

# Copy setup scripts
echo "Copying setup scripts..."
mkdir -p "$INSTALL_DIR/setup_scripts"
cp -R "$TEMP_DIR/setup_scripts"/* "$INSTALL_DIR/setup_scripts/"
chmod +x "$INSTALL_DIR/setup_scripts"/*.sh

# Copy translation layer
echo "Copying translation layer..."
cp "$TEMP_DIR/pyproject.toml" "$INSTALL_DIR/"
cp -R "$TEMP_DIR/translation_layer" "$INSTALL_DIR/"

# Copy icon if available
if [ -f "$TEMP_DIR/installer/desktopIcon.png" ]; then
    cp "$TEMP_DIR/installer/desktopIcon.png" "$INSTALL_DIR/icon.png"
fi

# Create launcher script
echo ""
echo "Creating launcher script..."
cat > "$INSTALL_DIR/sdapp-launcher.sh" << 'LAUNCHER_EOF'
#!/bin/bash

# Launcher script for Stable Diffusion Desktop App

INSTALL_DIR="/opt/sdapp"
APP_DIR="$INSTALL_DIR/app"
BACKEND_DIR="$INSTALL_DIR/backend"
VENV_DIR="$BACKEND_DIR/webui-venv"

# Check if virtual environment exists
if [ ! -d "$VENV_DIR" ]; then
    echo "First-time setup: Creating Python virtual environment..."
    cd "$INSTALL_DIR"
    ./setup_scripts/setup_sdapi_venv.sh
fi

# Launch the application
cd "$APP_DIR"
./myApp
LAUNCHER_EOF

chmod +x "$INSTALL_DIR/sdapp-launcher.sh"

# Create symlink in /usr/local/bin
echo "Creating system-wide launcher..."
ln -sf "$INSTALL_DIR/sdapp-launcher.sh" /usr/local/bin/sdapp

# Create desktop entry
echo ""
echo "Creating desktop entry..."
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
echo ""
echo "Setting permissions..."
chown -R $ACTUAL_USER:$ACTUAL_USER "$INSTALL_DIR"

# Clean up temporary directory
echo "Cleaning up..."
rm -rf "$TEMP_DIR"

# Run initial setup
echo ""
echo "Running initial setup..."
cd "$INSTALL_DIR"
sudo -u $ACTUAL_USER ./setup_scripts/setup_sdapi_venv.sh

echo ""
echo "=== Installation Complete ==="
echo ""
echo "The application has been installed to: $INSTALL_DIR"
echo ""
echo "To launch the application:"
echo "  - Run 'sdapp' from terminal"
echo "  - Or find 'Stable Diffusion Desktop App' in your applications menu"
echo ""
echo "Models should be placed in: $INSTALL_DIR/backend/models/"
echo ""
echo "For more information, visit: https://github.com/SpicyBananaUI/StableDiffusionDestopApp"
echo ""
