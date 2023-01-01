
from fastapi import FastAPI 
import fastapi_cls
import global_val as glb
import yaml
import uvicorn

fast = FastAPI()
fast.include_router(fastapi_cls.router)

if __name__ == "__main__":
    with open('conf/conf.yml' ,'r', encoding="utf-8") as file:
        glb.conf_yml = yaml.safe_load(file)
    uvicorn.run("analyze_splatoon:fast", host="127.0.0.1", port=8080, log_level="info")
