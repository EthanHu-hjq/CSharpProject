using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NAudio.Wave;
using SharpAvi;
using SharpAvi.Codecs;
using SharpAvi.Output;
using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using System.Windows.Forms; // 注意：需添加 System.Windows.Forms 引用（用于屏幕捕获）

namespace ScreenRecorderApp.ViewModels
{
    public partial class RecorderViewModel : ObservableObject
    {
        #region 界面绑定属性
        [ObservableProperty]
        private string _buttonText = "开始录屏";

        [ObservableProperty]
        private string _buttonIcon = ""; // 录屏初始图标（Material Design）

        [ObservableProperty]
        private string _recordTime = "00:00:00";

        [ObservableProperty]
        private bool _isRecording = false;
        #endregion

        #region 录屏核心对象
        private AviWriter _aviWriter; // SharpAvi 核心对象
        private Thread _videoThread; // 视频录制线程
        private IAudioRecorder _audioRecorder; // 音频录制对象
        private CancellationTokenSource _cts; // 取消令牌（停止录屏）
        private Stopwatch _stopwatch; // 计时工具
        private DispatcherTimer _timer; // 实时更新时长
        private string _tempVideoPath; // 临时文件路径
        private readonly int _frameRate = 30; // 录屏帧率
        private readonly int _audioSampleRate = 44100; // 音频采样率
        #endregion

        public RecorderViewModel()
        {
            // 初始化临时文件
            _tempVideoPath = Path.Combine(Path.GetTempPath(), $"ScreenRecord_{Guid.NewGuid()}.avi");

            // 初始化定时器（每秒更新时长）
            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            _timer.Tick += Timer_Tick;
        }

        #region 按钮命令（核心交互）
        [RelayCommand]
        private void ToggleRecord()
        {
            if (!IsRecording)
            {
                _ = StartRecordingAsync(); // 异步启动录屏，避免UI阻塞
            }
            else
            {
                StopRecording();
            }
        }
        #endregion

