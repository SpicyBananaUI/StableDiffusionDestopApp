"""
Initialization module for Translation Layer

This module can be used by the existing backend API to activate the
Gradio interceptor and register API endpoints associated with the
translation layer.

"""

import os
import sys

# Add parent directory to path if needed for relative imports
# (api_endpoints.py uses 'from .gradio_interceptor import ...')
_translation_layer_dir = os.path.dirname(os.path.abspath(__file__))
_parent_dir = os.path.dirname(_translation_layer_dir)

if _parent_dir not in sys.path:
    sys.path.insert(0, _parent_dir)

from translation_layer.gradio_interceptor import GradioInterceptor
from translation_layer.api_endpoints import setup_translation_api


def activate_translation_layer(app):
    """
    Activate the translation layer and point it at the FastAPI app
    """
    interceptor = GradioInterceptor.get_instance()
    interceptor.activate()

    setup_translation_api(app)

    print("Translation Layer Activated - Gradio components will be intercepted and serialized")


def deactivate_translation_layer():
    """Deactivate the translation layer."""
    interceptor = GradioInterceptor.get_instance()
    interceptor.deactivate()
    print("Translation Layer Deactivated")
