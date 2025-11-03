"""
Gradio Component Interceptor

Intercepts Gradio component creation and builds a serializable component tree.
"""

import json
import uuid
import inspect
import os
from typing import Any, Dict, List, Optional, Callable, Set
from collections import defaultdict
import gradio as gr


# Supported component types (frontend has renderers)
SUPPORTED_COMPONENT_TYPES: Set[str] = {
    'blocks', 'row', 'column', 'group', 'accordion',
    'button', 'textbox', 'slider', 'checkbox', 'dropdown', 'number'
}


class ComponentNode:
    """Represents a serializable Gradio component node"""
    
    def __init__(self, component: Any, node_id: str, extension_name: Optional[str] = None):
        self.id = node_id
        self.type = self._get_component_type(component)
        self.component = component
        self.parent_id: Optional[str] = None
        self.children: List[str] = []
        self.props: Dict[str, Any] = {}
        self.events: Dict[str, Any] = {}
        self.extension_name = extension_name
        
    def _get_component_type(self, component: Any) -> str:
        """Extract component type name from class name"""
        class_name = component.__class__.__name__
        
        return class_name.lower()
    
    def extract_props(self):
        """Extract serializable properties from the component"""
        props = {}
        
        # Shared properties
        if hasattr(self.component, 'label'):
            props['label'] = getattr(self.component, 'label', None)
        if hasattr(self.component, 'value'):
            props['value'] = self._serialize_value(getattr(self.component, 'value', None))
        if hasattr(self.component, 'visible'):
            props['visible'] = getattr(self.component, 'visible', True)
        if hasattr(self.component, 'interactive'):
            props['interactive'] = getattr(self.component, 'interactive', True)
        if hasattr(self.component, 'elem_id'):
            props['elem_id'] = getattr(self.component, 'elem_id', None)
        if hasattr(self.component, 'elem_classes'):
            props['elem_classes'] = getattr(self.component, 'elem_classes', None)
        
        # Type-specific properties
        if self.type == 'slider':
            if hasattr(self.component, 'minimum'):
                props['minimum'] = getattr(self.component, 'minimum', 0)
            if hasattr(self.component, 'maximum'):
                props['maximum'] = getattr(self.component, 'maximum', 100)
            if hasattr(self.component, 'step'):
                props['step'] = getattr(self.component, 'step', 1)
        
        if self.type == 'textbox':
            if hasattr(self.component, 'placeholder'):
                props['placeholder'] = getattr(self.component, 'placeholder', None)
            if hasattr(self.component, 'lines'):
                props['lines'] = getattr(self.component, 'lines', 1)
        
        if self.type == 'dropdown':
            if hasattr(self.component, 'choices'):
                props['choices'] = getattr(self.component, 'choices', [])
            if hasattr(self.component, 'multiselect'):
                props['multiselect'] = getattr(self.component, 'multiselect', False)
        
        if self.type == 'accordion':
            if hasattr(self.component, 'open'):
                props['open'] = getattr(self.component, 'open', False)
        
        if self.type == 'button':
            if hasattr(self.component, 'variant'):
                props['variant'] = getattr(self.component, 'variant', 'secondary')
        
        self.props = props
        return props
    
    def _serialize_value(self, value: Any) -> Any:
        """Serialize a value to JSON-serializable format"""
        if value is None:
            return None
        if isinstance(value, (str, int, float, bool)):
            return value
        if isinstance(value, (list, tuple)):
            return [self._serialize_value(v) for v in value]
        if isinstance(value, dict):
            return {k: self._serialize_value(v) for k, v in value.items()}
        # else stringify
        return str(value)
    
    def to_dict(self) -> Dict[str, Any]:
        """Convert node to dictionary"""
        return {
            'id': self.id,
            'type': self.type,
            'props': self.props,
            'events': self.events,
            'parent_id': self.parent_id,
            'children': self.children,
        }


