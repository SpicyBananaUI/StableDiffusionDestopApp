import os
import hashlib
from threading import Lock, Thread
from uuid import uuid4
import requests

ALLOWED_EXTENSIONS = {".safetensors", ".ckpt", ".pt", ".pth", ".bin", ".onnx"}


# Public API of this module
# - start_model_download(model_url, checksum, target_dir, file_name) -> str (download_id)
# - get_download_progress(download_id) -> dict


# Tracks progress of background model downloads keyed by a generated download_id
DOWNLOADS_PROGRESS = {}
DOWNLOADS_LOCK = Lock()
# Map of absolute target file path -> download_id for active downloads
ACTIVE_DOWNLOADS = {}


def _init_download_state(download_id: str, file_path: str):
    with DOWNLOADS_LOCK:
        DOWNLOADS_PROGRESS[download_id] = {
            "status": "in_progress",  # in_progress | completed | failed
            "downloaded_bytes": 0,
            "total_bytes": 0,
            "error": None,
            "file_path": file_path,
        }


def _update_download_progress(download_id: str, downloaded: int, total: int):
    with DOWNLOADS_LOCK:
        state = DOWNLOADS_PROGRESS.get(download_id)
        if state is not None:
            state["downloaded_bytes"] = downloaded
            state["total_bytes"] = total


def _finalize_download(download_id: str, status: str, error: str | None = None):
    with DOWNLOADS_LOCK:
        state = DOWNLOADS_PROGRESS.get(download_id)
        if state is not None:
            state["status"] = status
            state["error"] = error
            # Remove from active map if present
            file_path = state.get("file_path")
            if file_path in ACTIVE_DOWNLOADS:
                ACTIVE_DOWNLOADS.pop(file_path, None)


def _background_download(download_id: str, model_url: str, checksum: str | None, target_dir: str, file_name: str):
    file_path = os.path.join(target_dir, file_name)
    _init_download_state(download_id, file_path)

    if not any(file_name.lower().endswith(ext) for ext in ALLOWED_EXTENSIONS):
        _finalize_download(download_id, "failed", f"Invalid file extension. Allowed: {', '.join(sorted(ALLOWED_EXTENSIONS))}")
        return

    try:
        os.makedirs(target_dir, exist_ok=True)

        with requests.get(model_url, stream=True, timeout=60) as r:
            r.raise_for_status()
            total = int(r.headers.get('content-length', 0))
            downloaded = 0
            _update_download_progress(download_id, downloaded, total)

            with open(file_path, 'wb') as f:
                for chunk in r.iter_content(chunk_size=1024 * 1024):  # 1MB chunks
                    if not chunk:
                        continue
                    f.write(chunk)
                    downloaded += len(chunk)
                    _update_download_progress(download_id, downloaded, total)


        # Verify file type (content check)
        with open(file_path, "rb") as f:
            header = f.read(1024)
        
        if b"<!DOCTYPE html" in header or b"<html" in header.lower():
             try:
                os.remove(file_path)
             except OSError:
                pass
             _finalize_download(download_id, "failed", "File appears to be HTML (possible download error)")
             return

        # Verify checksum if provided
        if checksum is not None and checksum != "":
            with open(file_path, "rb") as f:
                file_hash = hashlib.sha256(f.read()).hexdigest()
            if file_hash != checksum:
                try:
                    os.remove(file_path)
                finally:
                    _finalize_download(download_id, "failed", "Checksum verification failed")
                    return
        else:
            # Log hash for reference
            try:
                with open(file_path, "rb") as f:
                    file_hash = hashlib.sha256(f.read()).hexdigest()
                print("Downloaded file checksum (no verification):", file_hash)
            except Exception:
                pass

        _finalize_download(download_id, "completed")
    except Exception as e:
        # Clean up partial file
        try:
            if os.path.exists(file_path):
                os.remove(file_path)
        except Exception:
            pass
        _finalize_download(download_id, "failed", str(e))


def start_model_download(model_url: str, checksum: str | None, target_dir: str, file_name: str) -> dict:
    """Attempts to start a model download.
    Returns a dict with:
      - status: 'started' | 'in_progress_existing' | 'exists'
      - download_id: present when status is 'started' or 'in_progress_existing'
      - message: optional info message
    """
    abs_target_dir = os.path.abspath(target_dir)
    file_path = os.path.join(abs_target_dir, file_name)

    with DOWNLOADS_LOCK:
        # If file already exists on disk, do not start
        if os.path.exists(file_path):
            print(f"File already exists at path: {file_path}")
            return {"status": "exists", "message": "File already exists", "file_path": file_path}

        # If a download for the same target path is active, return existing id
        existing_id = ACTIVE_DOWNLOADS.get(file_path)
        if existing_id:
            return {"status": "in_progress_existing", "download_id": existing_id}

        # Otherwise, create entry and start background thread
        download_id = str(uuid4())
        ACTIVE_DOWNLOADS[file_path] = download_id

    t = Thread(target=_background_download, args=(download_id, model_url, checksum, abs_target_dir, file_name), daemon=True)
    t.start()
    return {"status": "started", "download_id": download_id}


def get_download_progress(download_id: str) -> dict:
    with DOWNLOADS_LOCK:
        state = DOWNLOADS_PROGRESS.get(download_id)

    if state is None:
        raise KeyError("Download not found")

    downloaded = state.get("downloaded_bytes", 0)
    total = state.get("total_bytes", 0)
    progress = (float(downloaded) / float(total)) if total > 0 else 0.0

    return {
        "status": state.get("status"),
        "progress": progress,
        "downloaded_bytes": downloaded,
        "total_bytes": total,
        "error": state.get("error"),
        "file_path": state.get("file_path"),
    }


