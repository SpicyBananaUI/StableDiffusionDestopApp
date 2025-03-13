from fastapi import FastAPI, HTTPException
from fastapi.responses import FileResponse
import torch
import os

# Check if NumPy is available before importing diffusers
try:
    import numpy as np
    from diffusers import StableDiffusionPipeline
except ImportError as e:
    print(f"Critical import error: {e}")
    print("Please ensure NumPy is installed: pip install numpy")
    raise

app = FastAPI()

model_id = "CompVis/stable-diffusion-v1-4"

# Check if CUDA is available and set device accordingly
if torch.cuda.is_available():
    device = "cuda"
    print("Using GPU acceleration with CUDA")
else:
    device = "cpu"
    print("CUDA not available, using CPU for processing (this will be slow)")

# Make sure the output directory exists
output_dir = "outputImages"
os.makedirs(output_dir, exist_ok=True)

# Define the image filename
image_filename = "generated_image.png"

pipe = StableDiffusionPipeline.from_pretrained(model_id)
pipe = pipe.to(device)

@app.get("/")
async def root():
    return {"message": "Hello World"}

@app.get("/hello/{name}")
async def say_hello(name: str):
    return {"message": f"Hello {name}"}

@app.get("/photo/{prompt}/{steps}/{scale}/")
def create_photo(prompt: str, steps: int, scale: float):
    try:
        image = pipe(
            prompt,
            num_inference_steps=steps,
            guidance_scale=scale,
        ).images[0]
        
        # Get absolute path for the image file
        image_path = os.path.join(os.path.abspath(output_dir), image_filename)
        
        # Save the image
        image.save(image_path)
        
        return {
            "message": "Done!",
            "image_path": image_path
        }
    except RuntimeError as e:
        print(f"Error during image generation: {str(e)}")
        raise HTTPException(status_code=500, detail=f"Image generation failed: {str(e)}")

@app.get("/image")
async def get_image():
    """Endpoint to directly serve the generated image"""
    image_path = os.path.join(os.path.abspath(output_dir), image_filename)
    if os.path.exists(image_path):
        return FileResponse(image_path)
    else:
        raise HTTPException(status_code=404, detail="Image not found")
