import logging
import os
from datetime import datetime



from fastapi import FastAPI
from fastapi.responses import FileResponse # TODO: respond to api call with file to allow for host and client on different machines
from fastapi import Path
from fastapi.middleware.trustedhost import TrustedHostMiddleware
from fastapi import Security, HTTPException, Depends
from fastapi.security.api_key import APIKeyHeader
from pydantic import BaseModel



import torch
from diffusers import StableDiffusionPipeline


OUTPUT_DIR_IMAGES = "outputImages"
# Set the host based on the deployment mode
DEPLOY_MODE = os.getenv("DEPLOY_MODE", "local")  # Default to 'local'
# Required if using remotely
API_KEY = os.getenv("API_KEY", None)  # Set this in an environment variable
API_KEY_NAME = "X-API-Key"
api_key_header = APIKeyHeader(name=API_KEY_NAME, auto_error=False)

# Configure logging
logging.basicConfig(level=logging.INFO)
logger = logging.getLogger(__name__)

# Check if NumPy is available before importing diffusers
try:
    import numpy as np
    from diffusers import StableDiffusionPipeline
except ImportError as e:
    logger.error(f"Critical import error: {e}")
    logger.info("Please ensure NumPy is installed: pip install numpy")
    raise

import uvicorn
app = FastAPI()

model_id = "CompVis/stable-diffusion-v1-4"


if DEPLOY_MODE == "local":
    HOST = "127.0.0.1"
    logger.info("Running in LOCAL mode: API accessible only from this machine.")
else:
    HOST = "0.0.0.0"  # Allow external access
    logger.warning("Running in NETWORK mode: API is accessible remotely!")

# Use to verify key of client in remote use, in local use always accept
# TODO: use https instead of http
# TODO: verify security, may need additional measures/authentication
def verify_api_key(api_key: str = Security(api_key_header)):
    if DEPLOY_MODE == "local":
        return  # No authentication required in local mode
    if not API_KEY or api_key != API_KEY:
        raise HTTPException(status_code=403, detail="Invalid or missing API key.")

# Use to verify functionality of selected device before proceeding (CUDA, MPS, CPU)
def test_device(device):
    try:
        _ = torch.tensor([1.0], device=device) * 2  # Simple operation
        logger.info(f"Device {device} is working correctly.")
        return True
    except Exception as e:
        logger.warning(f"Device {device} failed execution test: {e}")
        return False

device = None

if torch.cuda.is_available():
    try:
        device = torch.device("cuda")
        logger.info(f"Using CUDA: {torch.cuda.get_device_name(0)} (CUDA version {torch.version.cuda})")
        if not test_device(device): device = None # Make sure it can actually execute

    except Exception as e:
        logger.error(f"Failed to use CUDA: {e}")
        device = None

if device is None and torch.backends.mps.is_available():
    try:
        device = torch.device("mps")
        logger.info("Using MPS (Metal Performance Shaders).")
        if not test_device(device): device = None # Make sure it can actually execute

    except Exception as e:
        logger.error(f"Failed to use MPS: {e}")
        device = None

if device is None:
    try:
        device = torch.device("cpu")
        logger.info("Using CPU as fallback.")
        if not test_device(device): device = None # Make sure it can actually execute

    except Exception as e:
        logger.critical(f"Failed to set device to CPU: {e}")
        raise RuntimeError("Fatal error: No available device for model execution.") # ur cooked buddy

logger.info(f"Using device: {device}")

# Load the model
try:
    pipe = StableDiffusionPipeline.from_pretrained(model_id)
    pipe.to(device)
    logger.info("Stable Diffusion model loaded successfully.")
except Exception as e:
    logger.error(f"Error loading Stable Diffusion model: {e}")
    raise RuntimeError("Failed to load the model.")

pipe = StableDiffusionPipeline.from_pretrained(model_id)
pipe = pipe.to(device)

@app.get("/")
async def root():
    return {"message": "Hello World"}

# TODO: verify functionality in remote mode when supported
@app.get("/secure-endpoint/")
async def secure_data(api_key: str = Depends(verify_api_key)):
    return {"message": "You have access!"}

@app.get("/hello/{name}")
async def say_hello(name: str):
    return {"message": f"Hello {name}"}

# TODO: If UI constraints on params change, change them here too (and vice versa)
# TODO: In the future, have UI query the range from here, get the range from config
@app.get("/photo/{prompt}/{steps}/{scale}")
async def create_photo(
    prompt: str,
    steps: int = Path(..., ge=1, le=50),  # Ensure steps is between 1 and 50
    scale: float = Path(..., ge=1.0, le=15.0)  # Ensure scale is between 1.0 and 15.0
):
    if len(prompt) > 200:
        raise HTTPException(status_code=400, detail="Prompt is too long.")

    try:
        image = pipe(prompt, num_inference_steps=steps, guidance_scale=scale).images[0]

        # TODO: need to standardize run location or something, since this directory just gets plopped wherever the python3.10 [path]/main.py command was run from
        output_dir = OUTPUT_DIR_IMAGES
        os.makedirs(output_dir, exist_ok=True)  # Ensure directory exists

        timestamp = datetime.now().strftime("%Y-%m-%d_%H-%M-%S")
        image_path = os.path.join(os.path.abspath(output_dir), f"generated_image_{timestamp}.png")

        # TODO: use proper exif metadata 
        image.info['model_used'] = model_id
        image.info['prompt'] = prompt

        image.save(image_path)

        return {"message": "Image generated successfully!", "path": image_path}
        # When the UI can handle it, 
        # return FileResponse(image_path, media_type="image/png")
    except Exception as e:
        logger.error(f"Error generating image: {e}")
        raise HTTPException(status_code=500, detail="Image generation failed.")

# Start server. For now this is handled by the run_server scripts
"""
if __name__ == "__main__":
    logger.info(f"Starting server...")
    uvicorn.run("main:app", host=HOST, port=8000, reload=True)
"""
