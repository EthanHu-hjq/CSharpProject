using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FFmpeg.AutoGen;
using System.Threading.Tasks;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace ScreenRecorder.Services
{
    public class ScreenRecordService
    {
        // FFmpeg相关核心变量
        private static IntPtr _formatContext;
        private static IntPtr _videoStream;
        private static IntPtr _codecContext;
        private static int _frameIndex;
        private static readonly object _lockObj = new object();
        private static bool _isRecording = false;

        // 2. 初始化录屏参数
        int screenWidth = Screen.PrimaryScreen.Bounds.Width;
        int screenHeight = Screen.PrimaryScreen.Bounds.Height;
        int fps = 30; // 录屏帧率
        string outputFile = "screen_record.mp4";

        // Windows GDI相关API声明
        [DllImport("user32.dll")]
        private static extern IntPtr GetDesktopWindow();

        [DllImport("user32.dll")]
        private static extern IntPtr GetWindowDC(IntPtr hWnd);

        [DllImport("gdi32.dll")]
        private static extern IntPtr CreateCompatibleDC(IntPtr hdc);

        [DllImport("gdi32.dll")]
        private static extern IntPtr CreateCompatibleBitmap(IntPtr hdc, int nWidth, int nHeight);

        [DllImport("gdi32.dll")]
        private static extern IntPtr SelectObject(IntPtr hdc, IntPtr hObject);

        [DllImport("gdi32.dll")]
        private static extern bool BitBlt(IntPtr hdcDest, int nXDest, int nYDest, int nWidth, int nHeight, IntPtr hdcSrc, int nXSrc, int nYSrc, int dwRop);

        [DllImport("gdi32.dll")]
        private static extern bool DeleteDC(IntPtr hdc);

        [DllImport("gdi32.dll")]
        private static extern bool DeleteObject(IntPtr hObject);

        [DllImport("user32.dll")]
        private static extern int ReleaseDC(IntPtr hWnd, IntPtr hdc);

        public ScreenRecordService()
        {
            ffmpeg.RootPath = @"D:\Tools\ffmpeg-master-latest-win64-gpl-shared\bin";
            ffmpeg.av_log_set_level(ffmpeg.AV_LOG_ERROR);
        }


    }
}
