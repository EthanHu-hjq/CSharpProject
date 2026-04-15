using System;
using System.Net.NetworkInformation;

namespace ValueType
{
    public struct MutablePoint
    {
        public int X;
        public int Y;
        public MutablePoint(int x, int y) => (X, Y) = (x, y);

        //重写ToString方法,当用Console.WriteLine打印时,会调用ToString方法
        public override string ToString() => $"({X}, {Y})";
    }
    internal class Program
    {
        static void Main(string[] args)
        {
            //var p1 = new MutablePoint(1, 2);
            //var p2 = p1;
            //p2.X = 200;
            //Console.WriteLine($"{nameof(p1)} after {nameof(p2)} is modified: {p1}");
            //Console.WriteLine($"{nameof(p2)}:{p2}");
            //MutateAndDisplay(p2);
            //Console.WriteLine($"{nameof(p2)} after mutation: {p2}");

            //ping本地回环地址
            string ipAddress = "192.168.10.12"; // 这里替换为你想要ping的IP地址
            Ping pingSender = new Ping();

            try
            {
                PingReply reply = pingSender.Send(ipAddress);
                if (reply.Status == IPStatus.Success)
                {
                    Console.WriteLine("IP地址 {0} 可达。", ipAddress);
                    Console.WriteLine("回复时间: {0} 毫秒", reply.RoundtripTime);
                }
                else
                {
                    Console.WriteLine("IP地址 {0} 不可达。状态: {1}", ipAddress, reply.Status);
                }
            }
            catch (PingException e)
            {
                Console.WriteLine(e.ToString());
            }

        }

        private static void MutateAndDisplay(MutablePoint p)
        {
            p.X = 100;
            Console.WriteLine($"Point after mutation: {p}");
        }
    }
}
