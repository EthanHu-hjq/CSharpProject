using System;
using System.Threading;
using System.Threading.Tasks;

namespace ProgramA
{
    /// <summary>
    /// 命名管道客户端同步控制台示例程序（改进版）
    /// 演示如何使用同步方法进行命名管道通信
    /// 修复了断开连接时可能卡死的问题
    /// 添加控制台关闭事件处理，避免直接点击×关闭时的资源泄漏
    /// </summary>
    class Program
    {
        private static SimpleNamedPipeClient _client;
        private static bool _running = true;
        private static ManualResetEventSlim _receivedEvent = new ManualResetEventSlim(false);
        private static string _lastReceivedMessage = string.Empty;
        private static int _messageCount = 0;
        private static CancellationTokenSource _globalCancellationTokenSource = new CancellationTokenSource();
        private static bool _isExiting = false;

        static async Task Main(string[] args)
        {
            Info("=== 命名管道客户端同步示例程序（改进版）===");
            Info("此程序演示如何使用同步方法进行命名管道通信");
            Info("功能包括：初始化、连接、发送消息、接收反馈、断开连接");
            Info("改进：添加了断开连接超时机制，避免卡死");
            Info("改进：添加控制台关闭事件处理，避免资源泄漏");
            Info();

            try
            {
                // 0. 检查服务器进程是否在运行，如果没有则启动服务器
                CheckServerProcess();
                // 1. 初始化客户端并连接到服务器
                InitClient();                

                // 3. 发送消息循环
                Info("\n[3] 进入消息发送模式...");
                Info("输入 'exit' 退出程序");
                Info("输入 'disconnect' 断开连接");
                Info("输入 'status' 查看连接状态");
                Info("输入 'send' 发送测试消息");
                Info("输入 'quickexit' 快速退出（不等待断开连接完成）");
                Info("按Ctrl+C或点击窗口×关闭程序会尝试清理资源");
                Info("----------------------------------------");

                while (_running && !_globalCancellationTokenSource.IsCancellationRequested)
                {
                    if (_isExiting)
                        break;

                    Console.Write("\n请输入命令: ");
                    string input = Console.ReadLine();

                    if (string.IsNullOrWhiteSpace(input))
                        continue;

                    switch (input.ToLower())
                    {
                        case "exit":
                            await ExitProgram();
                            break;

                        case "quickexit":
                            await QuickExitProgram();
                            break;

                        case "disconnect":
                            await Disconnect();
                            break;

                        case "status":
                            ShowStatus();
                            break;

                        case "send":
                            await SendTestMessage();
                            break;

                        case "help":
                            ShowHelp();
                            break;

                        default:
                            // 默认将输入作为消息发送
                            await SendCustomMessage(input);
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                Info($"\n程序发生异常: {ex.Message}");
                Info($"异常类型: {ex.GetType().Name}");
                if (ex.InnerException != null)
                {
                    Info($"内部异常: {ex.InnerException.Message}");
                }
            }
            finally
            {
                // 确保资源被释放
                Info("\n正在清理资源...");
                await CleanupResources();
                Info("程序结束，按任意键退出...");
                Console.ReadKey();
            }
        }

        /// <summary>
        /// 检查服务器进程是否在运行，如果没有则启动服务器
        /// </summary>
        private static void CheckServerProcess()
        {
            Info("[0] 检查服务器进程...");
            if (!ProcessManager.IsProcessRunning("ProgramB"))
            {
                Info("服务器未运行，正在启动服务器...");
                ProcessManager.StartProcess(@"D:\Project\Git\Ethan-Personal-Project\TYM_DataUploadTool\TYM_DataUploadTool\bin\Debug\net8.0-windows\TYM_DataUploadTool.exe");
                Thread.Sleep(2000); // 等待服务器启动
            }
            else
            {
                Info("服务器已在运行");
            }
        }

        /// <summary>
        /// 初始化客户端并订阅事件,连接到服务器
        /// </summary>
        private static void InitClient()
        {
            Info("[1] 初始化命名管道客户端...");
            _client = new SimpleNamedPipeClient("myPipe");
            Info($"  管道名称: {_client.PipeName}");
            Info($"  服务器名称: {_client.ServerName}");
            Info($"  连接超时: {_client.ConnectTimeout}ms");
            Info("初始化完成！");
            SubscribeToEvents();
            // 2. 连接到服务器
            Info("\n正在连接到服务器...");
            try
            {
                _client.Connect(); // 同步连接
                Info("连接成功！");
            }
            catch (Exception ex)
            {
                Info($"连接失败: {ex.Message}");
                Console.ReadKey();
                return;
            }
        }

        /// <summary>
        /// 清理资源
        /// </summary>
        private static async Task CleanupResources()
        {
            try
            {
                Info("正在清理客户端资源...");

                if (_client != null)
                {
                    if (_client.IsConnected)
                    {
                        Info("正在断开命名管道连接...");
                        try
                        {
                            // 尝试优雅断开连接，设置超时
                            var disconnectTask = _client.DisconnectAsync();
                            var timeoutTask = Task.Delay(2000); // 2秒超时

                            var completedTask = await Task.WhenAny(disconnectTask, timeoutTask);

                            if (completedTask == disconnectTask)
                            {
                                Info("命名管道连接已断开");
                            }
                            else
                            {
                                Info("断开连接超时，强制释放资源...");
                            }
                        }
                        catch (Exception ex)
                        {
                            Info($"断开连接时发生异常: {ex.Message}");
                        }
                    }

                    // 最终释放客户端资源
                    _client.Dispose();
                    Info("客户端资源已释放");
                }

                // 释放其他资源
                _receivedEvent?.Dispose();
                _globalCancellationTokenSource?.Dispose();

                Info("所有资源已清理完成");
            }
            catch (Exception ex)
            {
                Info($"清理资源时发生异常: {ex.Message}");
            }
        }

        /// <summary>
        /// 订阅客户端事件
        /// </summary>
        private static void SubscribeToEvents()
        {
            _client.MessageReceived += (sender, e) =>
            {
                _messageCount++;
                _lastReceivedMessage = e.Message;
                Console.ForegroundColor = ConsoleColor.Green;
                Info($"[收到] {e.Message} (时间: {e.Timestamp:HH:mm:ss.fff})");
                Console.ResetColor();
                Thread.Sleep(100);
                //Info($"客户端已收到消息: {e.Message}");
                _client.Send(e.Message); // 同步发送
                _receivedEvent.Set(); // 通知主线程已收到消息
            };

            _client.MessageSent += (sender, e) =>
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Info($"[发送] {e.Message} (时间: {e.Timestamp:HH:mm:ss.fff})");
                Console.ResetColor();
            };

            _client.ConnectionStatusChanged += (sender, e) =>
            {
                Console.ForegroundColor = e.IsConnected ? ConsoleColor.Cyan : ConsoleColor.Gray;
                Info($"[状态] {(e.IsConnected ? "已连接" : "已断开")}: {e.Message}");
                Console.ResetColor();
            };

            _client.ErrorOccurred += (sender, e) =>
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Info($"[错误] {e.Message}");
                if (e.InnerException != null)
                {
                    Info($"      内部异常: {e.InnerException.Message}");
                }
                Console.ResetColor();
            };
        }

