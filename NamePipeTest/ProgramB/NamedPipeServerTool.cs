using System;
using System.IO.Pipes;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.IO;
using System.Collections.Generic;

namespace ProgramB
{
    /// <summary>
    /// 命名管道消息项
    /// </summary>
    public class MessageItem
    {
        public string Content { get; set; }
        public DateTime SendTime { get; set; }
        public bool IsRetried { get; set; }
    }

    /// <summary>
    /// 命名管道服务器工具类
    /// 提供可靠的消息通信机制
    /// </summary>
    public class NamedPipeServerTool
    {
        // 核心组件
        private NamedPipeServerStream _pipeServer;
        private CancellationTokenSource _cancellationTokenSource;

        // 消息管理
        private readonly ConcurrentQueue<MessageItem> _messageQueue = new ConcurrentQueue<MessageItem>();
        private readonly ConcurrentDictionary<string, (DateTime SendTime, bool IsRetried)> _pendingAckMessages =
            new ConcurrentDictionary<string, (DateTime, bool)>();
        private readonly SemaphoreSlim _messageAvailable = new SemaphoreSlim(0, int.MaxValue);

        // 状态标志
        private bool _isSending = false;

        // 配置常量
        private const int AckTimeoutMs = 3000;
        private const string PipeName = "myPipe";
        private const int BufferSize = 1024;

        /// <summary>
        /// 启动命名管道服务器
        /// </summary>
        public async Task StartServerAsync()
        {
            Console.WriteLine("服务器已启动...");

            while (true) // 外层循环：服务器持续运行，支持重连
            {
                // 创建新的命名管道服务器实例
                _pipeServer = new NamedPipeServerStream(
                    PipeName,
                    PipeDirection.InOut,
                    NamedPipeServerStream.MaxAllowedServerInstances,
                    PipeTransmissionMode.Byte,
                    PipeOptions.Asynchronous
                );

                Console.WriteLine("等待客户端连接...");
                await _pipeServer.WaitForConnectionAsync();
                Console.WriteLine("客户端已连接！");
                Console.WriteLine("您可以输入消息并按Enter发送给客户端，或等待客户端发送消息。");

                // 初始化组件
                _cancellationTokenSource = new CancellationTokenSource();
                var cancellationToken = _cancellationTokenSource.Token;

                // 清理状态
                ClearMessageState();

                try
                {
                    // 创建并运行所有任务
                    await RunCommunicationTasksAsync(cancellationToken);
                }
                catch (Exception ex) when (!(ex is OperationCanceledException))
                {
                    Console.WriteLine($"通信异常: {ex.Message}");
                }
                finally
                {
                    CleanupResources();
                }

                Console.WriteLine("等待新客户端连接...");
            }
        }

        /// <summary>
        /// 清理消息状态
        /// </summary>
        private void ClearMessageState()
        {
            while (_messageQueue.TryDequeue(out _)) { }
            _pendingAckMessages.Clear();
            _isSending = false;
        }

        /// <summary>
        /// 运行所有通信任务
        /// </summary>
        private async Task RunCommunicationTasksAsync(CancellationToken cancellationToken)
        {
            // 创建所有任务
            var receiveTask = Task.Run(() => ReceiveMessagesAsync(_pipeServer, cancellationToken), cancellationToken);
            var sendTask = Task.Run(() => SendMessagesAsync(_pipeServer, cancellationToken), cancellationToken);
            var inputTask = Task.Run(() => HandleConsoleInputAsync(cancellationToken), cancellationToken);
            var ackCheckTask = Task.Run(() => CheckAckTimeoutAsync(cancellationToken), cancellationToken);

            // 等待任意任务完成
            await Task.WhenAny(receiveTask, sendTask, inputTask, ackCheckTask);

            // 取消所有任务
            _cancellationTokenSource.Cancel();

            // 等待所有任务完成
            await Task.WhenAll(receiveTask, sendTask, inputTask, ackCheckTask);
        }

        /// <summary>
        /// 清理资源
        /// </summary>
        private void CleanupResources()
        {
            _cancellationTokenSource?.Dispose();

            try
            {
                _pipeServer?.Close();
                _pipeServer?.Dispose();
            }
            catch
            {
                // 忽略关闭过程中的异常
            }
        }

