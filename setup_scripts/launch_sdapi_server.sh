#!/bin/bash
# Usage: launch_sdapi_server.sh [--remote-server|--local-backend]
#   --remote-server: Run in foreground in terminal (user can Ctrl+C to quit)
#   --local-backend: Run in background, dies when parent process dies

MODE="${1:---local-backend}"  # Default to local-backend if no argument
EXTRA_ARGS="${@:2}"       # Capture all arguments starting from the second one

BACKEND_DIR="./backend"
SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
LOG_FILE="$SCRIPT_DIR/../sdapi_server.log"

# Enable logging if not already redirected
if [ -z "$SDAPI_LOGGING_ENABLED" ]; then
    export SDAPI_LOGGING_ENABLED=1
    exec > >(tee -a "$LOG_FILE") 2>&1
fi

echo "=== Launching SDAPI Server ==="
echo "Date: $(date)"
echo "Script: $0"
echo "Args: $@"
echo "Log file: $LOG_FILE"

cd "$SCRIPT_DIR"
cd ".."



# Add common paths to PATH to ensure we can find python
export PATH="/usr/local/bin:/opt/homebrew/bin:/Library/Frameworks/Python.framework/Versions/3.10/bin:$PATH"

echo "PATH: $PATH"

# Check if Python is installed
PYTHON_CMD="python3.10"
if ! command -v $PYTHON_CMD &> /dev/null; then
    # Try absolute paths
    if [ -f "/usr/local/bin/python3.10" ]; then
        PYTHON_CMD="/usr/local/bin/python3.10"
    elif [ -f "/opt/homebrew/bin/python3.10" ]; then
        PYTHON_CMD="/opt/homebrew/bin/python3.10"
    elif [ -f "/Library/Frameworks/Python.framework/Versions/3.10/bin/python3.10" ]; then
        PYTHON_CMD="/Library/Frameworks/Python.framework/Versions/3.10/bin/python3.10"
    else
        echo "Python 3.10 is not installed or not found in PATH."
        echo "Please install Python 3.10 from python.org"
        exit 1
    fi
fi

echo "Found Python 3.10 at: $(command -v $PYTHON_CMD || echo $PYTHON_CMD)"

pwd

# Enter the Backend directory
if ! cd "$BACKEND_DIR"; then
    echo "ERROR: Failed to change to backend directory: $BACKEND_DIR"
    echo "Current directory: $(pwd)"
    exit 1
fi

echo "Changed to backend directory: $(pwd)"

# Verify we're in the right place
if [ ! -f "launch_webui_backend.py" ]; then
    echo "ERROR: launch_webui_backend.py not found in $(pwd)"
    exit 1
fi

if [ ! -d "webui-venv" ]; then
    echo "ERROR: webui-venv directory not found in $(pwd)"
    exit 1
fi

# Activate the virtual environment
echo "Activating virtual environment..."
source webui-venv/bin/activate

echo "Launching sdapi (webui backend)..."
echo "You can verify functionality by python ./test/basic_test.py"

# Use Python 3.10 - MUST be python3.10 specifically
# Check if python3.10 exists in venv, otherwise use system python3.10 (with venv activated, it will use venv packages)
VENV_PYTHON="$(pwd)/webui-venv/bin/python3.10"
if [ -f "$VENV_PYTHON" ] && [ -x "$VENV_PYTHON" ]; then
    PYTHON_EXEC="$VENV_PYTHON"
    echo "Using venv Python: $PYTHON_EXEC"
else
    # Use system python3.10 - with venv activated, it will use venv packages
    PYTHON_EXEC="$PYTHON_CMD"
    echo "Using system Python: $PYTHON_EXEC"
    # Verify it exists
    if ! command -v "$PYTHON_EXEC" &> /dev/null && [ ! -f "$PYTHON_EXEC" ]; then
        echo "ERROR: python3.10 not found in venv or system PATH"
        exit 1
    fi
fi

# Final verification that Python executable works
if ! "$PYTHON_EXEC" --version &> /dev/null; then
    echo "ERROR: Cannot execute Python: $PYTHON_EXEC"
    exit 1
fi

if [ "$MODE" = "--remote-server" ]; then
    # Remote server mode: Run in foreground so user can Ctrl+C to quit
    # The terminal window will stay open and show the output
    echo "Running in remote server mode (foreground). Press Ctrl+C to stop."
    echo "Current directory: $(pwd)"
    echo "Python executable: $PYTHON_EXEC"
    echo "Launching backend..."
    
    # Run Python and capture exit code
    "$PYTHON_EXEC" launch_webui_backend.py $EXTRA_ARGS
    EXIT_CODE=$?
    
    if [ $EXIT_CODE -ne 0 ]; then
        echo ""
        echo "Backend exited with error code: $EXIT_CODE"
        echo "Press Enter to close this window..."
        read -r
    fi
elif [ "$MODE" = "--local-backend" ]; then
    # Local backend mode: Run in background but DON'T disown
    # This allows it to die when the parent C# process exits
    # Output goes to terminal so user can see it
    echo "Running in local backend mode (background, attached to parent)."
    
    # Run in background
    "$PYTHON_EXEC" launch_webui_backend.py $EXTRA_ARGS &
    
    # Get the PID
    BACKEND_PID=$!
    
    echo "Backend started with PID: $BACKEND_PID"
    
    # Don't disown - keep it as a child process so it dies when parent dies
    # Exit immediately so the parent C# process doesn't wait
    exit 0
else
    echo "Unknown mode: $MODE"
    echo "Usage: $0 [--remote-server|--local-backend]"
    exit 1
fi
