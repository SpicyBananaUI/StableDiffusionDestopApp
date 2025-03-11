"""Based on https://github.com/AUTOMATIC1111/stable-diffusion-webui/blob/master/modules/api/models.py
Meant to enable seperate server and client in the future.
Right now, focus on only minimum API calls for pre-alpha build"""

from pydantic import BaseModel, Field, create_model, ConfigDict
from backend.modules.processing_scripts import StableDiffusionProcessingTxt2Img # TODO: implement


class TextToImageResponse(BaseModel):
    images: list[str] | None = Field(default=None, title="Image", description="The generated image in base64 format.")
    parameters: dict
    info: str

fields = {}
# TODO: populate fields based on predetermined options (from import)
OptionsModel = create_model("Options", **fields)


StableDiffusionTxt2ImgProcessingAPI = PydanticModelGenerator(
    "StableDiffusionProcessingTxt2Img",
    StableDiffusionProcessingTxt2Img,
    [
        {"key": "sampler_index", "type": str, "default": "Euler"},
        {"key": "script_name", "type": str | None, "default": None},
        {"key": "script_args", "type": list, "default": []},
        {"key": "send_images", "type": bool, "default": True},
        {"key": "save_images", "type": bool, "default": False},
        {"key": "alwayson_scripts", "type": dict, "default": {}},
        {"key": "force_task_id", "type": str | None, "default": None},
        {"key": "infotext", "type": str | None, "default": None},
    ]
).generate_model()

class TextToImageResponse(BaseModel):
    # TODO: replace with FileResponse ideally
    images: list[str] | None = Field(default=None, title="Image", description="The generated image in base64 format.")
    parameters: dict
    info: str

class PydanticModelGenerator:
    """
    Likely will keep this largely as-is. Everything below is original from Automatic1111
    Takes in created classes and stubs them out in a way FastAPI/Pydantic is happy about:
    source_data is a snapshot of the default values produced by the class
    params are the names of the actual keys required by __init__
    """

    def __init__(
        self,
        model_name: str = None,
        class_instance = None,
        additional_fields = None,
    ):
        def field_type_generator(k, v):
            field_type = v.annotation

            if field_type == 'Image':
                # images are sent as base64 strings via API
                field_type = 'str'

            return Optional[field_type]

        def merge_class_params(class_):
            all_classes = list(filter(lambda x: x is not object, inspect.getmro(class_)))
            parameters = {}
            for classes in all_classes:
                parameters = {**parameters, **inspect.signature(classes.__init__).parameters}
            return parameters

        self._model_name = model_name
        self._class_data = merge_class_params(class_instance)

        self._model_def = [
            ModelDef(
                field=underscore(k),
                field_alias=k,
                field_type=field_type_generator(k, v),
                field_value=None if isinstance(v.default, property) else v.default
            )
            for (k,v) in self._class_data.items() if k not in API_NOT_ALLOWED
        ]

        for fields in additional_fields:
            self._model_def.append(ModelDef(
                field=underscore(fields["key"]),
                field_alias=fields["key"],
                field_type=fields["type"],
                field_value=fields["default"],
                field_exclude=fields["exclude"] if "exclude" in fields else False))

    def generate_model(self):
        """
        Creates a pydantic BaseModel
        from the json and overrides provided at initialization
        """
        fields = {
            d.field: (d.field_type, Field(default=d.field_value, alias=d.field_alias, exclude=d.field_exclude)) for d in self._model_def
        }
        DynamicModel = create_model(self._model_name, __config__=ConfigDict(populate_by_name=True, frozen=False), **fields)
        return DynamicModel