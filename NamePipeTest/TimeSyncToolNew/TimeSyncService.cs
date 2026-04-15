using System;
using System.Net;
using System.Runtime.InteropServices;

namespace TimeSyncTool
{
    public static class TimeSyncService
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool SetSystemTime(ref SYSTEMTIME st);

        [StructLayout(LayoutKind.Sequential)]
        public struct SYSTEMTIME
        {
            public ushort wYear;
            public ushort wMonth;
            public ushort wDayOfWeek;
            public ushort wDay;
            public ushort wHour;
            public ushort wMinute;
            public ushort wSecond;
            public ushort wMilliseconds;
        }

        public static DateTime GetNetworkTime(string ntpServer)
        {
            var ntpData = new byte[48];
            ntpData[0] = 0x1B;

            var addresses = Dns.GetHostEntry(ntpServer).AddressList;

            // 👈 这是兼容 C# 7.3 的正确写法
            using (var socket = new System.Net.Sockets.Socket(
                System.Net.Sockets.AddressFamily.InterNetwork,
                System.Net.Sockets.SocketType.Dgram,
                System.Net.Sockets.ProtocolType.Udp))
            {
                socket.ReceiveTimeout = 3000;
                socket.Connect(addresses[0], 123);
                socket.Send(ntpData);
                socket.Receive(ntpData);
            }

            ulong intPart = BitConverter.ToUInt32(ntpData, 40);
            ulong fractPart = BitConverter.ToUInt32(ntpData, 44);

            intPart = SwapEndianness(intPart);
            fractPart = SwapEndianness(fractPart);

            var milliseconds = (intPart * 1000) + ((fractPart * 1000) / 0x100000000L);
            var networkDateTime = new DateTime(1900, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                .AddMilliseconds((long)milliseconds);

            return networkDateTime.ToLocalTime();
        }

        private static uint SwapEndianness(ulong x)
        {
            return (uint)(((x & 0x000000ff) << 24) |
                          ((x & 0x0000ff00) << 8) |
                          ((x & 0x00ff0000) >> 8) |
                          ((x & 0xff000000) >> 24));
        }

        public static double CalculateTimeDiff(DateTime serverTime, DateTime localTime)
        {
            TimeSpan diff = serverTime - localTime;
            return Math.Abs(diff.TotalMinutes);
        }

        public static SYSTEMTIME ConvertToSystemTime(DateTime utcTime)
        {
            return new SYSTEMTIME
            {
                wYear = (ushort)utcTime.Year,
                wMonth = (ushort)utcTime.Month,
                wDayOfWeek = (ushort)utcTime.DayOfWeek,
                wDay = (ushort)utcTime.Day,
                wHour = (ushort)utcTime.Hour,
                wMinute = (ushort)utcTime.Minute,
                wSecond = (ushort)utcTime.Second,
                wMilliseconds = (ushort)utcTime.Millisecond
            };
        }
    }
}