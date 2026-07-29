using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using ScreenRecordingTool.Models;

namespace ScreenRecordingTool.Services
{
    /// <summary>
    /// 基于 Windows 自带 GDI 抓屏（BitBlt/CopyFromScreen）的录屏实现：
    /// 后台线程按帧率截屏 → JPEG 编码 → 写入 AVI(MJPEG) 容器，全程无第三方依赖。
    /// </summary>
    public sealed class GdiScreenRecorderService : IScreenRecorderService
    {
        private const int SM_XVIRTUALSCREEN = 76;
        private const int SM_YVIRTUALSCREEN = 77;
        private const int SM_CXVIRTUALSCREEN = 78;
        private const int SM_CYVIRTUALSCREEN = 79;

        [DllImport("user32.dll")]
        private static extern int GetSystemMetrics(int nIndex);

        private CancellationTokenSource? _cts;
        private Task? _captureTask;
        private string? _outputFilePath;
        private Exception? _captureError;

        public bool IsRecording => _cts is { IsCancellationRequested: false };

        public void Start(RecordingSettings settings, string outputFilePath)
        {
            if (_cts != null)
            {
                throw new InvalidOperationException("录制已在进行中。");
            }

            // 边界处理：帧率钳制到合理范围，避免除零或过高帧率拖垮 CPU
            int fps = Math.Clamp(settings.FrameRate, 1, 60);

            _outputFilePath = outputFilePath;
            _captureError = null;
            _cts = new CancellationTokenSource();
            var token = _cts.Token;

            // 长时间运行的抓屏循环放到独立线程，避免占用线程池
            _captureTask = Task.Factory.StartNew(
                () => CaptureLoop(outputFilePath, fps, token),
                token,
                TaskCreationOptions.LongRunning,
                TaskScheduler.Default);
        }

        public async Task<string> StopAsync()
        {
            if (_cts == null || _captureTask == null)
            {
                throw new InvalidOperationException("当前没有正在进行的录制。");
            }

            // 防御性保护：防止 StopAsync 被并发多次调用（如 fire-and-forget + Dispose 同时触发）
            var cts = _cts;
            var captureTask = _captureTask;

            cts.Cancel();
            try
            {
                await captureTask.ConfigureAwait(false);
            }
            finally
            {
                // 仅首次调用者负责清理状态；若已被其他调用者清理则跳过
                if (_cts == cts)
                {
                    cts.Dispose();
                    _cts = null;
                    _captureTask = null;
                }
            }

            if (_captureError != null)
            {
                throw new InvalidOperationException($"录制过程中发生错误：{_captureError.Message}", _captureError);
            }

            return _outputFilePath!;
        }

        private void CaptureLoop(string outputFilePath, int fps, CancellationToken token)
        {
            try
            {
                // 取整个虚拟屏幕（多显示器）范围
                int left = GetSystemMetrics(SM_XVIRTUALSCREEN);
                int top = GetSystemMetrics(SM_YVIRTUALSCREEN);
                int width = GetSystemMetrics(SM_CXVIRTUALSCREEN);
                int height = GetSystemMetrics(SM_CYVIRTUALSCREEN);

                // AVI/JPEG 要求偶数尺寸，向下取偶避免解码器兼容问题
                width &= ~1;
                height &= ~1;

                var jpegCodec = GetJpegCodec();
                using var encoderParams = new EncoderParameters(1);
                encoderParams.Param[0] = new EncoderParameter(Encoder.Quality, 75L);

                using var bitmap = new Bitmap(width, height, PixelFormat.Format24bppRgb);
                using var graphics = Graphics.FromImage(bitmap);
                using var frameBuffer = new MemoryStream(width * height / 4);
                using var aviWriter = new AviMjpegWriter(outputFilePath, width, height, fps);

                long frameIntervalTicks = Stopwatch.Frequency / fps;
                var stopwatch = Stopwatch.StartNew();
                long nextFrameTick = 0;

                while (!token.IsCancellationRequested)
                {
                    // 截屏（Windows GDI BitBlt）
                    graphics.CopyFromScreen(left, top, 0, 0, bitmap.Size);

                    // JPEG 编码（复用缓冲流，避免每帧分配大内存）
                    frameBuffer.SetLength(0);
                    bitmap.Save(frameBuffer, jpegCodec, encoderParams);
                    aviWriter.AddFrame(frameBuffer.GetBuffer(), (int)frameBuffer.Length);

                    // 帧率节拍：按绝对时间对齐，防止编码耗时导致累积漂移
                    nextFrameTick += frameIntervalTicks;
                    long sleepTicks = nextFrameTick - stopwatch.ElapsedTicks;
                    if (sleepTicks > 0)
                    {
                        int sleepMs = (int)(sleepTicks * 1000 / Stopwatch.Frequency);
                        if (sleepMs > 0 && token.WaitHandle.WaitOne(sleepMs))
                        {
                            break;
                        }
                    }
                    else
                    {
                        // 机器性能不足跟不上帧率时，重置节拍避免持续满负荷追帧
                        nextFrameTick = stopwatch.ElapsedTicks;
                    }
                }

                aviWriter.Finish();
            }
            catch (Exception ex)
            {
                _captureError = ex;
            }
        }

        private static ImageCodecInfo GetJpegCodec()
        {
            return ImageCodecInfo.GetImageEncoders()
                       .FirstOrDefault(c => c.FormatID == ImageFormat.Jpeg.Guid)
                   ?? throw new NotSupportedException("当前系统缺少 JPEG 编码器。");
        }

        public void Dispose()
        {
            // 应用退出等场景下仍在录制时，确保文件正确收尾，避免生成损坏的 AVI
            if (_cts != null)
            {
                try
                {
                    StopAsync().GetAwaiter().GetResult();
                }
                catch
                {
                    // 释放阶段不再向外抛出
                }
            }
        }
    }
}
