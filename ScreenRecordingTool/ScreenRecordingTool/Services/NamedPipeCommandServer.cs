using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Text;

namespace ScreenRecordingTool.Services
{
    /// <summary>
    /// 命名管道命令服务端：接收外部进程发来的文本指令，驱动录制任务。
    /// 协议（每条消息一条，UTF-8，Message 模式）：
    ///   START &lt;flag&gt;   以标志位启动一个独立录制任务
    ///   STOP  &lt;flag&gt;   停止与该标志位匹配、且正在录制的任务
    /// </summary>
    public sealed class NamedPipeCommandServer : IDisposable
    {
        private const string PipeName = "ScreenRecordingTool.Pipe";
        private const int MaxMessageSize = 4096;

        private readonly Func<string, Task> _onStart;
        private readonly Func<string, Task> _onStop;
        private readonly CancellationTokenSource _cts = new();
        private readonly Task _listenTask;

        public NamedPipeCommandServer(Func<string, Task> onStart, Func<string, Task> onStop)
        {
            _onStart = onStart ?? throw new ArgumentNullException(nameof(onStart));
            _onStop = onStop ?? throw new ArgumentNullException(nameof(onStop));
            _listenTask = Task.Run(ListenLoop, _cts.Token);
        }

        private async Task ListenLoop()
        {
            while (!_cts.IsCancellationRequested)
            {
                NamedPipeServerStream? pipe = null;
                try
                {
                    // Message 模式：每条 Write/Flush 对应一条完整消息，服务端按消息边界读取，
                    // 避免 Byte 模式下 ReadLineAsync 与 Dispose 之间的竞态导致 ObjectDisposedException
                    pipe = new NamedPipeServerStream(
                        PipeName,
                        PipeDirection.InOut,          // InOut 以支持 WaitForPipeDrain
                        1,
                        PipeTransmissionMode.Message,
                        PipeOptions.Asynchronous);

                    await pipe.WaitForConnectionAsync(_cts.Token).ConfigureAwait(false);

                    await HandleClientAsync(pipe).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (IOException)
                {
                    // 客户端异常断开（如进程被杀），继续监听下一个连接
                    Debug.WriteLine("[NamedPipe] 客户端 IO 异常断开");
                }
                catch (Exception ex)
                {
                    // 记录但不中断监听循环（单个连接错误不应终止整个服务）
                    Debug.WriteLine($"[NamedPipe] 连接处理异常: {ex}");
                }
                finally
                {
                    if (pipe != null)
                    {
                        // 关键修复：等待客户端将缓冲区数据全部写入后再断开，
                        // 防止客户端 Flush() 时收到 ObjectDisposedException
                        try
                        {
                            if (pipe.IsConnected)
                            {
                                pipe.WaitForPipeDrain();
                            }
                        }
                        catch
                        {
                            // 客户端已断开时 WaitForPipeDrain 可能抛出，忽略即可
                        }

                        pipe.Dispose();
                    }
                }
            }
        }

        private async Task HandleClientAsync(NamedPipeServerStream pipe)
        {
            var buffer = new byte[MaxMessageSize];

            while (!_cts.IsCancellationRequested && pipe.IsConnected)
            {
                try
                {
                    // Message 模式：每次 ReadAsync 恰好返回一条完整消息（对应客户端一次 Write+Flush）
                    int bytesRead = await pipe.ReadAsync(buffer, _cts.Token).ConfigureAwait(false);

                    if (bytesRead == 0)
                    {
                        break; // 客户端已断开
                    }

                    string line = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    if (line.Length == 0)
                    {
                        continue;
                    }

                    int space = line.IndexOf(' ');
                    string verb = space < 0 ? line : line[..space];
                    string arg = space < 0 ? string.Empty : line[(space + 1)..].Trim();

                    if (string.Equals(verb, "START", StringComparison.OrdinalIgnoreCase) && arg.Length > 0)
                    {
                        await _onStart(arg).ConfigureAwait(false);
                    }
                    else if (string.Equals(verb, "STOP", StringComparison.OrdinalIgnoreCase) && arg.Length > 0)
                    {
                        await _onStop(arg).ConfigureAwait(false);
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (IOException)
                {
                    break; // 连接已断开，退出当前连接处理
                }
            }
        }

        public void Dispose()
        {
            _cts.Cancel();
            try
            {
                _listenTask.Wait(TimeSpan.FromSeconds(3));
            }
            catch
            {
                // 忽略关闭时的等待超时
            }
            _cts.Dispose();
        }
    }
}
