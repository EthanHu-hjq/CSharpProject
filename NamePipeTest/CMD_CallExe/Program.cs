using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace CMD_CallExe
{
    internal class Program
    {
        [DllImport("kernel32.dll")]
        static extern IntPtr GetConsoleWindow();

        [DllImport("user32.dll")]
        static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        const int SW_HIDE = 0;

        public static void RunExe(string exePath,params string[] args)
        {
            var psi = new ProcessStartInfo
            {
                FileName = exePath,
                Arguments = string.Join(" ", args.Select(a => $"\"{a}\"")),
                UseShellExecute = true,
                Verb = "runas" // 以管理员权限运行
            };
            try
            {
                Process.Start(psi);
            }
            catch (System.ComponentModel.Win32Exception)
            {
                Console.WriteLine("用户拒绝提权，程序退出。");
            }
        }
        static void Main(string[] args)
        {
            var hwnd = GetConsoleWindow();
            string exePath = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "Bin",
                "TimeSyncTool.exe"
            );
            try
            {
                RunExe(exePath, "ftp5d.tymphany.com", "0.1");
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
            //Environment.Exit(0); // 立即退出，不等待
        }
    }
}
