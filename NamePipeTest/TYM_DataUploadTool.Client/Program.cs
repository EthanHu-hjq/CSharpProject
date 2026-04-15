using System;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace TYM_DataUploadTool.Client
{
    public class PipeMessage
    {
        public string Command { get; set; } = "";
        public string? Data { get; set; }
    }

    class Program
    {
        // 绝对路径
        private const string ServerExePath = "TYM_DataUploadTool.exe";
        private const string PipeName = "TYM_DataUploadTool_Pipe";

        static async Task Main(string[] args)
        {
            // 1. 检查服务端是否已运行
            if (!IsServerRunning())
            {
                Info("服务端未运行，正在启动...");
                if (!StartServer())
                {
                    Info("服务端启动失败！");
                    return;
                }
                // 等待服务端初始化管道（建议3-5秒）
                await Task.Delay(3000);
            }
            else
            {
                Info("服务端已在运行。");
            }

            // 2. 通过命名管道发送消息
            try
            {
                using (var client = new NamedPipeClientStream(".", PipeName, PipeDirection.InOut, PipeOptions.Asynchronous))
                {
                    Info("正在连接服务端管道...");
                    // 多次重试连接
                    int retry = 5;
                    while (retry-- > 0)
                    {
                        try
                        {
                            await client.ConnectAsync(2000);
                            break;
                        }
                        catch
                        {
                            if (retry == 0) throw;
                            Info("管道连接失败，重试...");
                            await Task.Delay(1000);
                        }
                    }

                    var msg = new PipeMessage
                    {
                        Command = "SetTargetPath",
                        Data = @"D:\test\Target"
                    };
                    var json = JsonSerializer.Serialize(msg);
                    var bytes = Encoding.UTF8.GetBytes(json);

                    await client.WriteAsync(bytes, 0, bytes.Length);
                    await client.FlushAsync();

                    Info($"已发送消息: {msg.Command} {msg.Data}");

                    var buffer = new byte[4096];
                    int len = await client.ReadAsync(buffer, 0, buffer.Length);
                    var replyJson = Encoding.UTF8.GetString(buffer, 0, len);
                    var reply = JsonSerializer.Deserialize<PipeMessage>(replyJson);

                    Info($"收到服务端回复: {reply?.Command} {reply?.Data}");
                }
            }
            catch (TimeoutException)
            {
                Info("连接服务端超时。");
            }
            catch (Exception ex)
            {
                Info($"管道通信异常: {ex.Message}");
            }

            Info("按任意键退出...");
            Console.ReadKey();
        }

        static bool IsServerRunning()
        {
            // 取文件名不带扩展名
            var processName = Path.GetFileNameWithoutExtension(ServerExePath);
            return Process.GetProcessesByName(processName).Any();
        }

        static bool StartServer()
        {
            try
            {
                if (!File.Exists(ServerExePath))
                {
                    Info($"未找到服务端文件: {ServerExePath}");
                    return false;
                }
                Process.Start(new ProcessStartInfo
                {
                    FileName = ServerExePath,
                    UseShellExecute = true
                });
                return true;
            }
            catch (Exception ex)
            {
                Info($"启动服务端异常: {ex.Message}");
                return false;
            }
        }
    }
}
