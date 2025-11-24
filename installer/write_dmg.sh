#!/bin/bash

# Script to build macOS DMG installer
# This creates SDapp.app bundle and packages it into SDapp.dmg

set -e  # Exit on error

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
PROJECT_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"
BUILD_DIR="$SCRIPT_DIR/dmg_build"
APP_NAME="SDApp"
APP_BUNDLE="$BUILD_DIR/${APP_NAME}.app"
DMG_NAME="SDapp.dmg"
DMG_OUTPUT="$SCRIPT_DIR/$DMG_NAME"

echo "=== Building macOS DMG Installer ==="

# Clean previous build
if [ -d "$BUILD_DIR" ]; then
    echo "Cleaning previous build..."
    rm -rf "$BUILD_DIR"
fi

mkdir -p "$BUILD_DIR"

# Step 1: Build .NET app for macOS
echo ""
echo "Step 1: Building .NET frontend for macOS..."
cd "$PROJECT_ROOT/myApp"

# Detect architecture
ARCH=$(uname -m)
if [ "$ARCH" = "arm64" ]; then
    RUNTIME="osx-arm64"
else
    RUNTIME="osx-x64"
fi

echo "Building for runtime: $RUNTIME"

dotnet publish -c Release -r "$RUNTIME" --self-contained true

if [ $? -ne 0 ]; then
    echo "ERROR: .NET build failed"
    exit 1
fi

PUBLISH_DIR="$PROJECT_ROOT/myApp/bin/Release/net9.0/$RUNTIME/publish"

if [ ! -d "$PUBLISH_DIR" ]; then
    echo "ERROR: Publish directory not found: $PUBLISH_DIR"
    exit 1
fi

echo ".NET build completed"

# Step 2: Create app bundle structure
echo ""
echo "Step 2: Creating app bundle structure..."

mkdir -p "$APP_BUNDLE/Contents"
mkdir -p "$APP_BUNDLE/Contents/MacOS"
mkdir -p "$APP_BUNDLE/Contents/Resources"
mkdir -p "$APP_BUNDLE/Contents/backend"
mkdir -p "$APP_BUNDLE/Contents/setup_scripts"

# Step 3: Create Info.plist
echo ""
echo "Step 3: Creating Info.plist..."
cat > "$APP_BUNDLE/Contents/Info.plist" << EOF
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
    <key>CFBundleDevelopmentRegion</key>
    <string>en</string>
    <key>CFBundleExecutable</key>
    <string>myApp</string>
    <key>CFBundleIdentifier</key>
    <string>com.spicybanana.sdapp</string>
    <key>CFBundleInfoDictionaryVersion</key>
    <string>6.0</string>
    <key>CFBundleName</key>
    <string>$APP_NAME</string>
    <key>CFBundlePackageType</key>
    <string>APPL</string>
    <key>CFBundleShortVersionString</key>
    <string>1.0</string>
    <key>CFBundleVersion</key>
    <string>1</string>
    <key>LSMinimumSystemVersion</key>
    <string>10.15</string>
    <key>NSHighResolutionCapable</key>
    <true/>
</dict>
</plist>
EOF

