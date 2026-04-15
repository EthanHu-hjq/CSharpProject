using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FFmpeg.AutoGenExample
{
    internal class Program
    {
        private static Dictionary<int, ScreenRecorder> _recordingTasks = new Dictionary<int, ScreenRecorder>();
        static void Main(string[] args)
        {
            Console.WriteLine("请输入要录制的屏幕编号（0-3），输入exit退出：");
            var input = Console.ReadLine();
            if (int.TryParse(input, out int screenIndex) && screenIndex >= 0 && screenIndex <= 3)
            {
                if (!_recordingTasks.ContainsKey(screenIndex))
                {
                    var recorder = new ScreenRecorder();
                    recorder.StartRecording(input, "11");
                    _recordingTasks.Add(screenIndex, recorder);
                    Console.WriteLine($"开始录制屏幕 {screenIndex}");
                }
                else
                {
                    Console.WriteLine($"屏幕 {screenIndex} 已经在录制中");
                }
            }
            else
            {
                Console.WriteLine("无效的输入，请输入0-3之间的数字或exit");
            }

            // 这里可以添加一些逻辑来停止录制，例如输入stop 0来停止录制屏幕0
            Console.WriteLine("==============================");
            Console.WriteLine("输入Q/q停止录屏");

            var stopInput = Console.ReadLine();
            if (stopInput.StartsWith("q"))
            {
                var parts = stopInput.Split(' ');
                if (parts.Length == 2 && int.TryParse(parts[1], out int stopScreenIndex) && _recordingTasks.ContainsKey(stopScreenIndex))
                {
                    _recordingTasks[stopScreenIndex].StopRecording();
                    _recordingTasks.Remove(stopScreenIndex);
                    Console.WriteLine($"停止录制屏幕 {stopScreenIndex}");
                }
                else
                {
                    Console.WriteLine("无效的停止命令，请输入stop [屏幕编号]");
                }
            }

            //释放资源
            if (stopInput.Equals("Q", StringComparison.OrdinalIgnoreCase))
            {
                foreach (var recorder in _recordingTasks.Values)
                {
                    recorder.StopRecording();
                }
                _recordingTasks.Clear();
                Console.WriteLine("已停止所有录制，退出程序");
            }

        }
    }
}
