#!/bin/bash

# Test execution script for myApp.Tests
# This script runs the tests with proper configuration

echo "Running myApp.Tests..."

# Build the test project
echo "Building test project..."
dotnet build myApp.Tests.csproj

if [ $? -ne 0 ]; then
    echo "Build failed!"
    exit 1
fi

# Run tests with detailed output
echo "Running tests..."
dotnet test myApp.Tests.csproj \
    --verbosity normal \
    --logger "console;verbosity=detailed" \
    --collect:"XPlat Code Coverage"

echo "Tests completed!"
