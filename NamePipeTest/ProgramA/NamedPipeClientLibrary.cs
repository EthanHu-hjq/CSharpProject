using System;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Concurrent;

namespace ProgramA
{
    /// <summary>
    /// 命名管道消息事件参数
    /// </summary>
    public class PipeMessageEventArgs : EventArgs
    {
        public string Message { get; }
        public DateTime Timestamp { get; }

        public PipeMessageEventArgs(string message)
        {
            Message = message;
            Timestamp = DateTime.Now;
        }
    }

    /// <summary>
    /// 连接状态事件参数
    /// </summary>
    public class PipeConnectionEventArgs : EventArgs
    {
        public bool IsConnected { get; }
        public string Message { get; }

        public PipeConnectionEventArgs(bool isConnected, string message = "")
        {
            IsConnected = isConnected;
            Message = message;
        }
    }

    /// <summary>
    /// 简单的命名管道客户端
    /// 提供基本的命名管道通信功能，包含连接、发送、接收和资源管理
    /// </summary>
    public class SimpleNamedPipeClient : IDisposable
    {
        // 事件
        public event EventHandler<PipeMessageEventArgs> MessageReceived;
        public event EventHandler<PipeMessageEventArgs> MessageSent;
        public event EventHandler<PipeConnectionEventArgs> ConnectionStatusChanged;
        public event EventHandler<Exception> ErrorOccurred;

        // 配置属性
        public string PipeName { get; private set; }
        public string ServerName { get; private set; } = ".";
        public int ConnectTimeout { get; set; } = 5000; // 连接超时时间（毫秒）

        // 状态属性
        public bool IsConnected => _pipeClient != null && _pipeClient.IsConnected;
        public bool IsRunning => _isRunning;
        public int QueuedMessages => _messageQueue.Count;

        // 私有字段
        private NamedPipeClientStream _pipeClient;
        private readonly ConcurrentQueue<string> _messageQueue = new ConcurrentQueue<string>();
        private readonly SemaphoreSlim _messageAvailable = new SemaphoreSlim(0, int.MaxValue);
        private CancellationTokenSource _cancellationTokenSource;
        private bool _isRunning = false;
        private Task _receiveTask;
        private Task _sendTask;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="pipeName">管道名称</param>
        /// <param name="serverName">服务器名称，默认为本地计算机"."</param>
        public SimpleNamedPipeClient(string pipeName, string serverName = ".")
        {
            if (string.IsNullOrWhiteSpace(pipeName))
                throw new ArgumentException("管道名称不能为空", nameof(pipeName));

            PipeName = pipeName;
            ServerName = serverName ?? ".";
        }

        /// <summary>
        /// 连接到命名管道服务器
        /// </summary>
        public async Task ConnectAsync()
        {
            if (_isRunning)
                throw new InvalidOperationException("客户端已经在运行中");

            if (_pipeClient != null && _pipeClient.IsConnected)
                throw new InvalidOperationException("客户端已连接，请先断开连接");

            try
            {
                _isRunning = true;
                _cancellationTokenSource = new CancellationTokenSource();

                // 创建管道客户端
                _pipeClient = new NamedPipeClientStream(ServerName, PipeName, PipeDirection.InOut, PipeOptions.Asynchronous);

                // 连接服务器
                OnConnectionStatusChanged(false, $"正在连接到管道: {PipeName}");
                await _pipeClient.ConnectAsync(ConnectTimeout, _cancellationTokenSource.Token);

                OnConnectionStatusChanged(true, "已连接到服务器");

                // 启动接收和发送任务
                StartTasks();
            }
            catch (Exception ex)
            {
                _isRunning = false;
                _cancellationTokenSource?.Dispose();
                _cancellationTokenSource = null;

                OnErrorOccurred(new Exception($"连接失败: {ex.Message}", ex));
                throw;
            }
        }

        /// <summary>
        /// 连接到命名管道服务器（同步版本）
        /// </summary>
        public void Connect()
        {
            if (_isRunning)
                throw new InvalidOperationException("客户端已经在运行中");

            if (_pipeClient != null && _pipeClient.IsConnected)
                throw new InvalidOperationException("客户端已连接，请先断开连接");

            try
            {
                _isRunning = true;
                _cancellationTokenSource = new CancellationTokenSource();

                // 创建管道客户端
                _pipeClient = new NamedPipeClientStream(ServerName, PipeName, PipeDirection.InOut, PipeOptions.Asynchronous);

                // 连接服务器
                OnConnectionStatusChanged(false, $"正在连接到管道: {PipeName}");
                _pipeClient.Connect(ConnectTimeout);

                OnConnectionStatusChanged(true, "已连接到服务器");

                // 启动接收和发送任务
                StartTasks();
            }
            catch (Exception ex)
            {
                _isRunning = false;
                _cancellationTokenSource?.Dispose();
                _cancellationTokenSource = null;

                OnErrorOccurred(new Exception($"连接失败: {ex.Message}", ex));
                throw;
            }
        }

