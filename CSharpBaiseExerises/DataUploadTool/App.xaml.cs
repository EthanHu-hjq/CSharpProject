using System.Configuration;
using System.Data;
using System.Windows;

namespace DataUploadTool
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public IServiceProvider? serviceProvider { get; private set; }

    }

}
