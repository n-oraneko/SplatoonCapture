
from fastapi import APIRouter
import global_val as glb
from rest_class import *
import capture

router = APIRouter()
glb.capture = None       

@router.post("/capture")
async def cap_start(capture_model: CaptureModel):
    if not glb.capture is None:
        del glb.capture
        glb.capture = None
        return
    glb.capture = capture.CaptureClass(capture_model.save_name)
    glb.capture.resize(capture_model.width,capture_model.height)
    glb.capture.show_start(capture_model.device_id,capture_model.save_fps)
    print('capture start')
    return 

@router.post("/capture/resize")
async def cap_resize(capture_model: CaptureModel):
    if glb.capture is None:
        return
    glb.capture.resize(capture_model.width,capture_model.height)
    print('resized')
    return 

@router.get("/capture/all")
async def cap_all():
    if glb.capture is None:
        return {'kill':[],'death':[],'salmon_death':[]}
    #print(glb.capture.capture_trim)
    return glb.capture.capture_trim

@router.post("/capture/trim")
async def trim_start(capture_model: CaptureModel):
    if glb.capture is None:
        return
    glb.capture.trim_start(capture_model.save_name)
    return 