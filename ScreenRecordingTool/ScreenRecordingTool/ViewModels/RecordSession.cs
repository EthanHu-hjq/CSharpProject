using System.Collections.Generic;
using System.IO;
using System.Windows.Input;
using ScreenRecordingTool.Models;
using ScreenRecordingTool.Mvvm;
using ScreenRecordingTool.Services;

namespace ScreenRecordingTool.ViewModels
{
    /// <summary>
    /// 一次独立的录制任务（会话）。每个实例拥有自己的录屏服务、状态与输出文件，
    /// 因此多个会话可并行运行且互不干扰。
    /// </summary>
    public class RecordSession : ViewModelBase, IDisposable
    {
        private readonly Func<IScreenRecorderService> _recorderFactory;
        private readonly RecordingSettings _settings;

        private IScreenRecorderService? _recorder;
        private bool _isRecording;
        private string _statusText = "就绪";
        private DateTime _startedAt;
        private string? _outputFilePath;

        public RecordSession(Func<IScreenRecorderService> recorderFactory, RecordingSettings settings, string name)
        {
            _recorderFactory = recorderFactory ?? throw new ArgumentNullException(nameof(recorderFactory));
            _settings = settings;
            Name = name;
            StartCommand = new RelayCommand(Start, () => !IsRecording);
            StopCommand = new AsyncRelayCommand(() => StopAsync(null), () => IsRecording);
        }

        /// <summary>会话显示名称（用于区分多个录制任务）。</summary>
        public string Name { get; }

        /// <summary>是否正在录制。</summary>
        public bool IsRecording
        {
            get => _isRecording;
            private set => SetProperty(ref _isRecording, value);
        }

        /// <summary>状态文本。</summary>
        public string StatusText
        {
            get => _statusText;
            set => SetProperty(ref _statusText, value);
        }

        /// <summary>开始录制命令。</summary>
        public ICommand StartCommand { get; }

        /// <summary>停止录制命令（异步，收尾写文件时不卡 UI）。</summary>
        public ICommand StopCommand { get; }

        /// <summary>
        /// 外部调用方（如命名管道回调）使用的可等待停止方法。
        /// 与 StopCommand 不同，此方法返回 Task 可被 await，确保停止完成后才继续后续操作。
        /// </summary>
        /// <param name="saveFilePath">可选：外部指定的保存路径（完整文件路径或目录）；为 null 时使用默认命名规则。</param>
        public Task StopRecordingAsync(string? saveFilePath = null) => StopAsync(saveFilePath);

        private void Start()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(_settings.OutputDirectory))
                {
                    _settings.OutputDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyVideos);
                }
                Directory.CreateDirectory(_settings.OutputDirectory);

                // 每个会话生成独立的文件名，避免并行任务相互覆盖
                string fileName = $"Recording_{Name}_{DateTime.Now:yyyyMMdd_HHmmss}.avi";
                _outputFilePath = Path.Combine(_settings.OutputDirectory, fileName);

                _recorder = _recorderFactory();
                _recorder.Start(_settings, _outputFilePath);
                _startedAt = DateTime.Now;
                IsRecording = true;
                StatusText = $"录制中… ({_settings.FrameRate} FPS)，保存至 {_outputFilePath}";
            }
            catch (Exception ex)
            {
                IsRecording = false;
                StatusText = $"启动失败：{ex.Message}";
            }
        }

        private async Task StopAsync(string? saveFilePath)
        {
            if (_recorder == null)
            {
                return;
            }

            try
            {
                string filePath = await _recorder.StopAsync();

                if (_outputFilePath != null && File.Exists(_outputFilePath))
                {
                    string duration = FormatDuration(DateTime.Now - _startedAt);
                    string defaultName = $"{Name}_{_startedAt:yyyyMMdd_HHmmss}_{duration}.avi";
                    string finalPath;

                    if (!string.IsNullOrWhiteSpace(saveFilePath))
                    {
                        // 外部指定保存路径：目录 → 目录+默认文件名；完整文件路径 → 直接使用
                        if (Directory.Exists(saveFilePath) || string.IsNullOrEmpty(Path.GetExtension(saveFilePath)))
                        {
                            Directory.CreateDirectory(saveFilePath);
                            finalPath = Path.Combine(saveFilePath, defaultName);
                        }
                        else
                        {
                            Directory.CreateDirectory(Path.GetDirectoryName(saveFilePath)!);
                            finalPath = saveFilePath;
                        }
                    }
                    else
                    {
                        // 默认规则：标志位_开始录制时间_录制时长.avi
                        finalPath = Path.Combine(Path.GetDirectoryName(_outputFilePath)!, defaultName);
                    }

                    File.Move(_outputFilePath, finalPath, overwrite: true);
                    filePath = finalPath;
                }

                StatusText = $"已停止，文件已保存：{filePath}";
            }
            catch (Exception ex)
            {
                StatusText = $"停止失败：{ex.Message}";
            }
            finally
            {
                IsRecording = false;
                _recorder.Dispose();
                _recorder = null;
            }
        }

        /// <summary>把时长格式化为文件名友好的 “XhYmZs” 形式。</summary>
        private static string FormatDuration(TimeSpan span)
        {
            var parts = new List<string>();
            if (span.Hours > 0) parts.Add($"{span.Hours}h");
            if (span.Minutes > 0) parts.Add($"{span.Minutes}m");
            if (span.Seconds > 0 || parts.Count == 0) parts.Add($"{span.Seconds}s");
            return string.Join("", parts);
        }

        /// <summary>应用退出等场景下若仍在录制，确保文件正确收尾。</summary>
        public void Dispose() => _recorder?.Dispose();
    }
}
