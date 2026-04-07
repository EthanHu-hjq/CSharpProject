using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProgramA
{
    public class ProcessManager
    {
        /// <summary>
        /// Checks if a process with the specified name is currently running on the system.
        /// </summary>
        /// <param name="processName"></param>
        /// <returns></returns>
        public static bool IsProcessRunning(string processName)
        {
            var processes = System.Diagnostics.Process.GetProcessesByName(processName);
            return processes.Length > 0;
        }

        /// <summary>
        /// Starts a process with the specified path. If the process fails to start, it catches the exception and prints an error message to the console.
        /// </summary>
        /// <param name="processPath"></param>
        public static void StartProcess(string processPath)
        {
            try
            {
                if(!File.Exists(processPath))
                {
                    Console.WriteLine($"Error: The file '{processPath}' does not exist.");
                    return;
                }

                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = processPath,
                    CreateNoWindow = true,
                    UseShellExecute = true,
                    WindowStyle = ProcessWindowStyle.Normal
                };
                Process.Start(startInfo);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error starting process: {ex.Message}");
            }
        }
    }
}
