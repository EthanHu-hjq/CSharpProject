using System;
using System.IO.Pipes;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.IO;

namespace ProgramB
{
    internal class Program
    {
        private static CancellationTokenSource _cancellationTokenSource;
        private static NamedPipeServerStream _pipeServer;
        private static readonly ConcurrentQueue<string> _messageQueue = new ConcurrentQueue<string>();
        private static readonly SemaphoreSlim _messageAvailable = new SemaphoreSlim(0, int.MaxValue);
        private static bool _isSending = false;

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

                // 清空消息队列
                while (_messageQueue.TryDequeue(out _)) { }
                _isSending = false;

                // 创建接收、发送和输入任务
                var receiveTask = Task.Run(() => ReceiveMessagesAsync(_pipeServer, cancellationToken), cancellationToken);
                var sendTask = Task.Run(() => SendMessagesAsync(_pipeServer, cancellationToken), cancellationToken);
                var inputTask = Task.Run(() => HandleConsoleInputAsync(cancellationToken), cancellationToken);

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
        /// 接收来自客户端的消息
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
        /// 发送消息给客户端
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
                        if (_messageQueue.TryDequeue(out var message) && !string.IsNullOrWhiteSpace(message))
                        {
                            _isSending = true;
                            byte[] messageBytes = Encoding.UTF8.GetBytes(message);
                            await pipeServer.WriteAsync(messageBytes, 0, messageBytes.Length, cancellationToken);
                            Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] 已发送: {message}");
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
        /// 处理控制台输入
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
                            // 将消息加入队列
                            _messageQueue.Enqueue(input);
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
        /// 异步读取控制台输入
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
        /// 等待任务完成，支持取消操作
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