        #region 录屏核心方法
        private async Task StartRecordingAsync()
        {
            try
            {
                // 获取屏幕分辨率（录制整个屏幕）
                var screenBounds = System.Windows.Forms.Screen.PrimaryScreen.Bounds;
                var videoWidth = screenBounds.Width;
                var videoHeight = screenBounds.Height;

                // 初始化AviWriter（创建临时视频文件）
                _aviWriter = new AviWriter(_tempVideoPath)
                {
                    FramesPerSecond = _frameRate,
                    EmitIndex1 = true
                };

                // 创建视频流（使用MPEG-4编码，压缩率更高）
                var videoStream = _aviWriter.AddVideoStream(
                    width: videoWidth,
                    height: videoHeight,
                    codec: KnownFourCCs.Codecs.MP4V);
                videoStream.Name = "Screen Recording";

                // 初始化音频录制（NAudio + SharpAvi）
                _audioRecorder = new NAudioAudioRecorder(_aviWriter, _audioSampleRate);
                await _audioRecorder.StartAsync();

                // 启动视频录制线程（避免阻塞UI）
                _cts = new CancellationTokenSource();
                _videoThread = new Thread(() => RecordVideoFrames(videoStream, _cts.Token))
                {
                    IsBackground = true,
                    Priority = ThreadPriority.Highest
                };
                _videoThread.Start();

                // 启动计时器，更新界面状态
                _stopwatch = Stopwatch.StartNew();
                _timer.Start();
                IsRecording = true;
                ButtonText = "录屏中";
                ButtonIcon = ""; // 录屏中图标
            }
            catch (Exception ex)
            {
                MessageBox.Show($"开始录屏失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                CleanupResources(); // 失败时清理资源
            }
        }

        private void StopRecording()
        {
            try
            {
                // 停止计时和界面更新
                _stopwatch.Stop();
                _timer.Stop();

                // 停止视频录制线程
                _cts?.Cancel();
                _videoThread?.Join(2000); // 等待线程结束（最多2秒）

                // 停止音频录制
                _audioRecorder?.StopAsync().Wait();

                // 关闭AviWriter，完成视频文件写入
                _aviWriter?.Close();

                // 恢复界面状态
                IsRecording = false;
                ButtonText = "开始录屏";
                ButtonIcon = "";
                RecordTime = "00:00:00";

                // 弹出保存对话框
                SaveRecordedVideo();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"停止录屏失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                CleanupResources(); // 清理资源
            }
        }

        // 视频帧录制逻辑（后台线程执行）
        private void RecordVideoFrames(IAviVideoStream videoStream, CancellationToken token)
        {
            var frameInterval = TimeSpan.FromSeconds(1.0 / _frameRate);
            var buffer = new byte[videoStream.Width * videoStream.Height * 3];
            var screenCapture = new Bitmap(videoStream.Width, videoStream.Height);
            var graphics = Graphics.FromImage(screenCapture);

            while (!token.IsCancellationRequested)
            {
                var startTime = DateTime.Now;

                // 捕获屏幕画面
                graphics.CopyFromScreen(0, 0, 0, 0, screenCapture.Size);
                // 将位图转换为视频帧数据
                var bits = screenCapture.LockBits(
                    new Rectangle(0, 0, screenCapture.Width, screenCapture.Height),
                    System.Drawing.Imaging.ImageLockMode.ReadOnly,
                    System.Drawing.Imaging.PixelFormat.Format24bppRgb);
                System.Runtime.InteropServices.Marshal.Copy(bits.Scan0, buffer, 0, buffer.Length);
                screenCapture.UnlockBits(bits);

                // 写入视频帧
                videoStream.WriteFrame(true, buffer, 0, buffer.Length);

                // 控制帧率（避免录制过快）
                var elapsed = DateTime.Now - startTime;
                if (elapsed < frameInterval)
                {
                    Thread.Sleep(frameInterval - elapsed);
                }
            }

            // 释放资源
            graphics.Dispose();
            screenCapture.Dispose();
        }

        // 保存录屏文件
        private void SaveRecordedVideo()
        {
            if (!File.Exists(_tempVideoPath))
            {
                MessageBox.Show("录屏文件未生成", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var saveFileDialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "AVI 视频文件 (*.avi)|*.avi",
                Title = "保存录屏文件",
                FileName = $"录屏_{DateTime.Now:yyyyMMddHHmmss}.avi",
                DefaultExt = ".avi"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                try
                {
                    // 复制临时文件到用户选择的路径
                    File.Copy(_tempVideoPath, saveFileDialog.FileName, true);
                    MessageBox.Show($"录屏已保存至：{saveFileDialog.FileName}", "保存成功", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"保存失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                finally
                {
                    // 删除临时文件
                    DeleteTempFile();
                }
            }
            else
            {
                // 用户取消保存，删除临时文件
                DeleteTempFile();
            }
        }

        // 定时器更新录屏时长
        private void Timer_Tick(object? sender, EventArgs e)
        {
            if (_stopwatch?.IsRunning == true)
            {
                var elapsed = _stopwatch.Elapsed;
                RecordTime = $"{elapsed.Hours:D2}:{elapsed.Minutes:D2}:{elapsed.Seconds:D2}";
            }
        }

        // 清理资源（关键：避免文件占用）
        private void CleanupResources()
        {
            _cts?.Dispose();
            _audioRecorder?.Dispose();
            _aviWriter?.Dispose();
            DeleteTempFile();
        }

        // 删除临时文件
        private void DeleteTempFile()
        {
            if (File.Exists(_tempVideoPath))
            {
                try
                {
                    File.Delete(_tempVideoPath);
                }
                catch
                {
                    // 忽略删除失败（可能文件还在占用，下次启动自动清理）
                }
            }
        }
        #endregion

        #region 音频录制辅助类（适配SharpAvi）
        // NAudio 音频录制实现（与 SharpAvi 配合）
        private class NAudioAudioRecorder : IAudioRecorder, IDisposable
        {
            private readonly AviWriter _writer;
            private readonly IAviAudioStream _audioStream;
            private readonly WaveInEvent _waveIn;
            private readonly byte[] _buffer;
            private bool _isDisposed;

            public NAudioAudioRecorder(AviWriter writer, int sampleRate = 44100)
            {
                _writer = writer;
                // 创建音频流（16位立体声，44.1kHz）
                _audioStream = writer.AddAudioStream(
                    sampleRate: sampleRate,
                    channels: 2,
                    bitsPerSample: 16);

                // 初始化NAudio音频捕获
                _waveIn = new WaveInEvent
                {
                    WaveFormat = new WaveFormat(sampleRate, 16, 2),
                    BufferMilliseconds = 100
                };
                _waveIn.DataAvailable += WaveIn_DataAvailable;
                _buffer = new byte[_waveIn.WaveFormat.AverageBytesPerSecond / 10];
            }

            public async Task StartAsync()
            {
                await Task.Run(() => _waveIn.StartRecording());
            }

            public async Task StopAsync()
            {
                await Task.Run(() => _waveIn.StopRecording());
            }

            private void WaveIn_DataAvailable(object? sender, WaveInEventArgs e)
            {
                // 将音频数据写入Avi流
                _audioStream.WriteSamples(e.Buffer, 0, e.BytesRecorded);
            }

            public void Dispose()
            {
                if (!_isDisposed)
                {
                    _waveIn.Dispose();
                    _isDisposed = true;
                }
            }
        }

        // 音频录制接口
        private interface IAudioRecorder : IDisposable
        {
            Task StartAsync();
            Task StopAsync();
        }
        #endregion
    }
}
