using System.Windows;
using ScreenRecordingTool.ViewModels;
using ScreenRecordingTool.Views;

namespace ScreenRecordingTool
{
    /// <summary>
    /// 应用程序入口：组合根（Composition Root），在此完成 View 与 ViewModel 的装配。
    /// </summary>
    public partial class App : Application
    {
        private MainViewModel? _mainViewModel;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            _mainViewModel = new MainViewModel();
            var mainWindow = new MainWindow
            {
                DataContext = _mainViewModel
            };
            mainWindow.Show();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            // 边界处理：退出时若仍在录制，确保 AVI 文件写入索引并正确收尾
            _mainViewModel?.Dispose();
            base.OnExit(e);
        }
    }
}
