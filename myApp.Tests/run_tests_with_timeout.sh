#!/bin/bash

# Test runner script with timeout support
# Usage: ./run_tests_with_timeout.sh [timeout_seconds]

set -e  # Exit on any error

# Set default timeout if not provided
TIMEOUT_SECONDS=${1:-30}

echo "=========================================="
echo "Avalonia Headless Test Runner"
echo "=========================================="
echo "Timeout: ${TIMEOUT_SECONDS} seconds"
echo "To override timeout:"
echo "  - Set environment variable: export TEST_TIMEOUT_SECONDS=60"
echo "  - Pass as argument: ./run_tests_with_timeout.sh 60"
echo "=========================================="
echo ""

# Export timeout for the test configuration
export TEST_TIMEOUT_SECONDS=$TIMEOUT_SECONDS

# Change to project directory
cd "$(dirname "$0")/.."

echo "Building test project..."
dotnet build myApp.Tests/myApp.Tests.csproj --verbosity quiet

if [ $? -ne 0 ]; then
    echo "Build failed. Exiting."
    exit 1
fi

echo "Running tests..."
echo ""

# Run tests with timeout
dotnet test myApp.Tests/myApp.Tests.csproj \
    --verbosity minimal \
    --logger "console;verbosity=minimal" \
    --no-build

echo ""
echo "=========================================="
echo "Test run completed with ${TIMEOUT_SECONDS} second timeout"
echo "=========================================="
