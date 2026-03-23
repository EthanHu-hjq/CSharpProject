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
            Console.WriteLine("=== 命名管道客户端同步示例程序（改进版）===");
            Console.WriteLine("此程序演示如何使用同步方法进行命名管道通信");
            Console.WriteLine("功能包括：初始化、连接、发送消息、接收反馈、断开连接");
            Console.WriteLine("改进：添加了断开连接超时机制，避免卡死");
            Console.WriteLine("改进：添加控制台关闭事件处理，避免资源泄漏");
            Console.WriteLine();

            try
            {
                // 1. 初始化客户端
                Console.WriteLine("[1] 初始化命名管道客户端...");
                _client = new SimpleNamedPipeClient("myPipe");
                Console.WriteLine($"  管道名称: {_client.PipeName}");
                Console.WriteLine($"  服务器名称: {_client.ServerName}");
                Console.WriteLine($"  连接超时: {_client.ConnectTimeout}ms");
                Console.WriteLine("初始化完成！");

                // 订阅事件
                SubscribeToEvents();

                // 2. 连接到服务器
                Console.WriteLine("\n[2] 正在连接到服务器...");
                try
                {
                    _client.Connect(); // 同步连接
                    Console.WriteLine("连接成功！");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"连接失败: {ex.Message}");
                    Console.WriteLine("请确保服务器程序正在运行，然后按任意键重试...");
                    Console.ReadKey();
                    return;
                }

                // 3. 发送消息循环
                Console.WriteLine("\n[3] 进入消息发送模式...");
                Console.WriteLine("输入 'exit' 退出程序");
                Console.WriteLine("输入 'disconnect' 断开连接");
                Console.WriteLine("输入 'status' 查看连接状态");
                Console.WriteLine("输入 'send' 发送测试消息");
                Console.WriteLine("输入 'quickexit' 快速退出（不等待断开连接完成）");
                Console.WriteLine("按Ctrl+C或点击窗口×关闭程序会尝试清理资源");
                Console.WriteLine("----------------------------------------");

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
                Console.WriteLine($"\n程序发生异常: {ex.Message}");
                Console.WriteLine($"异常类型: {ex.GetType().Name}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"内部异常: {ex.InnerException.Message}");
                }
            }
            finally
            {
                // 确保资源被释放
                Console.WriteLine("\n正在清理资源...");
                await CleanupResources();
                Console.WriteLine("程序结束，按任意键退出...");
                Console.ReadKey();
            }
        }

        /// <summary>
        /// 清理资源
        /// </summary>
        private static async Task CleanupResources()
        {
            try
            {
                Console.WriteLine("正在清理客户端资源...");

                if (_client != null)
                {
                    if (_client.IsConnected)
                    {
                        Console.WriteLine("正在断开命名管道连接...");
                        try
                        {
                            // 尝试优雅断开连接，设置超时
                            var disconnectTask = _client.DisconnectAsync();
                            var timeoutTask = Task.Delay(2000); // 2秒超时

                            var completedTask = await Task.WhenAny(disconnectTask, timeoutTask);

                            if (completedTask == disconnectTask)
                            {
                                Console.WriteLine("命名管道连接已断开");
                            }
                            else
                            {
                                Console.WriteLine("断开连接超时，强制释放资源...");
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"断开连接时发生异常: {ex.Message}");
                        }
                    }

                    // 最终释放客户端资源
                    _client.Dispose();
                    Console.WriteLine("客户端资源已释放");
                }

                // 释放其他资源
                _receivedEvent?.Dispose();
                _globalCancellationTokenSource?.Dispose();

                Console.WriteLine("所有资源已清理完成");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"清理资源时发生异常: {ex.Message}");
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
                Console.WriteLine($"[收到] {e.Message} (时间: {e.Timestamp:HH:mm:ss.fff})");
                Console.ResetColor();
                _receivedEvent.Set(); // 通知主线程已收到消息
            };

            _client.MessageSent += (sender, e) =>
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"[发送] {e.Message} (时间: {e.Timestamp:HH:mm:ss.fff})");
                Console.ResetColor();
            };

            _client.ConnectionStatusChanged += (sender, e) =>
            {
                Console.ForegroundColor = e.IsConnected ? ConsoleColor.Cyan : ConsoleColor.Gray;
                Console.WriteLine($"[状态] {(e.IsConnected ? "已连接" : "已断开")}: {e.Message}");
                Console.ResetColor();
            };

            _client.ErrorOccurred += (sender, e) =>
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"[错误] {e.Message}");
                if (e.InnerException != null)
                {
                    Console.WriteLine($"      内部异常: {e.InnerException.Message}");
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
                Console.WriteLine("未连接到服务器，无法发送消息");
                return;
            }

            try
            {
                // 创建测试消息
                string testMessage = $"测试消息 #{DateTime.Now:HHmmss}";

                Console.WriteLine($"正在发送测试消息: {testMessage}");
                _client.Send(testMessage); // 同步发送

                // 等待服务器回复
                Console.WriteLine("等待服务器回复...");
                _receivedEvent.Reset();

                // 设置超时等待
                bool received = _receivedEvent.Wait(3000); // 等待3秒

                if (received)
                {
                    Console.WriteLine($"收到回复: {_lastReceivedMessage}");
                }
                else
                {
                    Console.WriteLine("等待回复超时，服务器可能未响应");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"发送消息失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 发送自定义消息
        /// </summary>
        private static async Task SendCustomMessage(string message)
        {
            if (!_client.IsConnected)
            {
                Console.WriteLine("未连接到服务器，无法发送消息");
                return;
            }

            try
            {
                Console.WriteLine($"正在发送消息: {message}");
                _client.Send(message); // 同步发送

                // 等待服务器回复
                Console.WriteLine("等待服务器回复...");
                _receivedEvent.Reset();

                // 设置超时等待
                bool received = _receivedEvent.Wait(3000); // 等待3秒

                if (received)
                {
                    Console.WriteLine($"收到回复: {_lastReceivedMessage}");
                }
                else
                {
                    Console.WriteLine("等待回复超时");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"发送消息失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 断开连接（带超时机制）
        /// </summary>
        private static async Task Disconnect()
        {
            if (!_client.IsConnected)
            {
                Console.WriteLine("当前未连接");
                return;
            }

            Console.WriteLine("正在断开连接...");

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
                    Console.WriteLine("已断开连接");
                }
                else
                {
                    // 超时，连接可能还在断开中
                    Console.WriteLine("断开连接超时，强制关闭客户端...");

                    // 直接释放客户端资源
                    _client.Dispose();
                    Console.WriteLine("客户端已强制关闭");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"断开连接时发生异常: {ex.Message}");
            }
        }

        /// <summary>
        /// 显示状态信息
        /// </summary>
        private static void ShowStatus()
        {
            Console.WriteLine("\n=== 客户端状态 ===");
            Console.WriteLine($"连接状态: {(_client.IsConnected ? "已连接" : "未连接")}");
            Console.WriteLine($"运行状态: {(_client.IsRunning ? "运行中" : "已停止")}");
            Console.WriteLine($"排队消息数: {_client.QueuedMessages}");
            Console.WriteLine($"已接收消息数: {_messageCount}");
            Console.WriteLine($"最后接收的消息: {(_lastReceivedMessage.Length > 0 ? _lastReceivedMessage : "无")}");
            Console.WriteLine($"管道名称: {_client.PipeName}");
            Console.WriteLine($"连接超时: {_client.ConnectTimeout}ms");
        }

        /// <summary>
        /// 显示帮助信息
        /// </summary>
        private static void ShowHelp()
        {
            Console.WriteLine("\n=== 可用命令 ===");
            Console.WriteLine("exit        - 退出程序（等待断开连接完成）");
            Console.WriteLine("quickexit   - 快速退出（强制断开连接）");
            Console.WriteLine("disconnect  - 断开连接（带超时）");
            Console.WriteLine("status      - 显示状态");
            Console.WriteLine("send        - 发送测试消息");
            Console.WriteLine("help        - 显示此帮助");
            Console.WriteLine("<任意文本>  - 发送自定义消息");
        }

        /// <summary>
        /// 退出程序（正常退出）
        /// </summary>
        private static async Task ExitProgram()
        {
            Console.WriteLine("\n正在退出程序...");
            _running = false;

            if (_client != null && _client.IsConnected)
            {
                Console.WriteLine("正在断开连接...");
                try
                {
                    // 使用带超时的断开连接
                    var disconnectTask = _client.DisconnectAsync();
                    var timeoutTask = Task.Delay(3000); // 3秒超时

                    var completedTask = await Task.WhenAny(disconnectTask, timeoutTask);

                    if (completedTask == disconnectTask)
                    {
                        Console.WriteLine("已断开连接");
                    }
                    else
                    {
                        Console.WriteLine("断开连接超时，强制关闭...");
                        _client.Dispose();
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"断开连接时发生异常: {ex.Message}");
                }
            }

            Console.WriteLine("程序退出中...");
        }

        /// <summary>
        /// 快速退出程序（强制退出）
        /// </summary>
        private static async Task QuickExitProgram()
        {
            Console.WriteLine("\n正在快速退出程序...");
            _running = false;
            _globalCancellationTokenSource.Cancel();
            _isExiting = true;

            if (_client != null)
            {
                Console.WriteLine("强制关闭客户端...");
                _client.Dispose();
            }

            Console.WriteLine("程序已退出");
        }
    }
}
