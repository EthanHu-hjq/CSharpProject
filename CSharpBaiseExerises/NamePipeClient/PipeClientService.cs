using System;
using System.Diagnostics;
using System.IO.Pipes;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TYM_DataUploadTool.Client
{
    public class PipeClientService : IDisposable
    {
        private readonly string _pipeName = "TYM_DataUploadTool_Pipe";
        private NamedPipeClientStream _client;
        private CancellationTokenSource _cts = new CancellationTokenSource(); // 显式指定类型（解决C# 7.3报错）
        private Process _serverProcess;

        public PipeClientService()
        {
            if (!IsServerRunning())
            {
                StartServer();
                WaitForServerToStart();
            }
            // 将PipeDirection.OutIn改为.NET Framework支持的PipeDirection.InOut
            _client = new NamedPipeClientStream(".", _pipeName, PipeDirection.InOut, PipeOptions.Asynchronous);
        }

        private bool IsServerRunning()
        {
            return Process.GetProcessesByName("TYM_DataUploadTool").Length > 0; // 注意：进程名是你的服务器程序名（比如NimePipeServer.exe的进程名是NimePipeServer）
        }

        private void StartServer()
        {
            _serverProcess = new Process
            {
                StartInfo = new ProcessStartInfo // 显式指定类型（解决C# 7.3报错）
                {
                    FileName = @"D:\Project\Git\Ethan-Personal-Project\TYM_DataUploadTool\TYM_DataUploadTool\bin\Debug\net8.0-windows\TYM_DataUploadTool.exe", // 服务器程序名（如果不在同一目录，需写完整路径）
                    UseShellExecute = true,
                    CreateNoWindow = false
                }
            };
            _serverProcess.Start();
        }

        private void WaitForServerToStart()
        {
            int maxRetries = 10;
            int retryCount = 0;
            while (!IsServerRunning() && retryCount < maxRetries)
            {
                Thread.Sleep(1000);
                retryCount++;
            }
        }

        public async Task ConnectAsync()
        {
            await _client.ConnectAsync(_cts.Token);
        }

        public async Task SendMessageAsync(PipeMessage message)
        {
            // 假设你用Newtonsoft.Json（.NET Framework 4.8需先安装NuGet包）
            var msgJson = Newtonsoft.Json.JsonConvert.SerializeObject(message);
            var msgBytes = Encoding.UTF8.GetBytes(msgJson);
            await _client.WriteAsync(msgBytes, 0, msgBytes.Length, _cts.Token);
            await _client.FlushAsync(_cts.Token);

            var buffer = new byte[4096];
            int len = await _client.ReadAsync(buffer, 0, buffer.Length, _cts.Token);
            var replyJson = Encoding.UTF8.GetString(buffer, 0, len);
            var reply = Newtonsoft.Json.JsonConvert.DeserializeObject<PipeMessage>(replyJson);
            Console.WriteLine($"收到服务端回复: {reply.Command} {reply.Data}");
        }

        public void Dispose()
        {
            _cts.Cancel();
            _client.Dispose();
        }
    }

    // 消息类（和服务器端保持一致）
    public class PipeMessage
    {
        public string Command { get; set; } = "";
        public string Data { get; set; }
    }
}