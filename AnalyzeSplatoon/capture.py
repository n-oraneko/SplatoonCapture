
import global_val as glb
import cv2
from multiprocessing import Process,Value,Queue,Manager
import time
import numpy as np

class CaptureClass:

    isOpen = False
    def __init__(self,name) :
        glb.processes=[]
        glb.trim_processes = []
        ##画質劣化させる　速度重視
        #画素は800×800になる。。このキャプチャボードだけかも。
        self.orign_width = 1920
        self.orign_height =1080
        self.default_width = int(self.orign_width//2)
        self.default_height = int(self.orign_height//2)

        self.cap_width = Value('i',self.orign_width)
        self.cap_height = Value('i',self.orign_height)
        self.save_name = name
        self.conf = glb.conf_yml
        self.end_flag = Value('i', 0)
        glb.manager = Manager()
        
        self.capture_trim= glb.manager.dict()
        self.capture_trim['kill'] =[] #= {'kill':[],'death':[],'salmon_death':[]}
        self.capture_trim['death'] =[]
        self.capture_trim['salmon_death'] =[]

    def __del__(self):
        for i in range(3):
            self.end_flag = Value('i', 1)
        time.sleep(1)
        for i in range(len(glb.processes)):
            glb.processes[i].terminate()
            glb.processes[i].join()


    def connection_display(self,capture_num):
        self.capture = cv2.VideoCapture(capture_num)
        if not self.capture.isOpened():
            self.isOpen = False
        else:
            self.isOpen = True
            self.capture.set(3, self.default_width)
            self.capture.set(4, self.default_height)

    def resize(self,width,height):
        print('view size',width,':',height)
        tmp_width = width
        tmp_height = (int)((width / self.default_width)*self.default_height)
        if tmp_height >  height:
            tmp_height = height
            tmp_width = (int)((height / self.default_height)*self.default_width)
        for i in range(3): 
            self.cap_width = Value('i', tmp_width)
            self.cap_height = Value('i', tmp_height)

    def show_start(self,capture_num,fps):
        queue = Queue()
        self.fps = fps
        glb.processes.append(Process(target=self.show_frame,args=(capture_num,queue)))
        glb.processes.append(Process(target=self.analyze_frame,args=(queue,self.capture_trim)))
        for i in range(len(glb.processes)):
            glb.processes[i].start()

            
    def show_frame(self,capture_num,queue):
        self.connection_display(capture_num)
        fps_time = 1/self.fps
        fourcc = cv2.VideoWriter_fourcc('m', 'p', '4', 'v')
        cap_out = cv2.VideoWriter(self.conf["cap_save_dir"]+ self.save_name, fourcc, self.fps, (self.default_width, self.default_height))
        while True:
            start_time = time.time()
            
            if self.end_flag.value == 1:
                break

            ret, frame = self.capture.read()
            cap_out.write(cv2.resize(frame,(self.default_width, self.default_height)))
            #print(len(frame[1]),':',len(frame[2]))
            show_frame = cv2.resize(frame, (self.cap_width.value,self.cap_height.value))
            cv2.imshow('capture', show_frame)
            queue.put(frame)
            cv2.moveWindow('capture', 0, 0)
            key =cv2.waitKey(1)
            end_time = time.time()
            sleep_time = fps_time - (end_time - start_time)
            if sleep_time > 0:
                time.sleep(sleep_time)
            else :
                print('fps over')
        self.capture.release()
        print('relese capture board')
        cap_out.release()
        print('created main capture mp4')

    def analyze_frame(self,queue,res_dict):
        past_frames=[]
        frame_num = 0
        save_time = self.conf["save_time"]
        kill_conf = self.conf["kill"]
        kill_waste_count = 0
        death_conf = self.conf["death"]
        death_waste_count = 0
        salmon_death_conf = self.conf["salmon_death"]
        salmon_death_waste_count = 0
        while True:
            start_time = time.time()
            try:
                if self.end_flag.value == 1:
                    break
                frame = queue.get(timeout=1)
                frame_num += 1
                kill_waste_count -=1
                death_waste_count -=1
                salmon_death_waste_count -=1

                if len(past_frames) == (int)(self.fps * save_time):
                    past_frames.pop(0)
                past_frames.append(frame)
                
                ret = self.check_color(frame,kill_conf)
                if ret and kill_waste_count<0:
                    name = self.conf["cap_save_dir"]+ self.save_name.replace('.mp4','_kill_'+str(frame_num) +'.mp4')
                    self.save_trim(past_frames,name)
                    tmp = res_dict["kill"]
                    tmp.append({"name":name})
                    res_dict["kill"] = tmp
                    kill_waste_count = int(self.conf["kill_waste_time"] * self.fps)

                ret = self.check_color(frame,death_conf)
                if ret and death_waste_count<0:
                    name = self.conf["cap_save_dir"]+ self.save_name.replace('.mp4','_death_'+str(frame_num) +'.mp4')
                    self.save_trim(past_frames,name)
                    tmp = res_dict["death"]
                    tmp.append({"name":name})
                    res_dict["death"] = tmp
                    death_waste_count = int(self.conf["death_waste_time"] * self.fps)

                ret = self.check_color(frame,salmon_death_conf)
                if ret and salmon_death_waste_count < 0:
                    name = self.conf["cap_save_dir"]+ self.save_name.replace('.mp4','_salmon_death_'+str(frame_num) +'.mp4')
                    self.save_trim(past_frames,name)
                    tmp = res_dict["salmon_death"]
                    tmp.append({"name":name})
                    res_dict["salmon_death"] = tmp
                    salmon_death_waste_count = int(self.conf["salmon_death_time"] * self.fps)

            except queue.Empty:
                print('analyze:not yet reach frame')
                pass
            end_time = time.time()
            #print(1/(end_time - start_time))

    def save_trim(self,past_frames,name):
        fourcc = cv2.VideoWriter_fourcc('m', 'p', '4', 'v')
        out = cv2.VideoWriter(name, fourcc, self.fps, (self.default_width, self.default_height))
        for frame in past_frames:
            out.write(cv2.resize(frame,(self.default_width, self.default_height)))
        out.release()
        print('created')

    def check_color(self,frame,conf):
        for c in conf:
            color = c["color_value"]
            reliability = c["reliability"]
            trim = c["image"]
            y_start = int(trim[0] * len(frame) /  self.orign_height)
            y_end = int(trim[1] * len(frame) /  self.orign_height)
            x_start = int(trim[2] * len(frame[0]) /  self.orign_width)
            x_end = int(trim[3] * len(frame[0]) /  self.orign_width)
            if y_start >= y_end or x_start >= x_end:
                return False
            tmp_frame = frame[y_start:y_end,x_start:x_end]
            average_color_row = np.average(tmp_frame, axis=0)
            average_color = np.average(average_color_row, axis=0)

            if (average_color[0]-reliability < color[0] and 
                color[0] < average_color[0]+reliability and
                average_color[1]-reliability < color[1] and 
                color[1] < average_color[1]+reliability and
                average_color[2]-reliability < color[2] and 
                color[2] < average_color[2]+reliability ):
                    continue
            return False
        return True

    def trim_show(self,name):
        
        trim = cv2.VideoCapture(name)
        fps = trim.get(cv2.CAP_PROP_FPS)*1.3
        fps_time = 1/fps
        while True :
            start_time = time.time()
            ret, img = trim.read()
            if ret == False:
                break
            show_frame = cv2.resize(img, (self.cap_width.value,self.cap_height.value))
            cv2.imshow('capture_trim', show_frame)
            cv2.moveWindow('capture_trim', 0, 0)
            key =cv2.waitKey(1)
            end_time = time.time()
            sleep_time = fps_time - (end_time - start_time)
            if sleep_time > 0:
                time.sleep(sleep_time)
            else :
                print('fps over')
        trim.release()

    def trim_start(self,name):
        p = Process(target=self.trim_show,args=(name,))
        p.start()