class GradioInterceptor:
    """Intercepts Gradio component creation and builds a component tree"""
    
    def __init__(self):
        self.components: Dict[str, ComponentNode] = {}
        self.component_map: Dict[Any, str] = {}  # Maps Gradio component -> node_id
        self.context_stack: List[str] = []  # Tracks nested contexts (Row, Column, etc.)
        self.root_nodes: List[str] = []  # Top-level component IDs
        self.original_init_methods = {}
        self.active = False
        self.encountered_types: Set[str] = set()  # Track all component types we've seen
        self.extension_components: Dict[str, List[str]] = {}  # Maps extension name -> list of component IDs
        
    def activate(self):
        """Activate the interceptor by patching Gradio"""
        if self.active:
            return
            
        self.active = True
        self._patch_block_context()
        self._patch_io_components()
        
    def deactivate(self):
        """Deactivate the interceptor"""
        if not self.active:
            return
            
        self.active = False
        
    def _patch_block_context(self):
        """Patch BlockContext to intercept container components"""
        # Avoid re-patching
        if hasattr(gr.blocks.BlockContext.__init__, '_interceptor_patched'):
            return
            
        original_init = gr.blocks.BlockContext.__init__
        
        def intercepted_init(self, *args, **kwargs):
            result = original_init(self, *args, **kwargs)
            
            if hasattr(self, '__class__'):
                class_name = self.__class__.__name__
                # Container components
                if class_name in ['Blocks', 'Row', 'Column', 'Group', 'Tabs', 'TabItem', 'Accordion']:
                    interceptor = GradioInterceptor.get_instance()
                    if interceptor.active:
                        interceptor._register_context(self)
                    
            return result
        
        intercepted_init._interceptor_patched = True
        gr.blocks.BlockContext.__init__ = intercepted_init
        
    def _patch_io_components(self):
        """Patch IOComponent to intercept input/output components"""
        # Avoid re-patching
        if hasattr(gr.components.Component.__init__, '_interceptor_patched'):
            return
            
        original_init = gr.components.Component.__init__
        
        def intercepted_init(self, *args, **kwargs):
            result = original_init(self, *args, **kwargs)
            
            interceptor = GradioInterceptor.get_instance()
            if interceptor.active:
                interceptor._register_component(self)
            
            return result
        
        intercepted_init._interceptor_patched = True
        gr.components.Component.__init__ = intercepted_init
        

    def _get_current_extension(self) -> Optional[str]:
        """Try to infer the current extension name by inspecting the call stack."""
        try:
            for frame_info in inspect.stack():
                filename = frame_info.filename.replace("\\", "/")
                parts = filename.split("/")

                # Look for the 'extensions' or 'extensions-builtin' folder
                for i, part in enumerate(parts):
                    if part in ["extensions", "extensions-builtin"] and i + 1 < len(parts):
                        ext_name = parts[i + 1]
                        print(f"[Interceptor] Detected extension '{ext_name}' from file: {filename}")
                        return ext_name
        except Exception as e:
            print(f"[Interceptor] Failed to detect extension: {e}")
        
        return None

    def _register_context(self, component: Any):
        """Register a context component (Row, Column, etc.)"""
        # Check if already registered
        if component in self.component_map:
            return
        
        # Check if this component has already been processed by checking for a marker
        if hasattr(component, '_translation_layer_registered'):
            return
        
        node_id = str(uuid.uuid4())
        extension_name = self._get_current_extension()
        node = ComponentNode(component, node_id, extension_name)
        node.extract_props()

        if extension_name is None:
            return

        # Mark registered
        component._translation_layer_registered = True
        
        # Track encountered component type
        self.encountered_types.add(node.type)
        
        # Track extension usage
        if extension_name:
            if extension_name not in self.extension_components:
                self.extension_components[extension_name] = []
            self.extension_components[extension_name].append(node_id)
        
        self.components[node_id] = node
        self.component_map[component] = node_id
        
        # Set parent if we're in a context
        if self.context_stack:
            parent_id = self.context_stack[-1]
            node.parent_id = parent_id
            # Only add to children if not already
            if node_id not in self.components[parent_id].children:
                self.components[parent_id].children.append(node_id)
        else:
            # Only add to root if not already
            if node_id not in self.root_nodes:
                self.root_nodes.append(node_id)
        
        # Push onto context stack
        if node_id not in self.context_stack:
            self.context_stack.append(node_id)
        
        # Use context manager tracking if available
        if hasattr(component, "__enter__") and hasattr(component, "__exit__"):
            # Only wrap if not already
            if not hasattr(component.__enter__, '_translation_layer_wrapped'):
                original_enter = component.__enter__
                original_exit = component.__exit__

                def enter_wrapper(*args, **kwargs):
                    result = original_enter(*args, **kwargs)
                    return result

                def exit_wrapper(exc_type=None, exc_val=None, exc_tb=None):
                    if self.context_stack and self.context_stack[-1] == node_id:
                        self.context_stack.pop()
                    return original_exit(exc_type, exc_val, exc_tb)

                enter_wrapper._translation_layer_wrapped = True
                exit_wrapper._translation_layer_wrapped = True
                component.__enter__ = enter_wrapper
                component.__exit__ = exit_wrapper

        
    def _register_component(self, component: Any):
        """Register an IO component"""
        # Check if already registered
        if component in self.component_map:
            return
        
        # Check if this component has already been processed by checking for a marker
        if hasattr(component, '_translation_layer_registered'):
            return
            
        node_id = str(uuid.uuid4())
        extension_name = self._get_current_extension()
        node = ComponentNode(component, node_id, extension_name)
        node.extract_props()
        
        if extension_name is None:
            return

        # Mark as registered
        component._translation_layer_registered = True
        
        # Track encountered component type
        self.encountered_types.add(node.type)
        
        # Track extension usage
        if extension_name:
            if extension_name not in self.extension_components:
                self.extension_components[extension_name] = []
            self.extension_components[extension_name].append(node_id)
        
        self.components[node_id] = node
        self.component_map[component] = node_id
        
        # Set parent if in a context
        if self.context_stack:
            parent_id = self.context_stack[-1]
            node.parent_id = parent_id
            # Only add to children if not already
            if node_id not in self.components[parent_id].children:
                self.components[parent_id].children.append(node_id)
        else:
            # Only add to root if not already
            if node_id not in self.root_nodes:
                self.root_nodes.append(node_id)
    
    def clear(self):
        """Clear all registered components"""
        self.components.clear()
        self.component_map.clear()
        self.context_stack.clear()
        self.root_nodes.clear()
        self.encountered_types.clear()
        self.extension_components.clear()
    
    def get_extension_compatibility(self, extension_name: str) -> Dict[str, Any]:
        """Get compatibility information for a specific extension"""
        component_ids = self.extension_components.get(extension_name, [])
        
        if not component_ids:
            return {
                'extension_name': extension_name,
                'supported': None,
                'component_types': [],
                'unsupported_types': [],
                'component_count': 0
            }
        
        component_types = set()
        unsupported_types = set()
        
        for comp_id in component_ids:
            if comp_id in self.components:
                node = self.components[comp_id]
                component_types.add(node.type)
                if node.type not in SUPPORTED_COMPONENT_TYPES:
                    unsupported_types.add(node.type)
        
        return {
            'extension_name': extension_name,
            'supported': len(unsupported_types) == 0 and len(component_types) > 0,
            'component_types': sorted(list(component_types)),
            'unsupported_types': sorted(list(unsupported_types)),
            'component_count': len(component_ids)
        }
    
    def get_all_extensions_compatibility(self) -> Dict[str, Dict[str, Any]]:
        """Get compatibility information for all extensions that have created components"""
        result = {}
        for extension_name in self.extension_components.keys():
            result[extension_name] = self.get_extension_compatibility(extension_name)
        return result
    
    def get_supported_types(self) -> Set[str]:
        """Get the set of supported component types"""
        return SUPPORTED_COMPONENT_TYPES.copy()
    
    def get_encountered_types(self) -> Set[str]:
        """Get all component types that have been encountered"""
        return self.encountered_types.copy()
    
    def get_unsupported_types(self) -> Set[str]:
        """Get component types that were encountered but are not supported"""
        return self.encountered_types - SUPPORTED_COMPONENT_TYPES
    
    def get_component_tree(self) -> Dict[str, Any]:
        """Get the full component tree as a dictionary"""
        components_dict = {}
        for node_id, node in self.components.items():
            component_dict = node.to_dict()
            component_dict['supported'] = node.type in SUPPORTED_COMPONENT_TYPES
            components_dict[node_id] = component_dict
        
        return {
            'root_nodes': self.root_nodes,
            'components': components_dict,
            'supported_types': sorted(SUPPORTED_COMPONENT_TYPES),
            'unsupported_encountered': sorted(self.get_unsupported_types())
        }
    
    def get_component_tree_json(self) -> str:
        """Get the component tree as JSON string"""
        return json.dumps(self.get_component_tree(), indent=2)
    
    def get_component_by_id(self, node_id: str) -> Optional[ComponentNode]:
        """Get a component node by ID"""
        return self.components.get(node_id)
    
    def get_component_value(self, node_id: str) -> Any:
        """Get the current value of a component"""
        node = self.components.get(node_id)
        if node and hasattr(node.component, 'value'):
            return node._serialize_value(node.component.value)
        return None
    
    def set_component_value(self, node_id: str, value: Any) -> bool:
        """Set the value of a component"""
        node = self.components.get(node_id)
        if node and hasattr(node.component, 'value'):
            try:
                node.component.value = value
                node.extract_props()
                return True
            except Exception:
                return False
        return False
    
    def trigger_event(self, node_id: str, event_name: str, data: Any = None) -> Dict[str, Any]:
        """Trigger an event on a component (placeholder for future implementation)"""
        node = self.components.get(node_id)
        if not node:
            return {'success': False, 'error': 'Component not found'}

        # This is a placeholder - full event handling would require
        # tracking Gradio event handlers and executing them
        # TODO: Implement actual event handler lookup and execution
        return {
            'success': True,
            'node_id': node_id,
            'event': event_name,
            'message': 'Event triggered (placeholder)'
        }
    

    # Singleton
    _instance = None
    
    @classmethod
    def get_instance(cls):
        """Get the singleton instance"""
        if cls._instance is None:
            cls._instance = cls()
        return cls._instance