        /// <summary>
        /// 接收客户端消息
        /// </summary>
        public async Task ReceiveMessagesAsync(NamedPipeServerStream pipeServer, CancellationToken cancellationToken)
        {
            try
            {
                while (!cancellationToken.IsCancellationRequested && pipeServer.IsConnected)
                {
                    byte[] buffer = new byte[BufferSize];
                    int bytesRead = 0;

                    try
                    {
                        bytesRead = await pipeServer.ReadAsync(buffer, 0, buffer.Length, cancellationToken);

                        if (bytesRead > 0)
                        {
                            await ProcessReceivedDataAsync(pipeServer, buffer, bytesRead, cancellationToken);
                        }
                        else if (bytesRead == 0)
                        {
                            Console.WriteLine("客户端断开连接。");
                            break;
                        }
                    }
                    catch (Exception ex) when (ex is IOException || ex is ObjectDisposedException)
                    {
                        Console.WriteLine($"接收消息异常: {ex.Message}");
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"接收消息任务异常: {ex.Message}");
            }
        }

        /// <summary>
        /// 处理接收到的数据
        /// </summary>
        private async Task ProcessReceivedDataAsync(NamedPipeServerStream pipeServer, byte[] buffer, int bytesRead, CancellationToken cancellationToken)
        {
            string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] 收到客户端消息: {message}");

            // 判断是否是确认消息
            if (message.StartsWith("ACK: "))
            {
                ProcessAckMessage(message);
                return;
            }

            // 非确认消息：回复确认回执
            await ReplyWithAckAsync(pipeServer, message, cancellationToken);
        }

        /// <summary>
        /// 处理确认消息
        /// </summary>
        private void ProcessAckMessage(string message)
        {
            // 提取原始消息内容
            int index = message.IndexOf("-> ");
            if (index != -1)
            {
                string originalMsg = message.Substring(index + 3);
                if (_pendingAckMessages.TryRemove(originalMsg, out _))
                {
                    Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] 收到客户端确认: {originalMsg}");
                }
            }
        }

        /// <summary>
        /// 回复确认消息
        /// </summary>
        private async Task ReplyWithAckAsync(NamedPipeServerStream pipeServer, string originalMessage, CancellationToken cancellationToken)
        {
            string ackMessage = $"ACK: 已收到消息 -> {originalMessage}";
            await SendAckMessageAsync(pipeServer, ackMessage, cancellationToken);
        }

        /// <summary>
        /// 发送消息给客户端
        /// </summary>
        public async Task SendMessagesAsync(NamedPipeServerStream pipeServer, CancellationToken cancellationToken)
        {
            try
            {
                while (!cancellationToken.IsCancellationRequested && pipeServer.IsConnected)
                {
                    try
                    {
                        // 等待消息队列中有消息
                        await _messageAvailable.WaitAsync(cancellationToken);

                        if (cancellationToken.IsCancellationRequested)
                            break;

                        // 从队列中获取并发送消息
                        if (_messageQueue.TryDequeue(out var messageItem) && !string.IsNullOrWhiteSpace(messageItem.Content))
                        {
                            await SendSingleMessageAsync(pipeServer, messageItem, cancellationToken);
                        }
                    }
                    catch (Exception ex) when (ex is IOException || ex is ObjectDisposedException)
                    {
                        Console.WriteLine($"发送消息异常: {ex.Message}");
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"发送消息任务异常: {ex.Message}");
            }
        }

        /// <summary>
        /// 发送单个消息
        /// </summary>
        private async Task SendSingleMessageAsync(NamedPipeServerStream pipeServer, MessageItem messageItem, CancellationToken cancellationToken)
        {
            _isSending = true;

            try
            {
                byte[] messageBytes = Encoding.UTF8.GetBytes(messageItem.Content);
                await pipeServer.WriteAsync(messageBytes, 0, messageBytes.Length, cancellationToken);
                await pipeServer.FlushAsync(cancellationToken);
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] 已发送: {messageItem.Content} ");

                // 记录待确认的消息（仅首次发送）
                if (!messageItem.IsRetried)
                {
                    _pendingAckMessages[messageItem.Content] = (messageItem.SendTime, messageItem.IsRetried);
                }
            }
            finally
            {
                _isSending = false;
            }
        }

