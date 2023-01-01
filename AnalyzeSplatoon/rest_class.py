
from pydantic import BaseModel
class CaptureModel(BaseModel):
    device_id: int
    save_name: str
    save_fps: int
    width: int
    height: int