using System;
using System.Collections.Generic;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace NimePipeServer
{
    // 定义用于管道消息的类
    public class PipeMessage
    {
        // 命令属性，默认值为空字符串
        public string Command { get; set; } = "";
        // 数据属性，可以为空
        public string Data { get; set; }
    }

    // 定义管道服务器服务类，并实现IDisposable接口以便资源清理
    public class PipeServerService : IDisposable
    {
        // 命名管道的名称
        private readonly string _pipeName = "TYM_DataUploadTool_Pipe";
        // 用于取消异步操作的CancellationTokenSource
        private CancellationTokenSource _cts = new CancellationTokenSource();
        // 定义消息接收事件，当接收到消息时触发
        public event Action<PipeMessage> MessageReceived;
        // 定义日志记录事件，当有日志信息时触发
        public event Action<string> Log;

        // 启动服务器的方法
        public void Start()
        {
            // 使用Task.Run启动一个新的异步任务来监听管道连接
            Task.Run(() => ListenAsync(_cts.Token));
        }

        // 异步监听管道连接的方法
        private async Task ListenAsync(CancellationToken token)
        {
            // 持续监听，直到取消令牌被触发
            while (!token.IsCancellationRequested)
            {
                // 创建一个命名管道服务器流，设置管道方向为双向，最大并发连接数为5，
                // 传输模式为按消息传输，使用异步选项
                var server = new NamedPipeServerStream(_pipeName, PipeDirection.InOut, 5, PipeTransmissionMode.Message, PipeOptions.Asynchronous);
                try
                {
                    // 等待客户端连接，此操作是异步的，并可通过取消令牌取消
                    await server.WaitForConnectionAsync(token);

                    // 启动一个新的异步任务来处理客户端连接
                    _ = Task.Run(async () =>
                    {
                        // 使用using语句确保在使用完管道后正确关闭和释放资源
                        using (server)
                        {
                            // 创建一个缓冲区用于读取数据
                            var buffer = new byte[4096];
                            // 异步读取管道中的数据，返回读取的字节数
                            int len = await server.ReadAsync(buffer, 0, buffer.Length, token);
                            // 将读取的字节数组转换为UTF8编码的字符串
                            var msgJson = Encoding.UTF8.GetString(buffer, 0, len);
                            // 将JSON字符串反序列化为PipeMessage对象
                            var msg = JsonSerializer.Deserialize<PipeMessage>(msgJson);

                            // 如果反序列化成功
                            if (msg != null)
                            {
                                // 触发消息接收事件，传递接收到的消息
                                MessageReceived?.Invoke(msg);
                                // 触发日志记录事件，记录接收到的消息内容
                                Log?.Invoke($"收到客户端消息: {msg.Command} {msg.Data}");
                                // 如果接收到的命令是"WakeUp"
                                if (msg.Command == "WakeUp")
                                {
                                }
                            }

                            // 创建回复消息
                            var reply = new PipeMessage { Command = "I will Ack", Data = "OK" };
                            // 将回复消息序列化为JSON字符串，并转换为UTF8编码的字节数组
                            var replyBytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(reply));
                            // 异步将回复消息写入管道
                            await server.WriteAsync(replyBytes, 0, replyBytes.Length, token);
                            // 刷新管道缓冲区，确保数据发送
                            await server.FlushAsync(token);
                        }
                    }, token);
                }
                // 捕获操作取消异常
                catch (OperationCanceledException) { }
                // 捕获其他异常
                catch (Exception ex)
                {
                    // 触发日志记录事件，记录管道异常信息
                    Log?.Invoke($"管道异常: {ex.Message}");
                }
            }
        }

        // 实现IDisposable接口的Dispose方法，用于取消异步操作
        public void Dispose()
        {
            _cts.Cancel();
        }
    }
}
