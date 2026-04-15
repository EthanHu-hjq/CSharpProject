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
        //// 导入Windows API：设置系统时间（需SYSTEMTIME结构体）
        //[DllImport("kernel32.dll", SetLastError = true)]
        //static extern bool SetSystemTime(ref SYSTEMTIME st);

        //// 定义SYSTEMTIME结构体（与Windows内核结构一致）
        //[StructLayout(LayoutKind.Sequential)]
        //public struct SYSTEMTIME
        //{
        //    public ushort wYear;
        //    public ushort wMonth;
        //    public ushort wDayOfWeek;
        //    public ushort wDay;
        //    public ushort wHour;
        //    public ushort wMinute;
        //    public ushort wSecond;
        //    public ushort wMilliseconds;
        //}

        ///// <summary>
        ///// 从网络时间服务器获取时间（NTP）
        ///// </summary>
        //static DateTime GetNetworkTime(string ntpServer)
        //{
        //    var ntpData = new byte[48];

        //    ntpData[0] = 0x1B; // LI=0, VN=3, Mode=3 (Client)

        //    var addresses = Dns.GetHostEntry(ntpServer).AddressList;
        //    using(var socket = new System.Net.Sockets.Socket(
        //        System.Net.Sockets.AddressFamily.InterNetwork,
        //        System.Net.Sockets.SocketType.Dgram,
        //        System.Net.Sockets.ProtocolType.Udp))
        //    {
        //        socket.ReceiveTimeout = 3000;
        //        socket.Connect(addresses[0], 123);
        //        socket.Send(ntpData);
        //        socket.Receive(ntpData);
        //    }

        //    ulong intPart = BitConverter.ToUInt32(ntpData, 40);
        //    ulong fractPart = BitConverter.ToUInt32(ntpData, 44);

        //    intPart = SwapEndianness(intPart);
        //    fractPart = SwapEndianness(fractPart);

        //    var milliseconds = (intPart * 1000) + ((fractPart * 1000) / 0x100000000L);
        //    var networkDateTime = new DateTime(1900, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        //        .AddMilliseconds((long)milliseconds);

        //    return networkDateTime.ToLocalTime();
        //}
        //private static uint SwapEndianness(ulong x)
        //{
        //    return (uint)(((x & 0x000000ff) << 24) |
        //                  ((x & 0x0000ff00) << 8) |
        //                  ((x & 0x00ff0000) >> 8) |
        //                  ((x & 0xff000000) >> 24));
        //}

        //// 计算时间差（分钟）
        //static double CalculateTimeDiff(DateTime serverTime, DateTime localTime)
        //{
        //    TimeSpan diff = serverTime - localTime;
        //    return Math.Abs(diff.TotalMinutes);
        //}

        //// 将DateTime转换为SYSTEMTIME（UTC时间）
        //static SYSTEMTIME ConvertToSystemTime(DateTime utcTime)
        //{
        //    SYSTEMTIME st = new SYSTEMTIME
        //    {
        //        wYear = (ushort)utcTime.Year,
        //        wMonth = (ushort)utcTime.Month,
        //        wDayOfWeek = (ushort)utcTime.DayOfWeek,
        //        wDay = (ushort)utcTime.Day,
        //        wHour = (ushort)utcTime.Hour,
        //        wMinute = (ushort)utcTime.Minute,
        //        wSecond = (ushort)utcTime.Second,
        //        wMilliseconds = (ushort)utcTime.Millisecond
        //    };
        //    return st;
        //}


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