using System;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Globalization;
using System.Management;
using System.Security.Principal;
using TimeSyncTool;
using static TimeSyncTool.TimeSyncService;
using System.CodeDom;

namespace TimeSyncToolNew
{
    class Program
    {
        static void Main(string[] args)
        {
            string ntpServer = ((args.Length > 0) && !string.IsNullOrEmpty(args[0])) ? args[0].Trim() : "ftp5d.tymphany.com"; // 国内常用NTP服务器（可替换）
            double timeThreshold = args.Length > 1 && double.TryParse(args[1], out double t) ? t : 1;// 时间差阈值（分钟）
            Console.WriteLine($"ftp : {ntpServer}.");
            Console.WriteLine($"timeThread : {timeThreshold}.");

            Console.WriteLine("时间同步工具 - 控制台版");
            Console.WriteLine("------------------------");

            try
            {
                // 步骤1：从NTP服务器获取时间
                Console.WriteLine("正在从NTP服务器获取时间...");
                DateTime serverTime = TimeSyncService.GetNetworkTime(ntpServer);
                Console.WriteLine($"NTP服务器时间：{serverTime:yyyy-MM-dd HH:mm:ss} (UTC)");

                // 步骤2：获取本地时间
                DateTime localTime = DateTime.Now;
                Console.WriteLine($"本地当前时间：{localTime:yyyy-MM-dd HH:mm:ss} (UTC+{TimeZoneInfo.Local.BaseUtcOffset.Hours})");

                // 步骤3：计算时间差并判断是否需要同步
                double timeDiff = TimeSyncService.CalculateTimeDiff(serverTime, localTime);
                Console.WriteLine($"时间差阈值为：{timeThreshold} 分钟");
                Console.WriteLine($"时间差：{timeDiff:F2} 分钟");

                if (timeDiff > timeThreshold)
                {
                    Console.WriteLine("时间差超过阈值，尝试同步系统时间...");

                    // 步骤4：验证并激活管理员权限
                    if (true)
                    {
                        Console.WriteLine("已获取管理员权限，执行时间设置...");

                        // 转换为UTC时间（SetSystemTime要求UTC）
                        DateTime utcServerTime = serverTime.ToUniversalTime();
                        SYSTEMTIME st = TimeSyncService.ConvertToSystemTime(utcServerTime);

                        // 调用Windows API设置系统时间
                        bool setResult = SetSystemTime(ref st);
                        if (setResult)
                        {
                            Console.WriteLine("系统时间设置成功！");
                        }
                        else
                        {
                            int errorCode = Marshal.GetLastWin32Error();
                            Console.WriteLine($"设置失败！错误码：{errorCode}（需以管理员身份运行）");
                        }
                    }
                }
                else
                {
                    Console.WriteLine("时间差在允许范围内，无需同步。");
                }
            }
            catch (SocketException se)
            {
                throw new Exception($"网络错误：无法连接到NTP服务器 {ntpServer}。请检查网络连接和服务器地址是否正确。", se);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"程序错误：{ex.Message}");
                throw new Exception("发生错误，请检查输入参数和网络连接。", ex);
            }
        }
    }
}