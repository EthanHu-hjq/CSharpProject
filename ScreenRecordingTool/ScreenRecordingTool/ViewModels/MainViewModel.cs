using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using ScreenRecordingTool.Models;
using ScreenRecordingTool.Mvvm;
using ScreenRecordingTool.Services;

namespace ScreenRecordingTool.ViewModels
{
    /// <summary>
    /// 主窗口 ViewModel：承载多个相互独立的录制会话（支持并行），
    /// 同时响应界面按钮与命名管道外部进程的启动/停止指令。
    /// </summary>
    public class MainViewModel : ViewModelBase, IDisposable
    {
        private readonly Func<IScreenRecorderService> _recorderFactory;
        private readonly NamedPipeCommandServer _pipeServer;

        public MainViewModel() : this(() => new GdiScreenRecorderService())
        {
        }

        /// <summary>支持注入录屏服务工厂，便于测试与替换。</summary>
        public MainViewModel(Func<IScreenRecorderService> recorderFactory)
        {
            _recorderFactory = recorderFactory;
            Settings = new RecordingSettings();

            // 初始为空：界面仅显示“新增录制任务”按钮，点击后才出现一组开始/停止按钮
            Sessions = new ObservableCollection<RecordSession>();

            AddSessionCommand = new RelayCommand(AddSession);

            // 启动命名管道服务端，接收外部进程命令。
            // 管道回调在后台线程触发，操作 ObservableCollection/命令必须切回 UI 线程
            _pipeServer = new NamedPipeCommandServer(
                onStart: flag => RunOnUiThreadAsync(() => { StartExternal(flag); return Task.CompletedTask; }),
                onStop: flag => RunOnUiThreadAsync(() => StopExternalAsync(flag)));
        }

        /// <summary>共享的录制配置。</summary>
        public RecordingSettings Settings { get; }

        /// <summary>当前所有录制会话（并行运行，含外部启动的任务）。</summary>
        public ObservableCollection<RecordSession> Sessions { get; }

        /// <summary>新增一个录制任务会话（界面按钮）。</summary>
        public ICommand AddSessionCommand { get; }

        /// <summary>外部进程通过命名管道启动一个录制任务，flag 用于区分任务。</summary>
        private void StartExternal(string flag)
        {
            // 仅阻止同名任务正在录制时的重复启动；已完成的同名任务可复用标志位重新开始
            if (Sessions.Any(s => s.Name == flag && s.IsRecording))
            {
                return;
            }
            Sessions.Add(new RecordSession(_recorderFactory, Settings, flag));
            Sessions.First(s => s.Name == flag).StartCommand.Execute(null);
        }

        /// <summary>
        /// 外部进程停止与 flag 匹配的、正在录制的任务，完成后从列表移除以释放标志位。
        /// 参数格式："标志位" 或 "标志位|保存路径"（保存路径可为目录或完整文件路径）。
        /// </summary>
        private async Task StopExternalAsync(string arg)
        {
            // 解析 "标志位|保存路径" 协议
            int sep = arg.IndexOf('|');
            string flag = sep < 0 ? arg : arg[..sep].Trim();
            string? savePath = sep < 0 ? null : arg[(sep + 1)..].Trim();
            if (string.IsNullOrEmpty(savePath))
            {
                savePath = null;
            }

            var target = Sessions.FirstOrDefault(s => s.Name == flag && s.IsRecording);
            if (target == null)
            {
                return;
            }

            // 先 await 停止完成（写 AVI 索引、重命名文件），再移除和释放，
            // 避免 fire-and-forget 导致 StopAsync 被并发调用两次引发 NullReferenceException
            try
            {
                await target.StopRecordingAsync(savePath);
            }
            catch
            {
                // 停止失败仍需移除会话以释放标志位
            }

            Sessions.Remove(target);
            target.Dispose();
        }

        private void AddSession()
        {
            AddSession($"任务{Sessions.Count + 1}");
        }

        private void AddSession(string name)
        {
            Sessions.Add(new RecordSession(_recorderFactory, Settings, name));
        }

        private static void RunOnUiThread(Action action)
        {
            var app = System.Windows.Application.Current;
            if (app?.Dispatcher.CheckAccess() == true)
            {
                action();
            }
            else
            {
                app?.Dispatcher.Invoke(action);
            }
        }

        /// <summary>将异步操作调度到 UI 线程执行并返回 Task，供管道回调 await 使用。</summary>
        private static async Task RunOnUiThreadAsync(Func<Task> asyncAction)
        {
            var app = System.Windows.Application.Current;
            if (app?.Dispatcher.CheckAccess() == true)
            {
                await asyncAction().ConfigureAwait(false);
            }
            else
            {
                await app?.Dispatcher.Invoke(asyncAction)!;
            }
        }

        /// <summary>应用退出时调用，停止管道服务端并确保所有录制文件正确收尾。</summary>
        public void Dispose()
        {
            _pipeServer.Dispose();
            foreach (var session in Sessions)
            {
                session.Dispose();
            }
        }
    }
}