        /// <summary>
        /// 断开连接
        /// </summary>
        public async Task DisconnectAsync()
        {
            if (!_isRunning)
                return;

            _isRunning = false;
            _cancellationTokenSource?.Cancel();

            try
            {
                // 等待任务完成
                if (_receiveTask != null)
                {
                    await _receiveTask;
                }

                if (_sendTask != null)
                {
                    await _sendTask;
                }
            }
            catch (OperationCanceledException)
            {
                // 任务被取消是预期的
            }
            catch (Exception ex)
            {
                OnErrorOccurred(ex);
            }
            finally
            {
                // 清理资源
                _cancellationTokenSource?.Dispose();
                _cancellationTokenSource = null;

                // 修复：ConcurrentQueue没有Clear方法，改为循环出队
                while (_messageQueue.TryDequeue(out _)) { }

                // 关闭管道
                try
                {
                    _pipeClient?.Close();
                    _pipeClient?.Dispose();
                }
                catch { }

                _pipeClient = null;
                OnConnectionStatusChanged(false, "连接已断开");
            }
        }

        /// <summary>
        /// 发送消息
        /// </summary>
        /// <param name="message">要发送的消息</param>
        public async Task SendAsync(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
                return;

            if (!IsConnected)
                throw new InvalidOperationException("未连接到服务器，无法发送消息");

            try
            {
                byte[] messageBytes = Encoding.UTF8.GetBytes(message);
                await _pipeClient.WriteAsync(messageBytes, 0, messageBytes.Length, _cancellationTokenSource?.Token ?? CancellationToken.None);
                OnMessageSent(message);
            }
            catch (Exception ex)
            {
                OnErrorOccurred(new Exception($"发送消息失败: {ex.Message}", ex));
                throw;
            }
        }

        /// <summary>
        /// 发送消息（同步版本）
        /// </summary>
        /// <param name="message">要发送的消息</param>
        public void Send(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
                return;

            if (!IsConnected)
                throw new InvalidOperationException("未连接到服务器，无法发送消息");

            try
            {
                byte[] messageBytes = Encoding.UTF8.GetBytes(message);
                _pipeClient.Write(messageBytes, 0, messageBytes.Length);
            }
            catch (Exception ex)
            {
                OnErrorOccurred(new Exception($"发送消息失败: {ex.Message}", ex));
                throw;
            }
        }

        /// <summary>
        /// 异步接收消息
        /// </summary>
        /// <returns>接收到的消息</returns>
        public async Task<string> ReceiveAsync()
        {
            if (!IsConnected)
                throw new InvalidOperationException("未连接到服务器，无法接收消息");

            try
            {
                byte[] buffer = new byte[4096];
                int bytesRead = await _pipeClient.ReadAsync(buffer, 0, buffer.Length, _cancellationTokenSource?.Token ?? CancellationToken.None);

                if (bytesRead > 0)
                {
                    string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    OnMessageReceived(message);
                    return message;
                }

                return null;
            }
            catch (Exception ex)
            {
                if (!(ex is OperationCanceledException))
                {
                    OnErrorOccurred(new Exception($"接收消息失败: {ex.Message}", ex));
                }
                throw;
            }
        }

        /// <summary>
        /// 接收消息（同步版本）
        /// </summary>
        /// <returns>接收到的消息</returns>
        public string Receive()
        {
            if (!IsConnected)
                throw new InvalidOperationException("未连接到服务器，无法接收消息");

            try
            {
                byte[] buffer = new byte[4096];
                int bytesRead = _pipeClient.Read(buffer, 0, buffer.Length);

                if (bytesRead > 0)
                {
                    string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    OnMessageReceived(message);
                    return message;
                }

                return null;
            }
            catch (Exception ex)
            {
                OnErrorOccurred(new Exception($"接收消息失败: {ex.Message}", ex));
                throw;
            }
        }

        /// <summary>
        /// 启动后台任务
        /// </summary>
        private void StartTasks()
        {
            var cancellationToken = _cancellationTokenSource.Token;

            // 启动接收任务
            _receiveTask = Task.Run(async () =>
            {
                await ReceiveMessagesAsync(cancellationToken);
            }, cancellationToken);

            // 启动发送任务
            _sendTask = Task.Run(async () =>
            {
                await SendMessagesAsync(cancellationToken);
            }, cancellationToken);
        }

