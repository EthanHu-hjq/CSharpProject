using System;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ProcessB
{
    class Program
    {
        // 命名管道客户端
        private static NamedPipeClientStream _pipeClient;

        // 运行状态标志
        private static bool _isRunning = false;
        private static bool _isPipeConnected = false;
        private static string _pipeName = "ProcessCommunicationPipe";
        private static CancellationTokenSource _cancellationTokenSource;

        /// <summary>
        /// 程序主入口点
        /// </summary>
        static async Task Main(string[] args)
        {
            Console.WriteLine("===============================");
            Console.WriteLine("进程B - 子控制台程序（优化版）");
            Console.WriteLine("===============================");
            Console.WriteLine("可用命令：");
            Console.WriteLine("1. 'ToA:消息内容' - 发送消息到进程A");
            Console.WriteLine("2. 'Exit' - 退出程序");
            Console.WriteLine("===============================");
            Console.WriteLine();

            _cancellationTokenSource = new CancellationTokenSource();

            try
            {
                // 1. 从命令行参数获取管道名称
                if (args.Length > 0)
                {
                    _pipeName = args[0];
                    Console.WriteLine($"从参数获取管道名称: {_pipeName}");
                }
                else
                {
                    Console.WriteLine($"使用默认管道名称: {_pipeName}");
                }

                // 2. 连接到进程A的命名管道服务器
                bool connected = await ConnectToProcessAAsync();
                if (!connected)
                {
                    Console.WriteLine("无法连接到进程A，程序将在5秒后退出...");
                    await Task.Delay(5000);
                    return;
                }

                // 3. 启动消息接收任务
                var receiveTask = Task.Run(() => ReceiveMessagesAsync(_cancellationTokenSource.Token));

                // 4. 主循环处理用户输入
                while (true)
                {
                    Console.Write("B> ");
                    string input = Console.ReadLine()?.Trim();

                    if (string.IsNullOrEmpty(input))
                        continue;

                    // 处理退出命令
                    if (input.Equals("Exit", StringComparison.OrdinalIgnoreCase))
                    {
                        Console.WriteLine("正在退出程序...");
                        _cancellationTokenSource.Cancel();
                        break;
                    }
                    // 处理发送消息到进程A命令
                    else if (input.StartsWith("ToA:", StringComparison.OrdinalIgnoreCase))
                    {
                        if (input.Length > 4)
                        {
                            string message = input.Substring(4);
                            await SendMessageToProcessAAsync(message);
                        }
                        else
                        {
                            Console.WriteLine("消息格式错误，正确格式: ToA:消息内容");
                        }
                    }
                    else
                    {
                        Console.WriteLine("未知命令，可用命令: ToA:消息, Exit");
                    }
                }

                // 等待接收任务完成
                await receiveTask;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"程序发生错误: {ex.Message}");
                Console.WriteLine($"错误详情: {ex.StackTrace}");
            }
            finally
            {
                CleanupResources();
            }

            Console.WriteLine("按任意键退出...");
            Console.ReadKey();
        }

        /// <summary>
        /// 连接到进程A的命名管道服务器
        /// </summary>
        private static async Task<bool> ConnectToProcessAAsync()
        {
            try
            {
                Console.WriteLine($"正在连接到命名管道: {_pipeName}");

                // 创建命名管道客户端
                _pipeClient = new NamedPipeClientStream(
                    ".",
                    _pipeName,
                    PipeDirection.InOut,
                    PipeOptions.Asynchronous);

                Console.WriteLine("正在连接到进程A（最多尝试10次，每次间隔2秒）...");

                // 尝试连接，最多重试10次
                bool connected = false;
                for (int i = 0; i < 10 && !connected; i++)
                {
                    try
                    {
                        Console.WriteLine($"连接尝试 {i + 1}/10...");

                        // 使用CancellationToken实现超时控制
                        var timeoutTokenSource = new CancellationTokenSource(5000); // 5秒超时

                        await _pipeClient.ConnectAsync(timeoutTokenSource.Token);

                        connected = true;
                        _isPipeConnected = true;
                        _isRunning = true;

                        Console.WriteLine("成功连接到进程A");
                        Console.WriteLine("可以开始通信，输入 'ToA:消息内容' 发送消息");
                        return true;
                    }
                    catch (OperationCanceledException)
                    {
                        Console.WriteLine($"连接尝试 {i + 1}/10 超时，等待2秒后重试...");
                        await Task.Delay(2000);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"连接尝试 {i + 1}/10 失败: {ex.Message}");
                        await Task.Delay(2000);
                    }
                }

                if (!connected)
                {
                    Console.WriteLine("无法连接到进程A，请确保：");
                    Console.WriteLine("1. 进程A已启动并输入了'StartB'命令");
                    Console.WriteLine("2. 管道名称正确（当前使用的名称: " + _pipeName + "）");
                    Console.WriteLine("3. 防火墙没有阻止进程间通信");
                    return false;
                }

                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"连接失败: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 发送消息到进程A
        /// </summary>
        private static async Task SendMessageToProcessAAsync(string message)
        {
            if (!_isPipeConnected || _pipeClient == null || !_pipeClient.IsConnected)
            {
                Console.WriteLine("错误：未连接到进程A");
                return;
            }

            if (string.IsNullOrEmpty(message))
            {
                Console.WriteLine("消息不能为空");
                return;
            }

            try
            {
                // 将消息转换为UTF-8编码的字节数组
                byte[] messageBytes = Encoding.UTF8.GetBytes(message);

                // 异步发送消息
                await _pipeClient.WriteAsync(messageBytes, 0, messageBytes.Length);
                await _pipeClient.FlushAsync();

                Console.WriteLine($"[发送到A] {message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"发送消息失败: {ex.Message}");
                _isPipeConnected = false;
            }
        }

        /// <summary>
        /// 异步接收来自进程A的消息
        /// </summary>
        private static async Task ReceiveMessagesAsync(CancellationToken cancellationToken)
        {
            byte[] buffer = new byte[4096];

            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    // 等待管道连接
                    while (!_isPipeConnected || _pipeClient == null || !_pipeClient.IsConnected)
                    {
                        await Task.Delay(100, cancellationToken);
                    }

                    // 异步读取消息
                    int bytesRead = await _pipeClient.ReadAsync(buffer, 0, buffer.Length, cancellationToken);

                    if (bytesRead > 0)
                    {
                        // 将接收到的字节转换为字符串
                        string receivedMessage = Encoding.UTF8.GetString(buffer, 0, bytesRead);

                        // 显示接收到的消息
                        Console.WriteLine($"[从A接收] {receivedMessage}");
                    }
                    else
                    {
                        // 连接已关闭
                        _isPipeConnected = false;
                        Console.WriteLine("与进程A的连接已断开");
                    }
                }
                catch (OperationCanceledException)
                {
                    // 任务被取消，正常退出
                    break;
                }
                catch (Exception ex)
                {
                    if (_isRunning)
                    {
                        Console.WriteLine($"接收消息错误: {ex.Message}");
                    }
                    break;
                }

                // 短暂等待避免CPU占用过高
                await Task.Delay(10, cancellationToken);
            }
        }

        /// <summary>
        /// 清理资源
        /// </summary>
        private static void CleanupResources()
        {
            Console.WriteLine("正在清理资源...");

            _isRunning = false;
            _isPipeConnected = false;

            try
            {
                // 取消所有异步操作
                _cancellationTokenSource?.Cancel();

                // 关闭管道连接
                if (_pipeClient != null)
                {
                    if (_pipeClient.IsConnected)
                    {
                        _pipeClient.Close();
                    }
                    _pipeClient.Dispose();
                    _pipeClient = null;
                    Console.WriteLine("管道连接已关闭");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"关闭管道时出错: {ex.Message}");
            }

            Console.WriteLine("资源清理完成");
        }
    }
}
