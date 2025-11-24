#!/bin/bash

# Script to build pre-compiled Linux binaries for distribution
# This should be run by developers to create release binaries

set -e  # Exit on error

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
PROJECT_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"
OUTPUT_DIR="$SCRIPT_DIR/linux_binaries"

echo "=== Building Linux Binaries for Distribution ==="
echo ""

# Check for .NET SDK
if ! command -v dotnet &> /dev/null; then
    echo "ERROR: .NET SDK not found."
    echo "Please install .NET 9.0 SDK from: https://dotnet.microsoft.com/download"
    exit 1
fi

DOTNET_VERSION=$(dotnet --version)
echo "Found .NET version: $DOTNET_VERSION"
echo ""

# Clean previous builds
if [ -d "$OUTPUT_DIR" ]; then
    echo "Cleaning previous builds..."
    rm -rf "$OUTPUT_DIR"
fi

mkdir -p "$OUTPUT_DIR"

# Build for linux-x64
echo "Building for linux-x64..."
cd "$PROJECT_ROOT/myApp"
dotnet publish -c Release -r linux-x64 --self-contained true -p:PublishSingleFile=false

if [ $? -ne 0 ]; then
    echo "ERROR: linux-x64 build failed"
    exit 1
fi

PUBLISH_DIR_X64="$PROJECT_ROOT/myApp/bin/Release/net9.0/linux-x64/publish"
if [ ! -d "$PUBLISH_DIR_X64" ]; then
    echo "ERROR: linux-x64 publish directory not found"
    exit 1
fi

# Create tarball for x64
echo "Creating tarball for linux-x64..."
cd "$PUBLISH_DIR_X64"
tar -czf "$OUTPUT_DIR/sdapp-linux-x64.tar.gz" .
echo "Created: sdapp-linux-x64.tar.gz"
echo ""

# Build for linux-arm64
echo "Building for linux-arm64..."
cd "$PROJECT_ROOT/myApp"
dotnet publish -c Release -r linux-arm64 --self-contained true -p:PublishSingleFile=false

if [ $? -ne 0 ]; then
    echo "ERROR: linux-arm64 build failed"
    exit 1
fi

PUBLISH_DIR_ARM64="$PROJECT_ROOT/myApp/bin/Release/net9.0/linux-arm64/publish"
if [ ! -d "$PUBLISH_DIR_ARM64" ]; then
    echo "ERROR: linux-arm64 publish directory not found"
    exit 1
fi

# Create tarball for arm
echo "Creating tarball for linux-arm64..."
cd "$PUBLISH_DIR_ARM64"
tar -czf "$OUTPUT_DIR/sdapp-linux-arm64.tar.gz" .
echo "Created: sdapp-linux-arm64.tar.gz"
echo ""

# Generate checksums
echo "Generating checksums..."
cd "$OUTPUT_DIR"
sha256sum sdapp-linux-x64.tar.gz > sdapp-linux-x64.tar.gz.sha256
sha256sum sdapp-linux-arm64.tar.gz > sdapp-linux-arm64.tar.gz.sha256

echo ""
echo "=== Build Complete ==="
echo ""
echo "Binaries created in: $OUTPUT_DIR"
echo ""
echo "Files:"
ls -lh "$OUTPUT_DIR"
echo ""
echo "These binaries should be committed to the repository."
echo "The installer will use them automatically when users run install_linux.sh"
echo ""
