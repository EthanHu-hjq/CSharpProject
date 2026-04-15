using System;
using System.Threading.Tasks;

namespace ProgramB
{
    /// <summary>
    /// 主程序类
    /// 使用工具类实现命名管道服务器
    /// </summary>
    public class Program
    {
        /// <summary>
        /// 程序入口点
        /// </summary>
        static async Task Main(string[] args)
        {
            Info("=== 命名管道服务器（重构版）===");
            Info("功能：可靠的消息通信，支持确认机制和超时重发");
            Info("按Ctrl+C可强制退出程序");

            try
            {
                // 创建并启动服务器工具实例
                var serverTool = new NamedPipeServerTool();
                await serverTool.StartServerAsync();
            }
            catch (Exception ex)
            {
                Info($"服务器异常终止: {ex.Message}");
                Info("按任意键退出...");
                Console.ReadKey();
            }
        }
    }
}
