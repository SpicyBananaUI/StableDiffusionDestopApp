import os
import sys
import subprocess

MACOS = True

if MACOS:
    os.environ["install_dir"] = "$HOME"
    os.environ["COMMANDLINE_ARGS"] = "--skip-torch-cuda-test --upcast-sampling --no-half-vae --use-cpu interrogate"
    os.environ["PYTORCH_ENABLE_MPS_FALLBACK"] = "1"

# dont launch the web ui, just the backend and api.
if "COMMANDLINE_ARGS" in os.environ:
    os.environ["COMMANDLINE_ARGS"] += " --nowebui --api --api-log"
else:
    os.environ["COMMANDLINE_ARGS"] = "--nowebui --api --api-log"

import launch

launch.main()