# Step 4: Copy .NET published files
echo ""
echo "Step 4: Copying .NET application files..."
cp -R "$PUBLISH_DIR"/* "$APP_BUNDLE/Contents/MacOS/"

# Make sure the executable is named correctly
if [ -f "$APP_BUNDLE/Contents/MacOS/myApp" ]; then
    echo "Executable found: myApp"
elif [ -f "$APP_BUNDLE/Contents/MacOS/myApp.exe" ]; then
    mv "$APP_BUNDLE/Contents/MacOS/myApp.exe" "$APP_BUNDLE/Contents/MacOS/myApp"
    chmod +x "$APP_BUNDLE/Contents/MacOS/myApp"
fi

# Step 5: Copy backend directory (exclude large files)
echo ""
echo "Step 5: Copying backend directory..."
cd "$PROJECT_ROOT/backend"

# Copy backend files, excluding large/cache directories
rsync -av --progress \
    --exclude='webui-venv' \
    --exclude='venv' \
    --exclude='__pycache__' \
    --exclude='*.pyc' \
    --exclude='*.pyo' \
    --exclude='/venv' \
    --exclude='/webui-venv' \
    --exclude='/cache' \
    --exclude='/models' \
    --exclude='/outputs' \
    --exclude='.git' \
    --exclude='/extensions' \
    --exclude='/embeddings' \
    --exclude='/log' \
#     --exclude='/localizations' \ # TODO: should not copy contents, but do need the empty dir
    --exclude='/config_states' \
    . "$APP_BUNDLE/Contents/backend/"

# Create empty directories that might be needed
mkdir -p "$APP_BUNDLE/Contents/backend/models"
mkdir -p "$APP_BUNDLE/Contents/backend/outputs"
mkdir -p "$APP_BUNDLE/Contents/backend/cache"

echo "Backend files copied"

# Step 6: Copy setup_scripts directory
echo ""
echo "Step 6: Copying setup scripts..."
cp -R "$PROJECT_ROOT/setup_scripts"/* "$APP_BUNDLE/Contents/setup_scripts/"

# Make scripts executable
chmod +x "$APP_BUNDLE/Contents/setup_scripts"/*.sh

echo "Setup scripts copied"

# Step 6.1: Copy translation layer and pyproject.toml
echo ""
echo "Step 6.1: Copying translation layer..."
cp "$PROJECT_ROOT/pyproject.toml" "$APP_BUNDLE/Contents/"
cp -R "$PROJECT_ROOT/translation_layer" "$APP_BUNDLE/Contents/"
echo "Translation layer copied"

# Step 7: Copy app icon if available
if [ -f "$SCRIPT_DIR/desktopIcon.png" ]; then
    echo ""
    echo "Step 7: Copying app icon..."
    cp "$SCRIPT_DIR/desktopIcon.png" "$APP_BUNDLE/Contents/Resources/icon.png"
    
    # Create icon set if iconutil is available (optional)
    if command -v iconutil &> /dev/null && [ -d "$SCRIPT_DIR/desktopIcon.iconset" ]; then
        iconutil -c icns "$SCRIPT_DIR/desktopIcon.iconset" -o "$APP_BUNDLE/Contents/Resources/icon.icns"
    fi
fi

# Step 8: Set permissions
echo ""
echo "Step 8: Setting permissions..."
chmod +x "$APP_BUNDLE/Contents/MacOS"/*

# Step 9: Create DMG
echo ""
echo "Step 9: Creating DMG..."

# Remove existing DMG if it exists
if [ -f "$DMG_OUTPUT" ]; then
    echo "Removing existing DMG..."
    rm "$DMG_OUTPUT"
fi

# Create a temporary directory for DMG contents
DMG_CONTENTS="$BUILD_DIR/dmg_contents"
mkdir -p "$DMG_CONTENTS"

# Copy app bundle to DMG contents
cp -R "$APP_BUNDLE" "$DMG_CONTENTS/"

# Create Applications symlink
ln -s /Applications "$DMG_CONTENTS/Applications"

# Create DMG using hdiutil
echo "Creating DMG image..."
hdiutil create -volname "$APP_NAME" \
    -srcfolder "$DMG_CONTENTS" \
    -ov \
    -format UDZO \
    "$DMG_OUTPUT"

if [ $? -ne 0 ]; then
    echo "ERROR: Failed to create DMG"
    exit 1
fi

# Clean up build directory
rm -rf "$BUILD_DIR"

echo ""
echo "=== DMG Build Complete ==="
echo "DMG created at: $DMG_OUTPUT"
echo ""
echo "To test the DMG:"
echo "  1. Double-click $DMG_OUTPUT"
echo "  2. Drag $APP_NAME.app to Applications folder"
echo "  3. Run from Applications or:"
echo "     /Applications/$APP_NAME.app/Contents/MacOS/myApp"

