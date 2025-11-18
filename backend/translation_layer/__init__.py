"""
Gradio to Avalonia Translation Layer

This module provides a proof-of-concept translation layer that takes Gradio components,
intercepts them, and serializes them with JSON for use by the Avalonia frontend via the API.
"""

from .gradio_interceptor import GradioInterceptor
from .api_endpoints import setup_translation_api

_interceptor = GradioInterceptor.get_instance()
if not _interceptor.active:
    _interceptor.activate()
    print("translation_layer: GradioInterceptor auto-activated")

__all__ = ['GradioInterceptor', 'setup_translation_api']

