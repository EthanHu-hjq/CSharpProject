using System;
using System.Threading;

namespace NimePipeServer
{
    internal class Program
    {
        static void Main(string[] args)
        {
            // 改用传统using代码块（替代using var）
            using (var pipeServer = new PipeServerService())
            {
                // 订阅事件
                pipeServer.MessageReceived += OnMessageReceived;
                pipeServer.Log += OnLogReceived;

                // 启动服务器
                pipeServer.Start();
                Console.WriteLine("管道服务器已启动，等待客户端连接...");

                // 保持程序运行
                Console.WriteLine("按任意键停止服务器...");
                Console.ReadLine();
            } // 离开代码块时自动调用pipeServer.Dispose()
        }

        // 消息接收事件处理
        private static void OnMessageReceived(PipeMessage msg)
        {
            Console.WriteLine($"[消息事件] 收到命令: {msg.Command}, 数据: {msg.Data ?? "无"}");
        }

        // 日志事件处理
        private static void OnLogReceived(string logMsg)
        {
            Console.WriteLine($"[日志] {logMsg}");
        }
    }
}