using ScreenRecordingTool.Models;

namespace ScreenRecordingTool.Services
{
    /// <summary>
    /// 录屏服务抽象，ViewModel 仅依赖接口，便于替换实现与单元测试。
    /// </summary>
    public interface IScreenRecorderService : IDisposable
    {
        /// <summary>是否正在录制。</summary>
        bool IsRecording { get; }

        /// <summary>
        /// 开始录制屏幕。
        /// </summary>
        /// <param name="settings">录制配置。</param>
        /// <param name="outputFilePath">输出文件完整路径。</param>
        void Start(RecordingSettings settings, string outputFilePath);

        /// <summary>
        /// 停止录制并完成文件写入。
        /// </summary>
        /// <returns>录制文件完整路径。</returns>
        Task<string> StopAsync();
    }
}
