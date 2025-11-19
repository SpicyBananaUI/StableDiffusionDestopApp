"""
API Endpoints for Translation Layer

Provides FastAPI endpoints for:
- Fetching gradio component tree as JSON
- Getting/setting component values
- Triggering component events
"""

from fastapi import APIRouter, HTTPException, Body
from typing import Any, Dict, Optional
import modules.shared as shared
from .gradio_interceptor import GradioInterceptor


router = APIRouter(prefix="/translation-layer", tags=["translation-layer"])


def setup_translation_api(app):
    """Setup translation layer API endpoints on the FastAPI app"""
    app.include_router(router)


@router.get("/component-tree")
async def get_component_tree():
    """Get the full component tree as JSON, grouped by extension"""
    interceptor = GradioInterceptor.get_instance()
    
    if not interceptor.active:
        print("Translation layer not active. Component tree may be empty.")
        return {
            'active': False,
            'message': 'Translation layer not active. Component tree may be empty.',
            'tree': interceptor.get_component_tree()
        }
    print("Translation layer active. Getting component tree...")
    # print(interceptor.get_component_tree())
    return {
        'active': True,
        'tree': interceptor.get_component_tree()
    }


@router.get("/extension-values")
async def get_extension_values():
    """Get all extension component values in alwayson_scripts format"""
    interceptor = GradioInterceptor.get_instance()
    
    if not interceptor.active:
        return {
            'active': False,
            'message': 'Translation layer not active.',
            'values': {}
        }
    
    return {
        'active': True,
        'values': interceptor.get_all_extension_values()
    }


@router.get("/component/{node_id}")
async def get_component(node_id: str):
    """Get component by ID"""
    interceptor = GradioInterceptor.get_instance()
    node = interceptor.get_component_by_id(node_id)
    
    if not node:
        raise HTTPException(status_code=404, detail=f"Component {node_id} not found")
    
    return {
        'node': node.to_dict(),
        'value': interceptor.get_component_value(node_id)
    }


@router.get("/component/{node_id}/value")
async def get_component_value(node_id: str):
    """Get current value of component"""
    interceptor = GradioInterceptor.get_instance()
    value = interceptor.get_component_value(node_id)
    
    if value is None:
        raise HTTPException(status_code=404, detail=f"Component {node_id} not found or has no value")
    
    return {'node_id': node_id, 'value': value}


@router.post("/component/{node_id}/value")
async def set_component_value(
    node_id: str,
    value: Any = Body(..., embed=True)
):
    """Set the value of component"""
    interceptor = GradioInterceptor.get_instance()
    
    success = interceptor.set_component_value(node_id, value)
    
    if not success:
        raise HTTPException(
            status_code=400,
            detail=f"Failed to set value for component {node_id}"
        )
    
    return {
        'success': True,
        'node_id': node_id,
        'value': interceptor.get_component_value(node_id)
    }


@router.post("/component/{node_id}/event/{event_name}")
async def trigger_component_event(
    node_id: str,
    event_name: str,
    data: Optional[Dict[str, Any]] = Body(None, embed=True)
):
    """Trigger an event on a component"""
    interceptor = GradioInterceptor.get_instance()
    
    result = interceptor.trigger_event(node_id, event_name, data)
    
    if not result.get('success'):
        raise HTTPException(
            status_code=400,
            detail=result.get('error', 'Failed to trigger event')
        )
    
    return result


@router.post("/clear")
async def clear_component_tree():
    """Clear the component tree (useful for testing)"""
    interceptor = GradioInterceptor.get_instance()
    interceptor.clear()
    
    return {'success': True, 'message': 'Component tree cleared'}


@router.get("/status")
async def get_status():
    """Get status of the translation layer"""
    interceptor = GradioInterceptor.get_instance()
    
    return {
        'active': interceptor.active,
        'component_count': len(interceptor.components),
        'root_nodes': len(interceptor.root_nodes)
    }


@router.get("/supported-types")
async def get_supported_types():
    """Get list of supported component types"""
    interceptor = GradioInterceptor.get_instance()
    
    return {
        'supported_types': sorted(interceptor.get_supported_types()),
        'encountered_types': sorted(interceptor.get_encountered_types()),
        'unsupported_types': sorted(interceptor.get_unsupported_types())
    }


@router.get("/extensions")
async def get_extensions_with_compatibility():
    """
    Get extensions list with translation layer compatibility information.
    Similar to /sdapi/v1/extensions but includes compatibility status.
    """
    try:
        from modules import extensions
        extensions.list_extensions()
        
        interceptor = GradioInterceptor.get_instance()
        compatibility_map = interceptor.get_all_extensions_compatibility()
        
        ext_list = []
        for ext in extensions.extensions:
            ext.read_info_from_repo()
            
            # Get compat info
            compat = compatibility_map.get(ext.name, {})
            if not compat:
                # Extension hasn't created components yet, check if it is compatible
                compat = interceptor.get_extension_compatibility(ext.name)
            
            ext_dict = {
                "name": str(ext.name or ""),
                "remote": str(ext.remote or ""),
                "branch": str(ext.branch or ""),
                "commit_hash": str(ext.commit_hash or ""),
                "commit_date": int(ext.commit_date or 0),
                "version": str(ext.version or ""),
                "enabled": bool(ext.enabled),
                # Translation layer compat info
                "translation_layer": {
                    "supported": compat.get('supported'),
                    "component_types": compat.get('component_types', []),
                    "unsupported_types": compat.get('unsupported_types', []),
                    "component_count": compat.get('component_count', 0)
                }
            }
            ext_list.append(ext_dict)
        
        return ext_list
    except Exception as e:
        raise HTTPException(status_code=500, detail=f"Failed to get extensions: {str(e)}")


@router.get("/extensions/{extension_name}/compatibility")
async def get_extension_compatibility(extension_name: str):
    """Get compatibility information for a specific extension"""
    interceptor = GradioInterceptor.get_instance()
    compat = interceptor.get_extension_compatibility(extension_name)
    
    if compat['component_count'] == 0:
        raise HTTPException(
            status_code=404,
            detail=f"Extension '{extension_name}' not found or has no tracked components"
        )
    
    return compat

