# Copyright (c) 2025 Spicy Banana
# SPDX-License-Identifier: AGPL-3.0-only


import os
import platform
import torch
import sys
import subprocess
import logging

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

# Add user AppData models directory for downloaded models (no admin required)
user_models_dir = os.path.join(os.environ.get("LOCALAPPDATA", ""), "StableDiffusion", "models", "Stable-diffusion")
if os.path.exists(user_models_dir):
    args += [f"--ckpt-dir={user_models_dir}"]

# Set final command line args
os.environ["COMMANDLINE_ARGS"] = " ".join(args)

# Optional debug
logger.info(f"Launching with device: {device}")
logger.debug(f"COMMANDLINE_ARGS: {os.environ['COMMANDLINE_ARGS']}")

# Launch backend
import launch
print("Launching Stable Diffusion WebUI backend...")
launch.main()
