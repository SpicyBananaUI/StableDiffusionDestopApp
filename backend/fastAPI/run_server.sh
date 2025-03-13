#!/bin/bash
echo "Starting FastAPI server..."

# Activate the virtual environment
source venv/bin/activate

# Run the FastAPI server
uvicorn main:app --reload

# The script will stay here until the server is stopped with Ctrl+C
# After that, we deactivate the virtual environment
deactivate
