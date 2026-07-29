namespace ScreenRecordingTool.Models
{
    /// <summary>
    /// 录制配置模型（示例 Model，仅存放数据，不包含界面逻辑）。
    /// </summary>
    public class RecordingSettings
    {
        /// <summary>帧率。</summary>
        public int FrameRate { get; set; } = 30;

        /// <summary>输出目录。</summary>
        public string OutputDirectory { get; set; } =
            Environment.GetFolderPath(Environment.SpecialFolder.MyVideos);

        /// <summary>是否录制系统声音。</summary>
        public bool CaptureAudio { get; set; } = true;
    }
}
