from fastapi import FastAPI
import torch
from diffusers import StableDiffusionPipeline
app = FastAPI()

model_id = "CompVis/stable-diffusion-v1-4"
device = "cpu"
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