        /// <summary>
        /// 持续接收消息
        /// </summary>
        private async Task ReceiveMessagesAsync(CancellationToken cancellationToken)
        {
            try
            {
                while (!cancellationToken.IsCancellationRequested && _pipeClient != null && _pipeClient.IsConnected)
                {
                    byte[] buffer = new byte[4096];
                    int bytesRead = 0;

                    try
                    {
                        bytesRead = await _pipeClient.ReadAsync(buffer, 0, buffer.Length, cancellationToken);

                        if (bytesRead > 0)
                        {
                            string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                            OnMessageReceived(message);
                        }
                        else if (bytesRead == 0)
                        {
                            // 服务器正常断开
                            OnConnectionStatusChanged(false, "服务器断开连接");
                            break;
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                    catch (Exception ex) when (ex is IOException || ex is ObjectDisposedException)
                    {
                        OnErrorOccurred(new Exception($"接收消息时发生IO异常: {ex.Message}", ex));
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                if (!(ex is OperationCanceledException))
                {
                    OnErrorOccurred(new Exception($"接收消息任务异常: {ex.Message}", ex));
                }
            }
        }

        /// <summary>
        /// 持续发送消息
        /// </summary>
        private async Task SendMessagesAsync(CancellationToken cancellationToken)
        {
            try
            {
                while (!cancellationToken.IsCancellationRequested && _pipeClient != null && _pipeClient.IsConnected)
                {
                    try
                    {
                        // 等待消息
                        await _messageAvailable.WaitAsync(cancellationToken);

                        if (cancellationToken.IsCancellationRequested)
                            break;

                        // 从队列获取消息
                        if (_messageQueue.TryDequeue(out var message) && !string.IsNullOrWhiteSpace(message))
                        {
                            byte[] messageBytes = Encoding.UTF8.GetBytes(message);
                            await _pipeClient.WriteAsync(messageBytes, 0, messageBytes.Length, cancellationToken);
                            OnMessageSent(message);
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                    catch (Exception ex) when (ex is IOException || ex is ObjectDisposedException)
                    {
                        OnErrorOccurred(new Exception($"发送消息时发生IO异常: {ex.Message}", ex));
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                if (!(ex is OperationCanceledException))
                {
                    OnErrorOccurred(new Exception($"发送消息任务异常: {ex.Message}", ex));
                }
            }
        }

        /// <summary>
        /// 队列化发送消息（异步处理）
        /// </summary>
        /// <param name="message">要发送的消息</param>
        public void QueueMessage(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
                return;

            if (!IsConnected)
                throw new InvalidOperationException("未连接到服务器，无法发送消息");

            _messageQueue.Enqueue(message);
            _messageAvailable.Release();
        }

        // 事件触发方法
        protected virtual void OnMessageReceived(string message)
        {
            MessageReceived?.Invoke(this, new PipeMessageEventArgs(message));
        }

        protected virtual void OnMessageSent(string message)
        {
            MessageSent?.Invoke(this, new PipeMessageEventArgs(message));
        }

        protected virtual void OnConnectionStatusChanged(bool isConnected, string message = "")
        {
            ConnectionStatusChanged?.Invoke(this, new PipeConnectionEventArgs(isConnected, message));
        }

        protected virtual void OnErrorOccurred(Exception ex)
        {
            ErrorOccurred?.Invoke(this, ex);
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            try
            {
                DisconnectAsync().Wait();
            }
            catch { }
            finally
            {
                _messageAvailable?.Dispose();
                _pipeClient?.Dispose();
                _cancellationTokenSource?.Dispose();
            }
        }

        /// <summary>
        /// 测试方法
        /// </summary>
        public static void Test()
        {
            Console.WriteLine("=== SimpleNamedPipeClient 测试 ===");

            // 创建客户端实例
            var client = new SimpleNamedPipeClient("testPipe");

            // 记录事件
            int messageReceivedCount = 0;
            int messageSentCount = 0;

            client.MessageReceived += (sender, e) =>
            {
                messageReceivedCount++;
                Console.WriteLine($"[{e.Timestamp:HH:mm:ss.fff}] 收到: {e.Message}");
            };

            client.MessageSent += (sender, e) =>
            {
                messageSentCount++;
                Console.WriteLine($"[{e.Timestamp:HH:mm:ss.fff}] 已发送: {e.Message}");
            };

            client.ConnectionStatusChanged += (sender, e) =>
            {
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] 连接状态: {(e.IsConnected ? "已连接" : "已断开")} {e.Message}");
            };

            client.ErrorOccurred += (sender, e) =>
            {
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] 错误: {e.Message}");
            };

            try
            {
                // 测试连接（预期会失败，因为没有服务器）
                Console.WriteLine("测试连接（预期失败）...");
                try
                {
                    client.Connect();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"连接失败（预期中）: {ex.Message}");
                }

                // 测试属性
                Console.WriteLine($"\n状态检查:");
                Console.WriteLine($"  IsConnected: {client.IsConnected}");
                Console.WriteLine($"  IsRunning: {client.IsRunning}");
                Console.WriteLine($"  QueuedMessages: {client.QueuedMessages}");

                // 测试发送消息（应该抛出异常）
                Console.WriteLine($"\n测试发送消息（预期异常）...");
                try
                {
                    client.Send("Test message");
                }
                catch (InvalidOperationException ex)
                {
                    Console.WriteLine($"发送失败（预期中）: {ex.Message}");
                }

                // 测试断开连接
                Console.WriteLine($"\n测试断开连接...");
                client.DisconnectAsync().Wait();

                // 测试清理
                Console.WriteLine($"\n测试资源清理...");
                client.Dispose();

                Console.WriteLine($"\n测试结果:");
                Console.WriteLine($"  收到消息数: {messageReceivedCount}");
                Console.WriteLine($"  发送消息数: {messageSentCount}");
                Console.WriteLine("测试完成！");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"测试过程中发生错误: {ex.Message}");
            }
        }
    }
}
