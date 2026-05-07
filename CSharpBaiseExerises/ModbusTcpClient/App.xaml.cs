using Microsoft.Extensions.DependencyInjection;
using ModbusTcpClient.Views;
using Serilog.Events;
using Serilog;
using System.Configuration;
using System.Data;
using System.Printing.IndexedProperties;
using System.Windows;
using Serilog.Sinks.File;

namespace ModbusTcpClient
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public App()
        {
            Log.Logger = new LoggerConfiguration()
               .MinimumLevel.Debug()
               .WriteTo.File(
                    "logs\\log-.txt",
                    rollingInterval: RollingInterval.Day,
                    restrictedToMinimumLevel: LogEventLevel.Information,
                    outputTemplate: "{Timestamp:yyyy - MM - dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
               .CreateLogger();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            Log.CloseAndFlush();
            base.OnExit(e);
        }
    }

}