        /// <summary>
        /// 发送确认消息
        /// </summary>
        public async Task SendAckMessageAsync(NamedPipeServerStream pipeServer, string ackMessage, CancellationToken cancellationToken)
        {
            if (pipeServer == null || !pipeServer.IsConnected || cancellationToken.IsCancellationRequested)
                return;

            try
            {
                byte[] ackBytes = Encoding.UTF8.GetBytes(ackMessage);
                await pipeServer.WriteAsync(ackBytes, 0, ackBytes.Length, cancellationToken);
                await pipeServer.FlushAsync(cancellationToken);
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] 已回复确认消息: {ackMessage}");
            }
            catch (Exception ex) when (ex is IOException || ex is ObjectDisposedException)
            {
                Console.WriteLine($"发送确认消息异常: {ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"发送确认消息失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 检查确认超时
        /// </summary>
        public async Task CheckAckTimeoutAsync(CancellationToken cancellationToken)
        {
            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    await Task.Delay(500, cancellationToken);
                    CheckAndResendTimeoutMessages();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"检查确认超时任务异常: {ex.Message}");
            }
        }

        /// <summary>
        /// 检查并重发超时消息
        /// </summary>
        private void CheckAndResendTimeoutMessages()
        {
            foreach (var msg in _pendingAckMessages.ToArray())
            {
                string message = msg.Key;
                var (sendTime, isRetried) = msg.Value;

                // 检查是否超时（超过3秒）且未重发过
                if (DateTime.Now - sendTime > TimeSpan.FromMilliseconds(AckTimeoutMs) && !isRetried)
                {
                    Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] 消息[{message}]确认超时，准备重发...");

                    // 标记为已重发
                    _pendingAckMessages[message] = (sendTime, true);

                    // 将消息重新加入发送队列
                    _messageQueue.Enqueue(new MessageItem
                    {
                        Content = message,
                        SendTime = DateTime.Now,
                        IsRetried = true
                    });
                    _messageAvailable.Release();
                }
            }
        }

        /// <summary>
        /// 处理控制台输入
        /// </summary>
        public async Task HandleConsoleInputAsync(CancellationToken cancellationToken)
        {
            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    try
                    {
                        Console.Write("请输入要发送的消息: ");
                        string input = await ReadConsoleLineAsync(cancellationToken);

                        if (cancellationToken.IsCancellationRequested)
                            break;

                        if (!string.IsNullOrWhiteSpace(input))
                        {
                            EnqueueMessage(input);
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"输入处理任务异常: {ex.Message}");
            }
        }

        /// <summary>
        /// 将消息加入队列
        /// </summary>
        private void EnqueueMessage(string message)
        {
            _messageQueue.Enqueue(new MessageItem
            {
                Content = message,
                SendTime = DateTime.Now,
                IsRetried = false
            });
            _messageAvailable.Release();
        }

        /// <summary>
        /// 异步读取控制台输入
        /// </summary>
        private Task<string> ReadConsoleLineAsync(CancellationToken cancellationToken)
        {
            return Task.Run(() =>
            {
                var tcs = new TaskCompletionSource<string>();

                ThreadPool.QueueUserWorkItem(_ =>
                {
                    try
                    {
                        if (cancellationToken.IsCancellationRequested)
                        {
                            tcs.TrySetResult(null);
                            return;
                        }

                        string input = Console.ReadLine();
                        tcs.TrySetResult(input);
                    }
                    catch (Exception ex)
                    {
                        tcs.TrySetException(ex);
                    }
                });

                return WaitForTaskWithCancellation(tcs.Task, cancellationToken);
            }, cancellationToken);
        }

        /// <summary>
        /// 等待任务完成（支持取消）
        /// </summary>
        private async Task<T> WaitForTaskWithCancellation<T>(Task<T> task, CancellationToken cancellationToken)
        {
            var tcs = new TaskCompletionSource<bool>();
            cancellationToken.Register(() => tcs.TrySetResult(true));

            var completedTask = await Task.WhenAny(task, tcs.Task);

            if (completedTask == task)
            {
                return await task;
            }
            else
            {
                cancellationToken.ThrowIfCancellationRequested();
                return default(T);
            }
        }
    }
}
