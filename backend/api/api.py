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

import sys
sys.path.append("/Users/ronan/Library/Application Support/StabilityMatrix/Packages/stable-diffusion-webui-forge/")  # REPLACE WITH YOUR PATH TO THE SD WEB UI PROJECT
from modules.api import models
from modules.processing import StableDiffusionProcessingTxt2Img, StableDiffusionProcessingImg2Img, process_images, process_extra_images

TMP_SCRIPT_RUNNER_PLACEHOLDER = None
TMP_OUTPATH_GRIDS = "../tmp/grids"
TMP_OUTPATH_SAMPLES = "../tmp/samples"
TMP_OUTPATH_IMAGES = "../../shared/tmp_images"


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
        # TODO: some sort of get_progress call would probably also be helpful

        self.default_script_arg_txt2img = []

    # __init__ end


    def add_api_route(self, path:str, endpoint, **kwargs):
        # TODO: handle authentication dependencies if applicable
        return self.app.add_api_route(path, endpoint, **kwargs)
    
    def text2imgapi(self, txt2imgreq: models.StableDiffusionTxt2ImgProcessingAPI):
        # TODO: implement some sort of task management system
        # task_id = txt2imgreq.force_task_id or create_task_id("txt2img")
        script_runner = TMP_SCRIPT_RUNNER_PLACEHOLDER # scripts.scripts_txt2img

        # Parse args (WIP)

        infotext_script_args = {}
        self.apply_infotext(txt2imgreq, "txt2img", script_runner=script_runner, mentioned_script_args=infotext_script_args)

        selectable_scripts, selectable_script_idx = self.get_selectable_script(txt2imgreq.script_name, script_runner)
        # sampler, scheduler = sd_samplers.get_sampler_and_scheduler(txt2imgreq.sampler_name or txt2imgreq.sampler_index, txt2imgreq.scheduler)

        populate = txt2imgreq.copy(update={  # Override __init__ params
            # "sampler_name": validate_sampler_name(sampler), # TODO: allow sampler selection
            "do_not_save_samples": not txt2imgreq.save_images,
            "do_not_save_grid": not txt2imgreq.save_images,
        })
        if populate.sampler_name:
            populate.sampler_index = None  # prevent a warning later on

        # TODO: populate scheduler once implemented

        args = vars(populate)
        args.pop('script_name', None)
        args.pop('script_args', None) # will refeed them to the pipeline directly after initializing them
        args.pop('alwayson_scripts', None)
        args.pop('infotext', None)

        script_args = self.init_script_args(txt2imgreq, self.default_script_arg_txt2img, selectable_scripts, selectable_script_idx, script_runner, input_script_args=infotext_script_args)

        send_images = args.pop('send_images', True)
        args.pop('save_images', None)

        # generate image
        p = StableDiffusionProcessingTxt2Img()
        p.is_api = True
        p.scrips = script_runner

        # TODO: support task scheduling, support selectable scripts, 
        try:
            processed = process_images(p)
        except Exception as e:
            print(f"error occured when generating image{e}")


        # TODO: return image
        # for now, just save as tmp
        i = 0
        for image in processed.images:
            image.save(TMP_OUTPATH_IMAGES + "/tmp" + str(i) + ".png")
            i+=1
        # TODO: explore returning images as FileResponse instaead of b64images for performance
        return models.TextToImageResponse(image)
