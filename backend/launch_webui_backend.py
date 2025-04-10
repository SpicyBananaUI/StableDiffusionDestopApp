import os
import sys
import subprocess

MACOS = True

# Temporary include of SD webui before we fork/copy the needed code
webui_path = os.path.abspath("/Users/ronan/Library/Application Support/StabilityMatrix/Packages/stable-diffusion-webui-forge/")
sys.path.append(webui_path)

if MACOS:
    os.environ["install_dir"] = "$HOME"
    os.environ["COMMANDLINE_ARGS"] = "--skip-torch-cuda-test --upcast-sampling --no-half-vae --use-cpu interrogate"
    os.environ["PYTORCH_ENABLE_MPS_FALLBACK"] = "1"

# dont launch the web ui, just the backend and api.
if "COMMANDLINE_ARGS" in os.environ:
    os.environ["COMMANDLINE_ARGS"] += " --nowebui --api"
else:
    os.environ["COMMANDLINE_ARGS"] = "--nowebui --api"

import launch

launch.main()