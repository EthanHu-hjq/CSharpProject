using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TYM_DataUploadTool.Client;

namespace NamePipeClient
{
    class Program
    {
        static async Task Main(string[] args)
        {
            // 改用传统using代码块（兼容C# 7.3）
            using (var clientService = new PipeClientService())
            {
                await clientService.ConnectAsync();
                Console.WriteLine("已连接到服务器");

                // 发送测试消息
                var testMsg = new PipeMessage { Command = "Test", Data = "Hello Server" };
                await clientService.SendMessageAsync(testMsg);

                Console.WriteLine("按任意键退出...");
                Console.ReadKey();
            }
        }
    }
}
