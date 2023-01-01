using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using MahApps.Metro.Controls;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace SplatoonCapture
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : MetroWindow
    {

        private const int GWL_STYLE = -16;
        private const int WS_THICKFRAME = 0x00040000;
        private const int WS_CAPTION = 0x00C00000;
        private const int WS_CHILD = 0x40000000;
        [DllImport("user32")] private static extern int GetWindowLong(IntPtr hWnd, int nIndex);
        [DllImport("user32")] private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);
        [DllImport("user32")] private static extern IntPtr SetParent(IntPtr hWndChild, IntPtr hWndNewParent);
        [DllImport("user32")] private static extern int MoveWindow(IntPtr hwnd, int x, int y, int nWidth, int nHeight, int bRepaint);
        public enum HostAreaEnum
        {
            Capture,
            Capture_trim,
            Record,
            Record_trim
        }

        PythonControl pythonControl;
        public DataGridModel dataGridModel = new DataGridModel();

        public MainWindow()
        {
            InitializeComponent();
            saveNameTextBox.Text = DateTime.Now.ToString("yyyy_MM_ddTHH_mm_ss")+".mp4";
            pythonControl = new PythonControl(this);
            this.DataContext = dataGridModel;
        }
        public void ErrorNotice(Exception e)
        {
            snackbarNotice.MessageQueue.Enqueue(e.Message);
            //MessageBox.Show(e.Message);
        }
        public void SetParentPython( HostAreaEnum areaEnum, IntPtr graphHWnd)
        {
            System.Windows.Forms.Control area;
            switch (areaEnum)
            {
                case HostAreaEnum.Capture:
                    area = this.captureBoardHost;
                    break;
                case HostAreaEnum.Capture_trim:
                    area = this.captureBoardTrimHost;
                    break;
                default:
                    return;

            }
            var style = GetWindowLong(graphHWnd, GWL_STYLE);
            style = style & ~WS_CAPTION & ~WS_THICKFRAME;
            SetWindowLong(graphHWnd, GWL_STYLE, style);
            SetParent(graphHWnd, area.Handle);

            var offset = (int)SystemParameters.MenuHeight
                + (int)SystemParameters.ResizeFrameHorizontalBorderHeight * 2;
            area.SizeChanged += (s, e)
                => MoveWindow(graphHWnd, 0, -offset, area.Width, area.Height + offset, 1);
        }

        private void captureStartButton_Click(object sender, RoutedEventArgs e)
        {
            pythonControl.RequestStartCapture();
        }

        int bef_width = 0;
        private void captureBoardHost_SizeChanged(object sender, EventArgs e)
        {
            if (bef_width == captureBoardHost.Width) return;
            pythonControl.RequestSizeChange(MainWindow.HostAreaEnum.Capture);
            bef_width = captureBoardHost.Width;
        }

        private void trimKillStartButton_Click(object sender, RoutedEventArgs e)
        {
            int i = captureKillDataGrid.SelectedIndex;
            pythonControl.RequestStartTrim(dataGridModel.CapKillItems[i].Name);
        }
        private void trimDeathStartButton_Click(object sender, RoutedEventArgs e)
        {
            int i = captureDeathDataGrid.SelectedIndex;
            pythonControl.RequestStartTrim(dataGridModel.CapDeathItems[i].Name);
        }
        private void trimSalmonDeathStartButton_Click(object sender, RoutedEventArgs e)
        {
            int i = captureSalmonDeathDataGrid.SelectedIndex;
            pythonControl.RequestStartTrim(dataGridModel.CapSalmonDeathItems[i].Name);
        }

        private void MetroWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            pythonControl.StopPython();
        }
    }
}
