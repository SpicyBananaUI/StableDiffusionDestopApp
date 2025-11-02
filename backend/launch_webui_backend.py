import os
import platform
import torch
import sys
import subprocess
import logging
import json
import secrets

AUTH_FILE = os.path.join(os.path.dirname(__file__), "auth.json")

def generate_api_key():
    return secrets.token_hex(16)  # 32-character random key

def setup_auth():
    if not os.path.exists(AUTH_FILE):
        key = generate_api_key()
        data = {"api_key": key}
        with open(AUTH_FILE, "w") as f:
            json.dump(data, f)
        print(f"[SECURITY] Generated new API key: {key}")
        print("[SECURITY] Share this key with trusted clients to allow privileged operations.")
    else:
        with open(AUTH_FILE) as f:
            data = json.load(f)
        print(f"[SECURITY] Using existing API key from auth.json: {data['api_key']}")
    return data["api_key"]

API_KEY = setup_auth()

# Setup logger
logging.basicConfig(level=logging.INFO)
logger = logging.getLogger("device-check")
logger.setLevel(logging.DEBUG)

def test_device(device):
    """Use to verify functionality of selected device before proceeding (CUDA, MPS, CPU)"""
    try:
        _ = torch.tensor([1.0], device=device) * 2  # Simple operation
        logger.info(f"Device {device} is working correctly.")
        return True
    except Exception as e:
        logger.warning(f"Device {device} failed execution test: {e}")
        return False

# Detect OS
system = platform.system()
MACOS = system == "Darwin"
WINDOWS = system == "Windows"
LINUX = system == "Linux"

# Base commandline args
args = []

device = "cpu"  # Default fallback

# debug torch/cuda
print(torch.__file__)
print(torch.__version__)
print(torch.version.cuda)
print(torch.cuda.is_available())

if MACOS:
    os.environ["install_dir"] = "$HOME"
    os.environ["PYTORCH_ENABLE_MPS_FALLBACK"] = "1"

    if getattr(torch.backends, "mps", None) and torch.backends.mps.is_available():
        if test_device("mps"):
            device = "mps"
            args += ["--skip-torch-cuda-test", "--upcast-sampling", "--no-half-vae", "--use-cpu interrogate"]
            # Make sure even UNet and attention are in full precision (MPS is broken with half precision)
            args += ["--no-half", "--precision full"]
        else:
            logger.warning("MPS is available but failed test. Falling back to CPU.")
    else:
        logger.warning("MPS is not available. Falling back to CPU.")

elif WINDOWS or LINUX:
    if torch.cuda.is_available():
        if test_device("cuda"):
            device = "cuda"
        else:
            logger.warning("CUDA is available but failed test. Falling back to CPU.")
    else:
        logger.warning("CUDA is not available. Falling back to CPU.")
        logger.warning("If you have a compatible device, try installing CUDA and the corresponding torch build at https://pytorch.org/get-started/locally/")

# If CPU fallback used, pass args to the backend
if device == "cpu":    
    args += ["--skip-torch-cuda-test", "--upcast-sampling", "--no-half-vae", "--use-cpu interrogate"]

# Disable web UI and enable API irrespective of platform/device
args += ["--nowebui", "--api", "--api-log", "--loglevel WARNING"]

# Skip git operations and environment preparation for installer builds
args += ["--skip-version-check", "--skip-prepare-environment"]

# Let the backend use its default models directory (backend/models/Stable-diffusion)
# This is the standard location that works both in development and installer
# No need to override with --ckpt-dir unless user wants a custom location

# Set final command line args
os.environ["COMMANDLINE_ARGS"] = " ".join(args)

# Optional debug
logger.info(f"Launching with device: {device}")
logger.debug(f"COMMANDLINE_ARGS: {os.environ['COMMANDLINE_ARGS']}")

# Launch backend
import launch
print("Launching Stable Diffusion WebUI backend...")
launch.main()
