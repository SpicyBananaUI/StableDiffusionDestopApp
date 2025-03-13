from fastapi import FastAPI
import torch
from diffusers import StableDiffusionPipeline
app = FastAPI()

model_id = "CompVis/stable-diffusion-v1-4"

if torch.cuda.is_available():
    print("CUDA is available.")
    device = torch.device("cuda")
    print("Device name:", torch.cuda.get_device_name(0))
    print("CUDA version:", torch.version.cuda)
    print("CUDNN version:", torch.backends.cudnn.version())
elif torch.backends.mps.is_available():
    print("MPS (Metal Performance Shaders) is available.")
    device = torch.device("mps")
    mps_device = torch.device("mps")
    print("MPS device:", mps_device)
    print("PyTorch MPS backend is enabled:", torch.backends.mps.is_built())
else:
    print("No GPU acceleration available. Using CPU.")
    device = torch.device("cpu")

print("Using device:", device)

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
    ##prompt = "a photo of an astronaut riding a horse on mars"
    ##prompt = "A capybara holding a sign that reads Hello World"
    image = pipe(
        prompt,
        num_inference_steps=steps,
        guidance_scale= scale,
    ).images[0]
    image.save("outputImages/capybara.png")
    return {"message": "Done!"}
