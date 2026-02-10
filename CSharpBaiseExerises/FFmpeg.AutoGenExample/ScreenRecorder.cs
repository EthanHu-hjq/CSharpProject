using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using Accord.Video.FFMPEG;
using System.Threading;
using System.IO;
using System.Windows.Forms;

namespace FFmpeg.AutoGenExample
{
    public class ScreenRecorder
    {
        #region 属性
        //录屏核心变量
        private bool _isRecording = false;//录制状态
        private VideoFileWriter _videoWriter;//视频写入器
        private Thread _captureThread;//捕获线程
        private System.Threading.Timer _autoStopTimer;//自动停止计时器
        private string _outputFilePath;//输出文件路径
        private string directoryPath = @"D:\TYM_ScreenRecord";//录屏文件保存目录

        //配置参数
        private readonly int _frameRate = 10;//帧率
        private readonly int _autoStopSeconds = 600;//自动停止时间（秒）
        private readonly int _bitRate = 1500000;//比特率
        #endregion

        #region 事件
        public event Action<int, bool> OnRecordingStateChanged;//录制状态改变事件
        public event Action<int, string> OnRecordingCompleted;//录制完成事件
        public event Action<int, string> OnRecordingError;//录制错误事件
        #endregion

        public ScreenRecorder()
        {
            //初始化自动停止计时器（600秒后触发）
            _autoStopTimer = new System.Threading.Timer(
                AutoStopTimerCallback,
                null,
                Timeout.Infinite,
                Timeout.Infinite);
        }

        /// <summary>
        /// 开始录制
        /// </summary>
        public void StartRecording(string sn, string projectName)
        {
            try
            {
                if (_isRecording) return;

                //创建保存目录（按项目分类）
                directoryPath = Path.Combine(directoryPath, $"ScreenRecord_{DateTime.Now:yyyyMMdd_HHmmss}");

                //保存文件路径
                string fileName = $"ScreenRecord_{DateTime.Now:yyyyMMdd_HHmmss}.mp4";
                if (!Directory.Exists(directoryPath))
                {
                    Directory.CreateDirectory(directoryPath);
                }
                _outputFilePath = Path.Combine(
                    directoryPath,
                    fileName);

                //获取屏幕分辨路
                var screen = Screen.PrimaryScreen;
                int width = screen.Bounds.Width;
                int height = screen.Bounds.Height;

                //初始化视频写入器
                _videoWriter = new VideoFileWriter();
                _videoWriter.Open(
                    _outputFilePath,
                    width,
                    height,
                    _frameRate,
                    VideoCodec.MPEG4,
                    _bitRate);

                //更新状态
                _isRecording = true;

                //启动自动停止计时器（600秒后触发）
                _autoStopTimer.Change(_autoStopSeconds * 1000, Timeout.Infinite);

                //启动捕获线程
                _captureThread = new Thread(CaptureScreenLoop)
                {
                    IsBackground = true,
                    Priority = ThreadPriority.BelowNormal,
                    Name = $"ScreenCapture"
                };
                _captureThread.Start();

                //可在此处触发开始事件（如有需要）
            }
            catch (Exception ex)
            {
                StopRecording();
                throw new Exception($"启动录制失败：{ex.Message}");
            }
        }

        public void StopRecording()
        {
            try
            {
                if (!_isRecording) return;

                //停止录制
                _isRecording = false;

                //停止自动计时器
                _autoStopTimer.Change(Timeout.Infinite, Timeout.Infinite);

                //等待捕获线程结束
                if (_captureThread != null && _captureThread.IsAlive)
                {
                    _captureThread.Join(1000);
                }

                //关闭视频写入器
                if (_videoWriter != null && _videoWriter.IsOpen)
                {
                    _videoWriter.Close();
                    _videoWriter.Dispose();
                }

                //根据日期判断是否需要清理旧文件，满一个月的删除
                var files = Directory.GetFiles(directoryPath);
                foreach (var file in files)
                {
                    try
                    {
                        var creationTime = File.GetCreationTime(file);
                        int dateInterval = (DateTime.Now - creationTime).Days;
                        if (dateInterval > 30)
                        {
                            File.Delete(file);
                        }
                    }
                    catch
                    {
                        //忽略删除文件时的异常
                    }
                }

            }
            catch (Exception ex)
            {
                throw new Exception($"停止录制失败：{ex.Message}");
            }
        }

        /// <summary>
        /// 屏幕捕获循环（独立线程）
        /// </summary>
        private void CaptureScreenLoop()
        {
            while (_isRecording)
            {
                try
                {
                    using (var frame = CaptureScreen())
                    {
                        _videoWriter.WriteVideoFrame(frame);
                    }
                    Thread.Sleep(1000 / _frameRate);
                }
                catch (Exception ex)
                {
                    StopRecording();
                    throw new Exception(ex.Message);
                }
            }
        }
        /// <summary>
        /// 捕获屏幕帧
        /// </summary>
        private Bitmap CaptureScreen()
        {
            var screen = Screen.PrimaryScreen;
            var bitmap = new Bitmap(screen.Bounds.Width, screen.Bounds.Height);
            using (var g = Graphics.FromImage(bitmap))
            {
                g.CopyFromScreen(
                    screen.Bounds.X, screen.Bounds.Y,
                    0, 0,
                    screen.Bounds.Size,
                    CopyPixelOperation.SourceCopy);
            }
            return bitmap;
        }

        /// <summary>
        /// 到达自动停止时间后的回调
        /// </summary>
        /// <param name="state"></param>
        /// <exception cref="NotImplementedException"></exception>
        private void AutoStopTimerCallback(object state)
        {
            if (_isRecording)
            {
                StopRecording();
            }
        }

        #region 释放资源
        /// <summary>
        /// 释放资源
        /// </summary>
        // 完整实现 IDisposable 规范
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this); // 告诉GC无需调用析构函数
        }

        // 拆分托管/非托管资源释放
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                // 释放托管资源（由GC管理的对象）
                StopRecording(); // 自动停止=测试通过，删除文件（修正业务逻辑）

                // 停止并释放计时器
                _autoStopTimer?.Change(Timeout.Infinite, Timeout.Infinite);
                _autoStopTimer?.Dispose();
                _autoStopTimer = null;

                // 强制终止捕获线程（避免残留）
                if (_captureThread != null && _captureThread.IsAlive)
                {
                    try
                    {
                        if (!_captureThread.Join(1000)) // 等待1秒，超时则强制终止
                        {
                            _captureThread.Abort();
                        }
                    }
                    catch (Exception ex)
                    {
                        throw new Exception($"强制终止捕获线程失败：{ex.Message}");
                    }
                }
                _captureThread = null;

                // 释放视频写入器并置空
                if (_videoWriter != null)
                {
                    if (_videoWriter.IsOpen)
                    {
                        _videoWriter.Close();
                    }
                    _videoWriter.Dispose();
                    _videoWriter = null;
                }
            }
            // 释放非托管资源（此处无，留空）
        }

        // 析构函数：兜底释放非托管资源（Dispose未被调用时触发）
        ~ScreenRecorder()
        {
            Dispose(false);
        }
        #endregion
    }
}
