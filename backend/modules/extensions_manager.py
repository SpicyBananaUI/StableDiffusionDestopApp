import os
import glob

from modules import shared, scripts


def reload_extensions_and_scripts():
    from modules import extensions as mod_extensions
    mod_extensions.list_extensions()
    scripts.load_scripts()


def compute_disabled_from_enabled(enabled: list[str]) -> list[str]:
    from modules import extensions as mod_extensions
    mod_extensions.list_extensions()
    names = [e.name for e in mod_extensions.extensions]
    return [n for n in names if n not in set(enabled)]


def is_backend_safe_extension(ext_path: str) -> bool:
    try:
        if os.path.isdir(os.path.join(ext_path, 'javascript')):
            return False
        scripts_dir = os.path.join(ext_path, 'scripts')
        for script_file in glob.glob(os.path.join(scripts_dir, '*.py')):
            try:
                with open(script_file, 'r', encoding='utf-8', errors='ignore') as f:
                    content = f.read()
                    if 'import gradio' in content or 'from gradio' in content:
                        return False
            except Exception:
                return False
    except Exception:
        return False
    return True


def enable_extensions_via_options(enabled: list[str], disable_all: str | None = None) -> dict:
    disabled_list = compute_disabled_from_enabled(enabled)

    shared.opts.disabled_extensions = disabled_list
    if disable_all is not None:
        shared.opts.disable_all_extensions = disable_all
    shared.opts.save(shared.config_filename)

    reload_extensions_and_scripts()

    return {"enabled": enabled, "disabled": disabled_list, "disable_all": shared.opts.disable_all_extensions}




