import os
import platform
import torch
import sys
import subprocess
import logging
import json
import secrets
import getpass

# Ensure backend modules are importable when running under the embedded Python distribution
BACKEND_ROOT = os.path.dirname(os.path.abspath(__file__))
if BACKEND_ROOT not in sys.path:
    sys.path.insert(0, BACKEND_ROOT)

DEBUG_PATHS = os.environ.get("SDAPP_DEBUG_PATHS") == "1"
if DEBUG_PATHS:
    print(f"[debug] BACKEND_ROOT set to: {BACKEND_ROOT}")
    print(f"[debug] sys.path[:5] => {sys.path[:5]}")
    print(f"[debug] translation_layer present: {os.path.isdir(os.path.join(BACKEND_ROOT, 'translation_layer'))}")

AUTH_FILE = os.path.join(os.path.dirname(__file__), "auth.json")

def generate_api_key():
    return secrets.token_hex(16)  # 32-character random key

def secure_auth_file(path):
    """Restrict auth.json permissions to the current user."""
    try:
        if os.name == "nt":
            current_user = getpass.getuser()
            subprocess.run(["icacls", path, "/inheritance:r"], check=True, stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL)
            subprocess.run(["icacls", path, "/grant:r", f"{current_user}:(F)"], check=True, stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL)
        else:
            os.chmod(path, 0o600)
    except Exception as exc:
        print(f"[SECURITY] Warning: Unable to restrict auth.json permissions ({exc})")

def prompt_copy_api_key(key):
    print("[SECURITY] Generated a new API key. Copy and store it securely; it will not be displayed again.")
    print(f"[SECURITY] API key: {key}")
    try:
        if sys.stdin and sys.stdin.isatty():
            input("Press Enter after copying the key...")
    except EOFError:
        # Non-interactive shells cannot prompt; best effort only.
        pass

def setup_auth():
    data = None
    if not os.path.exists(AUTH_FILE):
        key = generate_api_key()
        data = {"api_key": key}
        with open(AUTH_FILE, "w", encoding="utf-8") as f:
            json.dump(data, f)
        secure_auth_file(AUTH_FILE)
        prompt_copy_api_key(key)
        return key

    try:
        with open(AUTH_FILE, encoding="utf-8") as f:
            data = json.load(f)
    except (json.JSONDecodeError, FileNotFoundError, KeyError):
        print("[SECURITY] auth.json was missing or invalid; generating a new API key.")
        try:
            os.remove(AUTH_FILE)
        except FileNotFoundError:
            pass
        return setup_auth()

    api_key = data.get("api_key")
    if not api_key:
        print("[SECURITY] auth.json did not contain an api_key; generating a new one.")
        os.remove(AUTH_FILE)
        return setup_auth()

    secure_auth_file(AUTH_FILE)
    return api_key

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

# Activate the translation layer before launching the backend
translation_layer_imported = False
try:
    import translation_layer
    translation_layer_imported = True
    print("[translation_layer] Interceptor auto-activated in launch_webui_backend.py")
except Exception as e:
    print(f"[translation_layer] Failed to import/activate: {e}")
finally:
    if DEBUG_PATHS:
        print(f"[debug] translation_layer import success: {translation_layer_imported}")

# Launch backend
import launch
print("Launching Stable Diffusion WebUI backend...")
launch.main()
