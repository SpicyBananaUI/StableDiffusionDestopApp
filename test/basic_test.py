# Copyright (c) 2025 Spicy Banana
# SPDX-License-Identifier: AGPL-3.0-only


import requests
import base64
from PIL import Image
from io import BytesIO
"""Simple Call to txt2img endpoint to verify server operation"""

# API endpoint
url = "http://127.0.0.1:7861/sdapi/v1/txt2img"

# Request payload
payload = {
    "prompt": "a spicy banana drawing an image, photorealistic",
    "steps": 20,
    "cfg_scale": 7.0,
    "width": 512,
    "height": 512
}

print(f"Sending POST request for prompt: \"{payload["prompt"]}\"")

# Send the POST request
response = requests.post(url, json=payload)
response.raise_for_status()

print(f"Got response for prompt: \"{payload["prompt"]}\"")

# Get the image data from the response
data = response.json()
image_base64 = data['images'][0]  # first generated image
print(f"Raw base64 image length: {len(image_base64)}")

# Decode the image and save it
image_data = base64.b64decode(image_base64)
image = Image.open(BytesIO(image_data))
image.save("./test/test_output.png")

print("Image saved to test_output.png")
