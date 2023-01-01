using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Threading;
using System.Diagnostics;
using System.Net.Sockets;
using System.Net;
using System.Windows.Threading;
using System.IO;
using Newtonsoft.Json;
using System.Net.Http;
using System.Text;
using System.Windows.Controls;
using Newtonsoft.Json.Linq;

namespace SplatoonCapture
{
    internal class PythonControl
    {
        MainWindow mainWindow;
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)] public static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        private DispatcherTimer monitorTimer;
        private DispatcherTimer getCapTrimerTimer;

        internal PythonControl(MainWindow mainWindow)
        {
            this.mainWindow = mainWindow;
            StartPython();
            SetMonitor();
        }

        void StartPython()
        {
            //python -m uvicorn fastapi_sample:app --reload
            string filename = @"analyze_splatoon.py";
            FileInfo fi = new FileInfo(filename);
            if (!fi.Exists)
            {
                return;
            }
            var p = Process.Start(new ProcessStartInfo
            {
                FileName = "python",
                Arguments = @"analyze_splatoon.py",
                WindowStyle = ProcessWindowStyle.Minimized,
            });
        }
        internal void StopPython()
        {
            Process[] ps =Process.GetProcessesByName("python");

            foreach (Process p in ps)
            {
                //プロセスを強制的に終了させる
                p.Kill();
            }
        }

        void SetMonitor()
        {
            monitorTimer = new DispatcherTimer();
            monitorTimer.Interval = new TimeSpan(0, 0, 0,0,300);
            monitorTimer.Tick += new EventHandler(MonitorPython);
            monitorTimer.Start();
            getCapTrimerTimer = new DispatcherTimer();
            getCapTrimerTimer.Interval = new TimeSpan(0, 0, 1);
            getCapTrimerTimer.Tick += new EventHandler(GetCaptureRes);
            getCapTrimerTimer.Start();
        }

        async void GetCaptureRes(object sender, EventArgs e)
        {
            string response = await GetRequest("/capture/all");
            try
            {
                JObject jsonObject = JObject.Parse(response);
                KillItem[] kills = jsonObject["kill"].ToObject<KillItem[]>();
                for (int i = 0; i < kills.Length; i++) kills[i].No = i + 1;
                mainWindow.dataGridModel.CapKillItems = kills;

                DeathItem[] deathes = jsonObject["death"].ToObject<DeathItem[]>();
                for (int i = 0; i < deathes.Length; i++) deathes[i].No = i + 1;
                mainWindow.dataGridModel.CapDeathItems = deathes;

                SalmonDeathItem[] salmonDeathes = jsonObject["salmon_death"].ToObject<SalmonDeathItem[]>();
                for (int i = 0; i < salmonDeathes.Length; i++) salmonDeathes[i].No = i + 1;
                mainWindow.dataGridModel.CapSalmonDeathItems = salmonDeathes;
            }
            catch(Exception ex)
            {
                mainWindow.ErrorNotice(ex);
                return;
            }
            
        }

        void MonitorPython(object sender, EventArgs e)
        {

            IntPtr graphHWnd = GetWHandle("capture");
            if (graphHWnd != IntPtr.Zero) {
                mainWindow.SetParentPython(MainWindow.HostAreaEnum.Capture, graphHWnd);
            }
            graphHWnd = GetWHandle("capture_trim");
            if (graphHWnd != IntPtr.Zero)
            {
                mainWindow.SetParentPython(MainWindow.HostAreaEnum.Capture_trim, graphHWnd);
            }
        }


        IntPtr GetWHandle(string name)
        {
            //IntPtr hWnd = FindWindow(null, "pyplot-title");
            IntPtr hWnd = FindWindow(null, name);
            return hWnd;
        }

        internal async void RequestStartCapture()
        {
            var info = new CaptureModel
            {
                device_id = int.Parse(((ComboBoxItem)mainWindow.captureBoardIdComboBox.SelectedItem).Content.ToString()),
                save_name = mainWindow.saveNameTextBox.Text,
                save_fps = (int)mainWindow.fpsSlider.Value,
                width = mainWindow.captureBoardHost.Width,
                height = mainWindow.captureBoardHost.Height
            };
            string json = JsonConvert.SerializeObject(info, Formatting.Indented);
            await PostRequest(json,"/capture");
        }

        internal async void RequestStartTrim(string name)
        {
            var info = new CaptureModel
            {
                save_name = name
            };
            string json = JsonConvert.SerializeObject(info, Formatting.Indented);
            await PostRequest(json, "/capture/trim");
        }

        internal async void RequestSizeChange(MainWindow.HostAreaEnum host)
        {
            var info = new CaptureModel
            {
                width = (int)mainWindow.captureBoardHost.Width,
                height = (int)mainWindow.captureBoardHost.Height
                //width = (int)mainWindow.captureBoardWIndowFormHost.ActualWidth,
                //height = (int)mainWindow.captureBoardWIndowFormHost.ActualHeight
            };
            string json = JsonConvert.SerializeObject(info, Formatting.Indented);

            string urlparm = string.Empty;
            switch (host)
            {
                case MainWindow.HostAreaEnum.Capture:
                    urlparm = "/capture/resize";
                    break;
            }


            await PostRequest(json, urlparm);
        }

        async Task<bool> PostRequest(string json,string urlParameter)
        {
            var client = new HttpClient();
            // タイムアウト時間の設定(5秒)
            client.Timeout = TimeSpan.FromMilliseconds(5000);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            try
            {
                var response = await client.PostAsync("http://127.0.0.1:8080"+ urlParameter, content);
                var resString = await response.Content.ReadAsStringAsync();
            }
            catch (Exception ex)
            {
                mainWindow.ErrorNotice(ex);
                return false;
            }
            return true;
        }
        async Task<string> GetRequest(string urlParameter)
        {
            var client = new HttpClient();
            // タイムアウト時間の設定(5秒)
            client.Timeout = TimeSpan.FromMilliseconds(5000);
            string resString;
            try
            {
                var response = await client.GetAsync("http://127.0.0.1:8080" + urlParameter);
                resString = await response.Content.ReadAsStringAsync();
            }
            catch (Exception ex)
            {
                mainWindow.ErrorNotice(ex);
                return string.Empty;
            }
            return resString;
        }
    }

    public class CaptureModel
    {
        //class CaptureModel(BaseModel):
        //    device_id: int
        //    save_name: str
        //    save_fps: int
        //    width: int
        public int device_id { get; set; }
        public string save_name { get; set; } = string.Empty;
        public int save_fps { get; set; }
        public int width { get; set; }
        public int height { get; set; }
    }
}
