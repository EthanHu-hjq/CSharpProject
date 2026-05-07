using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Net;
using static System.Net.Mime.MediaTypeNames;
using System.Diagnostics;
using ValueType;

namespace TimeChecker
{
    class Program
    {
        // NTP服务器地址
        private const string NtpServer = "ftp5d.tymphany.com";
        // 时间校验周期（单位：秒）
        private const int CheckInterval = 5;
        // 时间偏差阈值（单位：秒）
        private const double TimeDiffThreshold = 5;
        // NTP重同步周期（单位：小时）
        private const int ResyncIntervalHours = 3;
        // NTP同步最大重试次数
        private const int MaxNtpRetries = 3;

        // 初始化时间标识（主程序启动时的NTP时间）
        private static DateTime _initialNtpTime;
        // 程序启动后的高精度计时器（计算运行时长）
        private static Stopwatch _programStopwatch;
        // 后台任务取消令牌
        private static CancellationTokenSource _cts;


        static async Task Main(string[] args)
        {
            Console.WriteLine("程序启动，开始初始化NTP同步...");

            try
            {
                // 1. 初始化NTP同步（带重试机制）
                if (!await SyncNtpWithRetry())
                {
                    Console.WriteLine("NTP初始同步失败，程序退出");
                    return;
                }

                // 2. 启动高精度计时器（记录程序运行时长）
                _programStopwatch = Stopwatch.StartNew();

                // 3. 启动后台时间校验任务
                _cts = new CancellationTokenSource();
                _ = RunTimeCheckTask(_cts.Token);

                Console.WriteLine("程序运行中，按任意键退出...");
                Console.ReadKey();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"程序异常: {ex.Message}");
            }
            finally
            {
                // 停止后台任务
                _cts?.Cancel();
                Console.WriteLine("程序退出");
            }
        }


        /// <summary>
        /// NTP同步（带重试机制）
        /// </summary>
        private static async Task<bool> SyncNtpWithRetry()
        {
            for (int retry = 0; retry < MaxNtpRetries; retry++)
            {
                try
                {
                    _initialNtpTime = TimeSyncService.GetNetworkTime(NtpServer);
                    Console.WriteLine($"NTP同步成功，初始时间: {_initialNtpTime:yyyy-MM-dd HH:mm:ss.fff}");
                    return true;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"第{retry + 1}次NTP同步失败: {ex.Message}");
                    if (retry < MaxNtpRetries - 1)
                    {
                        await Task.Delay(1000); // 重试间隔1秒
                    }
                }
            }
            return false;
        }


        /// <summary>
        /// 后台时间校验任务
        /// </summary>
        private static async Task RunTimeCheckTask(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    // ========== 步骤1：判断是否需要重同步NTP ==========
                    var runHours = _programStopwatch.Elapsed.TotalHours;
                    if (runHours >= ResyncIntervalHours)
                    {
                        Console.WriteLine($"程序已运行{runHours:F2}小时，开始NTP重同步...");
                        if (await SyncNtpWithRetry())
                        {
                            _programStopwatch.Restart(); // 重置计时器（重新计算运行时长）
                        }
                    }

                    // ========== 步骤2：计算模拟NTP时间 ==========
                    // 模拟NTP时间 = 初始NTP时间 + 程序运行时长
                    var elapsedMs = _programStopwatch.ElapsedMilliseconds;
                    var simulatedNtpTime = _initialNtpTime.AddMilliseconds(elapsedMs);

                    // ========== 步骤3：获取本地时间并比对 ==========
                    var localTime = DateTime.Now;
                    var diffSeconds = TimeSyncService.CalculateTimeDiff(simulatedNtpTime, localTime);

                    Console.WriteLine($"[时间校验] 模拟NTP时间: {simulatedNtpTime:yyyy-MM-dd HH:mm:ss} | 本地时间: {localTime:yyyy-MM-dd HH:mm:ss} | 偏差: {diffSeconds:F2}秒");

                    // 偏差超过阈值 → 提示（WPF跨线程安全）
                    if (Math.Abs(diffSeconds) > TimeDiffThreshold)
                    {
                        Console.WriteLine($"系统时间与服务器时间偏差过大，当前偏差：{diffSeconds:F2}秒，即将执行时间修正...");
                        //Application.Current.Dispatcher.Invoke(() =>
                        //{
                        //    MessageBox.Show(
                        //        $"时间偏差过大！当前偏差：{diffSeconds:F2}秒，即将执行时间修正...",
                        //        "时间异常",
                        //        MessageBoxButton.OK,
                        //        MessageBoxImage.Warning
                        //    );
                        //    // 实际场景：调用外部exe修改时间
                        //    // Process.Start("TimeSetter.exe");
                        //});
                        TimeSync(NtpServer, TimeDiffThreshold);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[时间校验异常] {ex.Message}");
                }

                // 等待下一个校验周期
                await Task.Delay(TimeSpan.FromSeconds(CheckInterval), cancellationToken);
            }
        }

        private static void TimeSync(string host, double maxDifTotalSeconds)
        {
            //通过命令行调用当前程序所在目录下Bin文件下的TimeSyncTool.exe，用于同步时间，确保测试数据的时间戳正确
            string timeSyncToolPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Bin", "TimeSyncTool.exe");
            if (File.Exists(timeSyncToolPath))
            {
                try
                {
                    var syncArgs = new[] { host, maxDifTotalSeconds.ToString() };
                    var psi = new ProcessStartInfo
                    {
                        FileName = timeSyncToolPath,
                        Arguments = string.Join(" ", syncArgs.Select(a => $"\"{a}\"")),
                        UseShellExecute = true,
                        Verb = "runas" // 以管理员权限运行
                    };
                    var process = Process.Start(psi);
                    if (process != null && !process.HasExited)
                    {
                        Console.WriteLine("时间同步工具已启动");
                        //Application.Current.Dispatcher.Invoke(() =>
                        //{
                        //    Message = "时间同步工具已启动";
                        //});
                    }
                    else
                    {
                        Console.WriteLine("时间同步工具启动失败");
                        //Application.Current.Dispatcher.Invoke(() =>
                        //{
                        //    Message = "时间同步工具启动失败";
                        //});
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"时间同步失败: {ex.Message}");
                    //Application.Current.Dispatcher.Invoke(() =>
                    //{
                    //    Message = $"时间同步失败: {ex.Message}";
                    //});
                }
            }
            else
            {
                Console.WriteLine($"时间同步工具未找到: {timeSyncToolPath}");
                //Application.Current.Dispatcher.Invoke(() =>
                //{
                //    Message = $"时间同步工具未找到: {timeSyncToolPath}";
                //});
            }
        }
    }
}

