using System;
using System.IO.Pipes;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.IO;

namespace ProgramA
{
    internal class Program
    {
        private static CancellationTokenSource _cancellationTokenSource;
        private static NamedPipeClientStream _pipeClient;
        private static readonly ConcurrentQueue<string> _messageQueue = new ConcurrentQueue<string>();
        private static readonly SemaphoreSlim _messageAvailable = new SemaphoreSlim(0, int.MaxValue);
        private static bool _isConnected = false;

        static async Task Main(string[] args)
        {
            Console.WriteLine("客户端已启动...");

            while (true) // 外层循环：客户端持续运行，支持重连
            {
                try
                {
                    // 创建新的命名管道客户端实例
                    _pipeClient = new NamedPipeClientStream(".", "myPipe", PipeDirection.InOut, PipeOptions.Asynchronous);

                    Console.WriteLine("正在连接服务器...");
                    await _pipeClient.ConnectAsync(5000); // 5秒连接超时
                    _isConnected = true;
                    Console.WriteLine("已连接到服务器！");
                    Console.WriteLine("您可以输入消息并按Enter发送给服务器，或等待服务器发送消息。");

                    // 创建取消令牌源
                    _cancellationTokenSource = new CancellationTokenSource();
                    var cancellationToken = _cancellationTokenSource.Token;

                    // 清空消息队列
                    while (_messageQueue.TryDequeue(out _)) { }

                    // 创建接收、发送和输入任务
                    var receiveTask = Task.Run(() => ReceiveMessagesAsync(_pipeClient, cancellationToken), cancellationToken);
                    var sendTask = Task.Run(() => SendMessagesAsync(_pipeClient, cancellationToken), cancellationToken);
                    var inputTask = Task.Run(() => HandleConsoleInputAsync(cancellationToken), cancellationToken);

                    try
                    {
                        // 等待任意任务完成
                        await Task.WhenAny(receiveTask, sendTask, inputTask);

                        // 取消所有任务
                        _cancellationTokenSource.Cancel();
                        _isConnected = false;

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

                    // 关闭当前管道客户端实例，释放资源
                    try
                    {
                        _pipeClient.Close();
                        _pipeClient.Dispose();
                    }
                    catch { } // 忽略关闭过程中的任何异常

                    Console.WriteLine("连接断开，5秒后尝试重新连接...");
                    await Task.Delay(5000); // 等待5秒后重连
                }
                catch (TimeoutException)
                {
                    Console.WriteLine("连接服务器超时，5秒后重试...");
                    await Task.Delay(5000);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"连接异常: {ex.Message}，5秒后重试...");
                    await Task.Delay(5000);
                }
            }
        }

        /// <summary>
        /// 接收来自服务器的消息
        /// </summary>
        private static async Task ReceiveMessagesAsync(NamedPipeClientStream pipeClient, CancellationToken cancellationToken)
        {
            try
            {
                while (!cancellationToken.IsCancellationRequested && pipeClient.IsConnected)
                {
                    byte[] buffer = new byte[1024];
                    int bytesRead = 0;
                    try
                    {
                        bytesRead = await pipeClient.ReadAsync(buffer, 0, buffer.Length, cancellationToken);
                        if (bytesRead > 0)
                        {
                            string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                            Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] 收到服务器消息: {message}");
                        }
                        else if (bytesRead == 0)
                        {
                            // 服务器正常断开连接
                            Console.WriteLine("服务器断开连接。");
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
        /// 发送消息给服务器
        /// </summary>
        private static async Task SendMessagesAsync(NamedPipeClientStream pipeClient, CancellationToken cancellationToken)
        {
            try
            {
                while (!cancellationToken.IsCancellationRequested && pipeClient.IsConnected)
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
                            byte[] messageBytes = Encoding.UTF8.GetBytes(message);
                            await pipeClient.WriteAsync(messageBytes, 0, messageBytes.Length, cancellationToken);
                            Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] 已发送: {message}");
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
                        if (!_isConnected)
                        {
                            await Task.Delay(100, cancellationToken);
                            continue;
                        }

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
