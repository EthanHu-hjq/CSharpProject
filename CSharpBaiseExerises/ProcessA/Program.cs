using System;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ProcessA
{
    class Program
    {
        // 命名管道名称，用于进程间通信
        private const string PipeName = "ProcessCommunicationPipe";

        // 进程B对象和命名管道服务器
        private static Process _processB;
        private static NamedPipeServerStream _pipeServer;

        // 运行状态标志
        private static bool _isRunning = false;
        private static bool _isPipeConnected = false;
        private static CancellationTokenSource _cancellationTokenSource;

        /// <summary>
        /// 程序主入口点
        /// </summary>
        static async Task Main(string[] args)
        {
            Console.WriteLine("===============================");
            Console.WriteLine("进程A - 主控制台程序（优化版）");
            Console.WriteLine("===============================");
            Console.WriteLine("可用命令：");
            Console.WriteLine("1. 'StartB' - 启动进程B");
            Console.WriteLine("2. 'ToB:消息内容' - 发送消息到进程B");
            Console.WriteLine("3. 'Exit' - 退出程序");
            Console.WriteLine("===============================");
            Console.WriteLine();

            _cancellationTokenSource = new CancellationTokenSource();

            try
            {
                // 启动消息接收任务
                var receiveTask = Task.Run(() => ReceiveMessagesAsync(_cancellationTokenSource.Token));

                // 主循环处理用户输入
                while (true)
                {
                    Console.Write("A> ");
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
                    // 处理启动进程B命令
                    else if (input.Equals("StartB", StringComparison.OrdinalIgnoreCase))
                    {
                        if (_processB == null || _processB.HasExited)
                        {
                            await StartProcessBAsync();
                        }
                        else
                        {
                            Console.WriteLine("进程B已经在运行中");
                        }
                    }
                    // 处理发送消息到进程B命令
                    else if (input.StartsWith("ToB:", StringComparison.OrdinalIgnoreCase))
                    {
                        if (input.Length > 4)
                        {
                            string message = input.Substring(4);
                            await SendMessageToProcessBAsync(message);
                        }
                        else
                        {
                            Console.WriteLine("消息格式错误，正确格式: ToB:消息内容");
                        }
                    }
                    else
                    {
                        Console.WriteLine("未知命令，可用命令: StartB, ToB:消息, Exit");
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
        /// 启动进程B并建立命名管道连接
        /// </summary>
        private static async Task StartProcessBAsync()
        {
            try
            {
                Console.WriteLine("正在启动进程B...");

                // 1. 首先启动进程B
                bool processStarted = StartProcessB();
                if (!processStarted)
                {
                    Console.WriteLine("启动进程B失败，请检查错误信息");
                    return;
                }

                // 2. 等待片刻让进程B完全启动
                Console.WriteLine("等待进程B初始化...");
                await Task.Delay(1000);

                // 3. 启动命名管道服务器
                bool pipeStarted = await StartNamedPipeServerAsync();
                if (!pipeStarted)
                {
                    Console.WriteLine("启动管道服务器失败，但进程B可能已启动");
                    return;
                }

                _isRunning = true;
                Console.WriteLine("进程B启动和连接成功完成！");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"启动进程B失败: {ex.Message}");
                Console.WriteLine($"详细错误: {ex.StackTrace}");
                CleanupResources();
            }
        }

        /// <summary>
        /// 启动命名管道服务器
        /// </summary>
        private static async Task<bool> StartNamedPipeServerAsync()
        {
            try
            {
                Console.WriteLine($"正在创建命名管道服务器: {PipeName}");

                // 创建命名管道服务器
                _pipeServer = new NamedPipeServerStream(
                    PipeName,
                    PipeDirection.InOut,
                    NamedPipeServerStream.MaxAllowedServerInstances,
                    PipeTransmissionMode.Message,
                    PipeOptions.Asynchronous);

                Console.WriteLine("等待进程B连接（最长等待15秒）...");

                // 使用CancellationToken实现超时控制
                var cancellationTokenSource = new CancellationTokenSource(15000); // 15秒超时

                try
                {
                    // 异步等待客户端连接
                    await _pipeServer.WaitForConnectionAsync(cancellationTokenSource.Token);

                    _isPipeConnected = true;
                    Console.WriteLine("进程B已成功连接到命名管道");
                    Console.WriteLine("可以开始通信，输入 'ToB:消息内容' 发送消息");
                    return true;
                }
                catch (OperationCanceledException)
                {
                    Console.WriteLine("等待进程B连接超时，进程B可能启动失败或无法连接");
                    Console.WriteLine("请检查：");
                    Console.WriteLine("1. ProcessB.exe是否与ProcessA.exe在同一目录");
                    Console.WriteLine("2. 进程B是否成功启动");
                    Console.WriteLine("3. 防火墙是否阻止了进程间通信");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"启动管道服务器失败: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 启动进程B
        /// </summary>
        private static bool StartProcessB()
        {
            try
            {
                // 获取当前程序所在的目录
                string currentDirectory = AppDomain.CurrentDomain.BaseDirectory;
                Console.WriteLine($"当前目录: {currentDirectory}");

                // 进程B的可执行文件路径
                string processBPath = Path.Combine(currentDirectory, "ProcessB.exe");
                Console.WriteLine($"查找ProcessB.exe路径: {processBPath}");

                if (!File.Exists(processBPath))
                {
                    Console.WriteLine($"错误：找不到进程B可执行文件: {processBPath}");
                    Console.WriteLine("请确保：");
                    Console.WriteLine("1. 已成功编译ProcessB项目");
                    Console.WriteLine("2. ProcessB.exe与ProcessA.exe在同一目录");
                    Console.WriteLine("3. ProcessB.exe文件名拼写正确");
                    return false;
                }

                Console.WriteLine($"找到ProcessB.exe，文件大小: {new FileInfo(processBPath).Length} 字节");

                // 创建进程启动信息
                var startInfo = new ProcessStartInfo
                {
                    FileName = processBPath,
                    UseShellExecute = true,           // 使用系统shell执行，避免权限问题
                    CreateNoWindow = false,           // 创建控制台窗口
                    Arguments = PipeName,              // 将管道名称作为参数传递给进程B
                    WorkingDirectory = currentDirectory,
                    Verb = "runas"                     // 如果需要，以管理员权限运行
                };

                // 启动进程B
                Console.WriteLine("正在启动进程B...");
                _processB = Process.Start(startInfo);

                if (_processB != null)
                {
                    Console.WriteLine($"成功启动进程B，进程ID: {_processB.Id}");
                    Console.WriteLine($"进程B启动时间: {_processB.StartTime}");

                    // 监控进程B退出事件
                    _processB.EnableRaisingEvents = true;
                    _processB.Exited += (sender, e) =>
                    {
                        Console.WriteLine("进程B已退出");
                        _isRunning = false;
                        _isPipeConnected = false;
                    };

                    return true;
                }
                else
                {
                    Console.WriteLine("错误：Process.Start返回null，进程B启动失败");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"启动进程B失败: {ex.Message}");
                Console.WriteLine($"详细错误: {ex.StackTrace}");
                return false;
            }
        }

        /// <summary>
        /// 发送消息到进程B
        /// </summary>
        private static async Task SendMessageToProcessBAsync(string message)
        {
            if (!_isPipeConnected || _pipeServer == null || !_pipeServer.IsConnected)
            {
                Console.WriteLine("错误：未连接到进程B，请先输入'StartB'启动进程B");
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
                await _pipeServer.WriteAsync(messageBytes, 0, messageBytes.Length);
                await _pipeServer.FlushAsync();

                Console.WriteLine($"[发送到B] {message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"发送消息失败: {ex.Message}");
                _isPipeConnected = false;
            }
        }

        /// <summary>
        /// 异步接收来自进程B的消息
        /// </summary>
        private static async Task ReceiveMessagesAsync(CancellationToken cancellationToken)
        {
            byte[] buffer = new byte[4096];  // 4KB缓冲区

            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    // 等待管道连接
                    while (!_isPipeConnected || _pipeServer == null || !_pipeServer.IsConnected)
                    {
                        await Task.Delay(100, cancellationToken);  // 等待100ms再检查
                    }

                    // 异步读取消息
                    int bytesRead = await _pipeServer.ReadAsync(buffer, 0, buffer.Length, cancellationToken);

                    if (bytesRead > 0)
                    {
                        // 将接收到的字节转换为字符串
                        string receivedMessage = Encoding.UTF8.GetString(buffer, 0, bytesRead);

                        // 显示接收到的消息
                        Console.WriteLine($"[从B接收] {receivedMessage}");
                    }
                    else
                    {
                        // 连接已关闭
                        _isPipeConnected = false;
                        Console.WriteLine("与进程B的连接已断开");
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
                if (_pipeServer != null)
                {
                    if (_pipeServer.IsConnected)
                    {
                        _pipeServer.Disconnect();
                    }
                    _pipeServer.Close();
                    _pipeServer.Dispose();
                    _pipeServer = null;
                    Console.WriteLine("管道连接已关闭");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"关闭管道时出错: {ex.Message}");
            }

            try
            {
                // 检查进程B是否仍在运行
                if (_processB != null && !_processB.HasExited)
                {
                    Console.WriteLine("正在停止进程B...");

                    // 首先尝试正常关闭
                    _processB.CloseMainWindow();

                    // 等待2秒让进程正常退出
                    if (!_processB.WaitForExit(2000))
                    {
                        // 如果超时，强制终止进程
                        _processB.Kill();
                        Console.WriteLine("进程B已被强制终止");
                    }
                    else
                    {
                        Console.WriteLine("进程B已正常退出");
                    }

                    _processB.Close();
                    _processB.Dispose();
                    _processB = null;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"终止进程B时出错: {ex.Message}");
            }

            Console.WriteLine("资源清理完成");
        }
    }
}
