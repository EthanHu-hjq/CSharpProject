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
    internal class Program
    {
        private static CancellationTokenSource _cancellationTokenSource;
        private static NamedPipeServerStream _pipeServer;
        private static readonly ConcurrentQueue<MessageItem> _messageQueue = new ConcurrentQueue<MessageItem>();
        private static readonly SemaphoreSlim _messageAvailable = new SemaphoreSlim(0, int.MaxValue);
        private static bool _isSending = false;

        // 存储待确认的消息（消息内容 -> 发送时间+重发标记）
        private static readonly ConcurrentDictionary<string, (DateTime SendTime, bool IsRetried)> _pendingAckMessages = new ConcurrentDictionary<string, (DateTime, bool)>();
        // 确认消息超时时间（单位：毫秒）
        private const int AckTimeoutMs = 3000;

        // 新增：消息项实体，包含原始消息和是否已重发标记
        private class MessageItem
        {
            public string Content { get; set; }
            public DateTime SendTime { get; set; }
            public bool IsRetried { get; set; }
        }

        static async Task Main(string[] args)
        {
            Console.WriteLine("服务器已启动...");

            while (true) // 外层循环：服务器持续运行，支持重连
            {
                // 创建新的命名管道服务器实例
                _pipeServer = new NamedPipeServerStream("myPipe",
                    PipeDirection.InOut,
                    NamedPipeServerStream.MaxAllowedServerInstances,
                    PipeTransmissionMode.Byte,
                    PipeOptions.Asynchronous);

                Console.WriteLine("等待客户端连接...");
                await _pipeServer.WaitForConnectionAsync();
                Console.WriteLine("客户端已连接！");
                Console.WriteLine("您可以输入消息并按Enter发送给客户端，或等待客户端发送消息。");

                // 创建取消令牌源
                _cancellationTokenSource = new CancellationTokenSource();
                var cancellationToken = _cancellationTokenSource.Token;

                // 清空消息队列和待确认消息字典
                while (_messageQueue.TryDequeue(out _)) { }
                _pendingAckMessages.Clear();
                _isSending = false;

                // 创建接收、发送和输入任务
                var receiveTask = Task.Run(() => ReceiveMessagesAsync(_pipeServer, cancellationToken), cancellationToken);
                var sendTask = Task.Run(() => SendMessagesAsync(_pipeServer, cancellationToken), cancellationToken);
                var inputTask = Task.Run(() => HandleConsoleInputAsync(cancellationToken), cancellationToken);
                // 新增：检查确认超时的任务
                //var ackCheckTask = Task.Run(() => CheckAckTimeoutAsync(cancellationToken), cancellationToken);

                try
                {
                    // 等待任意任务完成
                    await Task.WhenAny(receiveTask, sendTask, inputTask);

                    // 取消所有任务
                    _cancellationTokenSource.Cancel();

                    // 等待所有任务完成
                    await Task.WhenAll(receiveTask, sendTask, inputTask);
                }
                catch (OperationCanceledException)
                {
                    // 任务被取消是预期的
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"任务异常: {ex.Message}");
                }
                finally
                {
                    _cancellationTokenSource.Dispose();
                }

                // 关闭当前管道服务器实例，释放资源
                try
                {
                    _pipeServer.Close();
                    _pipeServer.Dispose();
                }
                catch { } // 忽略关闭过程中的任何异常
                Console.WriteLine("等待新客户端连接...");
            }
        }

        /// <summary>
        /// 接收来自客户端的消息（修改后：接收到消息回复确认 + 处理客户端的确认消息）
        /// </summary>
        private static async Task ReceiveMessagesAsync(NamedPipeServerStream pipeServer, CancellationToken cancellationToken)
        {
            try
            {
                while (!cancellationToken.IsCancellationRequested && pipeServer.IsConnected)
                {
                    byte[] buffer = new byte[1024];
                    int bytesRead = 0;
                    try
                    {
                        bytesRead = await pipeServer.ReadAsync(buffer, 0, buffer.Length, cancellationToken);
                        if (bytesRead > 0)
                        {
                            string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                            Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] 收到客户端消息: {message}");

                            // 1. 判断是否是客户端的确认消息
                            if (message.StartsWith("ACK: "))
                            {
                                // 提取原始消息内容（格式：ACK: 已收到消息 -> 原消息）
                                string originalMsg = message.Substring(message.IndexOf("-> ") + 3);
                                if (_pendingAckMessages.TryRemove(originalMsg, out _))
                                {
                                    Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] 收到客户端确认: {originalMsg}");
                                }
                                continue; // 确认消息无需回复，直接跳过
                            }

                            // 2. 非确认消息：回复确认回执
                            string ackMessage = $"ACK: 已收到消息 -> {message}";
                            await SendAckMessageAsync(pipeServer, ackMessage, cancellationToken);
                        }
                        else if (bytesRead == 0)
                        {
                            // 客户端正常断开连接
                            Console.WriteLine("客户端断开连接。");
                            break;
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        // 任务被取消
                        break;
                    }
                    catch (Exception ex) when (ex is IOException || ex is ObjectDisposedException)
                    {
                        // 处理网络异常或管道关闭导致的断开
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
        /// 发送消息给客户端（修改后：记录发送状态 + 支持重发）
        /// </summary>
        private static async Task SendMessagesAsync(NamedPipeServerStream pipeServer, CancellationToken cancellationToken)
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

                        // 从队列中获取消息
                        if (_messageQueue.TryDequeue(out var messageItem) && !string.IsNullOrWhiteSpace(messageItem.Content))
                        {
                            _isSending = true;
                            byte[] messageBytes = Encoding.UTF8.GetBytes(messageItem.Content);
                            await pipeServer.WriteAsync(messageBytes, 0, messageBytes.Length, cancellationToken);
                            await pipeServer.FlushAsync(cancellationToken); // 强制刷新缓冲区
                            Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] 已发送: {messageItem.Content} ");

                            // 记录待确认的消息（仅首次发送/未重发过的消息需要确认）
                            if (!messageItem.IsRetried)
                            {
                                _pendingAckMessages[messageItem.Content] = (messageItem.SendTime, messageItem.IsRetried);
                            }
                            _isSending = false;
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        // 任务被取消
                        break;
                    }
                    catch (Exception ex) when (ex is IOException || ex is ObjectDisposedException)
                    {
                        // 处理发送异常
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
        /// 新增：发送确认消息给客户端
        /// </summary>
        private static async Task SendAckMessageAsync(NamedPipeServerStream pipeServer, string ackMessage, CancellationToken cancellationToken)
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
            catch (OperationCanceledException)
            {
                // 取消操作，无需处理
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
        /// 新增：检查待确认消息的超时，超时则重发一次
        /// </summary>
        private static async Task CheckAckTimeoutAsync(CancellationToken cancellationToken)
        {
            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    await Task.Delay(500, cancellationToken); // 每500ms检查一次

                    // 遍历所有待确认的消息
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

                            // 将消息重新加入发送队列（标记为已重发）
                            _messageQueue.Enqueue(new MessageItem
                            {
                                Content = message,
                                SendTime = DateTime.Now,
                                IsRetried = true
                            });
                            _messageAvailable.Release(); // 通知发送任务
                        }
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // 任务被取消
            }
            catch (Exception ex)
            {
                Console.WriteLine($"检查确认超时任务异常: {ex.Message}");
            }
        }

        /// <summary>
        /// 处理控制台输入（修改后：封装MessageItem）
        /// </summary>
        private static async Task HandleConsoleInputAsync(CancellationToken cancellationToken)
        {
            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    try
                    {
                        // 读取控制台输入
                        Console.Write("请输入要发送的消息: ");
                        string input = await ReadConsoleLineAsync(cancellationToken);

                        if (cancellationToken.IsCancellationRequested)
                            break;

                        if (!string.IsNullOrWhiteSpace(input))
                        {
                            // 将消息封装为MessageItem加入队列
                            _messageQueue.Enqueue(new MessageItem
                            {
                                Content = input,
                                SendTime = DateTime.Now,
                                IsRetried = false
                            });
                            _messageAvailable.Release(); // 通知发送任务有消息可发送
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        // 任务被取消
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
        /// 异步读取控制台输入（无修改）
        /// </summary>
        private static Task<string> ReadConsoleLineAsync(CancellationToken cancellationToken)
        {
            return Task.Run(() =>
            {
                var tcs = new TaskCompletionSource<string>();

                // 在后台线程中读取控制台输入
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

                // 等待输入完成或取消
                return WaitForTaskWithCancellation(tcs.Task, cancellationToken);
            }, cancellationToken);
        }

        /// <summary>
        /// 等待任务完成，支持取消操作（无修改）
        /// </summary>
        private static async Task<T> WaitForTaskWithCancellation<T>(Task<T> task, CancellationToken cancellationToken)
        {
            // 创建一个在取消时完成的任务
            var tcs = new TaskCompletionSource<bool>();
            cancellationToken.Register(() => tcs.TrySetResult(true));

            // 等待原始任务或取消任务完成
            var completedTask = await Task.WhenAny(task, tcs.Task);

            if (completedTask == task)
            {
                return await task;
            }
            else
            {
                // 任务被取消
                cancellationToken.ThrowIfCancellationRequested();
                return default(T);
            }
        }
    }
}