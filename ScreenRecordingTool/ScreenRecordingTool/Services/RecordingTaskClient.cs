using System.IO.Pipes;
using System.Text;

namespace ScreenRecordingTool.Services
{
    /// <summary>
    /// 静态录制任务客户端：通过命名管道向录屏程序发送启动/停止指令。
    /// 供外部进程使用（可将本文件直接复制到外部项目中）。
    /// 用法：
    ///   RecordingTaskClient.StartRecording("TaskA");            // 启动任务
    ///   RecordingTaskClient.StopRecording(@"D:\Videos\a.avi");  // 停止并保存到指定路径
    /// </summary>
    public static class RecordingTaskClient
    {
        private const string PipeName = "ScreenRecordingTool.Pipe";
        private const int ConnectTimeoutMs = 3000;

        private static readonly object _lock = new();

        /// <summary>记录最近一次启动的任务标志位，供无标志位的 StopRecording 重载使用。</summary>
        private static string? _lastFlag;

        /// <summary>
        /// 启动一个录制任务。
        /// </summary>
        /// <param name="flag">任务标志位，用于区分不同录制任务。</param>
        public static void StartRecording(string flag)
        {
            ValidateFlag(flag);

            Send($"START {flag}");
            lock (_lock)
            {
                _lastFlag = flag;
            }
        }

        /// <summary>
        /// 停止最近一次启动的录制任务，并将录屏文件保存到指定路径。
        /// </summary>
        /// <param name="saveFilePath">
        /// 录屏文件保存路径：可为完整文件路径（如 D:\Videos\a.avi），
        /// 也可为目录路径（此时使用默认命名规则 标志位_开始时间_时长.avi）。
        /// </param>
        public static void StopRecording(string saveFilePath)
        {
            string? flag;
            lock (_lock)
            {
                flag = _lastFlag;
                _lastFlag = null;
            }

            if (flag == null)
            {
                throw new InvalidOperationException("没有可停止的录制任务，请先调用 StartRecording。");
            }

            StopRecording(flag, saveFilePath);
        }

        /// <summary>
        /// 停止指定标志位的录制任务，并将录屏文件保存到指定路径。
        /// </summary>
        public static void StopRecording(string flag, string saveFilePath)
        {
            ValidateFlag(flag);
            if (string.IsNullOrWhiteSpace(saveFilePath))
            {
                throw new ArgumentException("保存路径不能为空。", nameof(saveFilePath));
            }

            // 协议：STOP 标志位|保存路径（'|' 为分隔符）
            Send($"STOP {flag}|{saveFilePath}");
        }

        private static void ValidateFlag(string flag)
        {
            if (string.IsNullOrWhiteSpace(flag))
            {
                throw new ArgumentException("任务标志位不能为空。", nameof(flag));
            }
            if (flag.Contains('|') || flag.Contains(' '))
            {
                throw new ArgumentException("任务标志位不能包含 '|' 或空格字符。", nameof(flag));
            }
        }

        /// <summary>连接命名管道并发送一条完整消息（对应服务端 Message 模式的一次读取）。</summary>
        private static void Send(string message)
        {
            using var pipe = new NamedPipeClientStream(".", PipeName, PipeDirection.Out);
            pipe.Connect(ConnectTimeoutMs);

            byte[] data = Encoding.UTF8.GetBytes(message);
            pipe.Write(data, 0, data.Length);
            pipe.Flush();
            pipe.WaitForPipeDrain(); // 确保服务端读完再断开
        }
    }
}
