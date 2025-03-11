"""Based on https://github.com/AUTOMATIC1111/stable-diffusion-webui/blob/master/modules/api/api.py
Meant to enable seperate server and client in the future.
Right now, focus on only minimum API calls for pre-alpha build"""

from threading import Lock

from fastapi import APIRouter, Depends, FastAPI, Request, Response
from fastapi.middleware.cors import CORSMiddleware
from fastapi.security import HTTPBasic, HTTPBasicCredentials
from fastapi.exceptions import HTTPException
from fastapi.responses import JSONResponse
from fastapi.encoders import jsonable_encoder

from backend.api.response_models import models


class Api:
    def __init__(self, app: FastAPI, queue_lock: Lock):
        # TODO: unpack athentication info if given

        self.router = APIRouter()
        self.app = app
        self.queue_lock = queue_lock

        self.add_api_route("/sdapi/v1/txt2img", self.text2imgapi, methods=["POST"], response_model=models.TextToImageResponse)

        self.add_api_route("/sdapi/v1/options", self.get_config, methods=["GET"], response_model=models.OptionsModel)
        self.add_api_route("/sdapi/v1/options", self.set_config, methods=["POST"])

        # TODO: server control calls

        self.default_script_arg_txt2img = []


        def add_api_route(self, path:str, endpoint, **kwargs):
            # TODO: handle authentication dependencies if applicable
            return self.app.add_api_route(path, endpoint, **kwargs)
        
        def text2imgapi(self, txt2imgreq: models.StableDiffusionTxt2ImgProcessingAPI):
            # TODO: implement
            # TODO: explore returning images as FileResponse instaead of b64images for performance
            return models.TextToImageResponse()