        /// <summary>
        /// 发送测试消息
        /// </summary>
        private static async Task SendTestMessage()
        {
            if (!_client.IsConnected)
            {
                Info("未连接到服务器，无法发送消息");
                return;
            }

            try
            {
                // 创建测试消息
                string testMessage = $"测试消息 #{DateTime.Now:HHmmss}";

                Info($"正在发送测试消息: {testMessage}");
                _client.Send(testMessage); // 同步发送

                // 等待服务器回复
                Info("等待服务器回复...");
                _receivedEvent.Reset();

                // 设置超时等待
                bool received = _receivedEvent.Wait(3000); // 等待3秒

                if (received)
                {
                    Info($"收到回复: {_lastReceivedMessage}");
                }
                else
                {
                    Info("等待回复超时，服务器可能未响应");
                }
            }
            catch (Exception ex)
            {
                Info($"发送消息失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 发送自定义消息
        /// </summary>
        private static async Task SendCustomMessage(string message)
        {
            if (!_client.IsConnected)
            {
                Info("未连接到服务器，无法发送消息");
                return;
            }

            try
            {
                Info($"正在发送消息: {message}");
                _client.Send(message); // 同步发送

                // 等待服务器回复
                Info("等待服务器回复...");
                _receivedEvent.Reset();

                // 设置超时等待
                bool received = _receivedEvent.Wait(1000); // 等待3秒

                if (received)
                {
                    Info($"收到回复: {_lastReceivedMessage}");
                }
                else
                {
                    Info("等待回复超时");
                }
            }
            catch (Exception ex)
            {
                Info($"发送消息失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 断开连接（带超时机制）
        /// </summary>
        private static async Task Disconnect()
        {
            if (!_client.IsConnected)
            {
                Info("当前未连接");
                return;
            }

            Info("正在断开连接...");

            try
            {
                // 创建一个带超时的断开连接任务
                var disconnectTask = _client.DisconnectAsync();
                var timeoutTask = Task.Delay(3000); // 3秒超时

                // 等待任意一个任务完成
                var completedTask = await Task.WhenAny(disconnectTask, timeoutTask);

                if (completedTask == disconnectTask)
                {
                    // 正常断开连接完成
                    Info("已断开连接");
                }
                else
                {
                    // 超时，连接可能还在断开中
                    Info("断开连接超时，强制关闭客户端...");

                    // 直接释放客户端资源
                    _client.Dispose();
                    Info("客户端已强制关闭");
                }
            }
            catch (Exception ex)
            {
                Info($"断开连接时发生异常: {ex.Message}");
            }
        }

        /// <summary>
        /// 显示状态信息
        /// </summary>
        private static void ShowStatus()
        {
            Info("\n=== 客户端状态 ===");
            Info($"连接状态: {(_client.IsConnected ? "已连接" : "未连接")}");
            Info($"运行状态: {(_client.IsRunning ? "运行中" : "已停止")}");
            Info($"排队消息数: {_client.QueuedMessages}");
            Info($"已接收消息数: {_messageCount}");
            Info($"最后接收的消息: {(_lastReceivedMessage.Length > 0 ? _lastReceivedMessage : "无")}");
            Info($"管道名称: {_client.PipeName}");
            Info($"连接超时: {_client.ConnectTimeout}ms");
        }

        /// <summary>
        /// 显示帮助信息
        /// </summary>
        private static void ShowHelp()
        {
            Info("\n=== 可用命令 ===");
            Info("exit        - 退出程序（等待断开连接完成）");
            Info("quickexit   - 快速退出（强制断开连接）");
            Info("disconnect  - 断开连接（带超时）");
            Info("status      - 显示状态");
            Info("send        - 发送测试消息");
            Info("help        - 显示此帮助");
            Info("<任意文本>  - 发送自定义消息");
        }

        /// <summary>
        /// 退出程序（正常退出）
        /// </summary>
        private static async Task ExitProgram()
        {
            Info("\n正在退出程序...");
            _running = false;

            if (_client != null && _client.IsConnected)
            {
                Info("正在断开连接...");
                try
                {
                    // 使用带超时的断开连接
                    var disconnectTask = _client.DisconnectAsync();
                    var timeoutTask = Task.Delay(3000); // 3秒超时

                    var completedTask = await Task.WhenAny(disconnectTask, timeoutTask);

                    if (completedTask == disconnectTask)
                    {
                        Info("已断开连接");
                    }
                    else
                    {
                        Info("断开连接超时，强制关闭...");
                        _client.Dispose();
                    }
                }
                catch (Exception ex)
                {
                    Info($"断开连接时发生异常: {ex.Message}");
                }
            }

            Info("程序退出中...");
        }

        /// <summary>
        /// 快速退出程序（强制退出）
        /// </summary>
        private static async Task QuickExitProgram()
        {
            Info("\n正在快速退出程序...");
            _running = false;
            _globalCancellationTokenSource.Cancel();
            _isExiting = true;

            if (_client != null)
            {
                Info("强制关闭客户端...");
                _client.Dispose();
            }

            Info("程序已退出");
        }
    }
}
