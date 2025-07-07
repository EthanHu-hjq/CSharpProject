using Robin.Core;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Xml.Serialization;
using TestCore.Base;
using TestCore.Services;

namespace Robin
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hwnd, int ncmdshow);

        public static HardwareControl HardwareDefinition { get; private set; }
        public static GlobalGroupSetting GroupSetting { get; private set; }

        public static string Workbase { get; } = Path.Combine(ServiceStatic.AppDir, "Robin");
        public static string CommonFileDir { get; } = Path.Combine(Workbase, "Data");
        public static string TemplateDir { get; } = Path.Combine(App.Workbase, "Template");

        public static string HardwareDefinitionPath { get; } = Path.Combine(Workbase, "Equipment.xml");
        public static string GroupDefinitionPath { get; } = System.IO.Path.Combine(Workbase, "GlobalItem.xml");

        private Mutex mutex = null;
        public App()
        {
            bool instance = false;
            
            string name = System.Reflection.Assembly.GetExecutingAssembly().GetName().Name;
            if(Mutex.TryOpenExisting(name, out mutex))
            {
                Process[] processes = Process.GetProcessesByName(Path.GetFileNameWithoutExtension(name));
                if (processes.FirstOrDefault() is Process ps)
                {
                    ShowWindow(ps.MainWindowHandle, 1);
                    Shutdown();
                    return;
                }
            }
            else
            {
                mutex = new Mutex(true, name, out instance);
            }

            if (!instance)
            {
                
            }
        }

        protected override void OnExit(ExitEventArgs e)
        {
            mutex?.Close();
            mutex?.Dispose();
            base.OnExit(e);
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            

            try
            {
                if (!Directory.Exists(Workbase))
                {
                    Directory.CreateDirectory(Workbase);
                    Directory.CreateDirectory(CommonFileDir);
                }
                if (!File.Exists(GroupDefinitionPath))
                {
                    File.Copy(Path.Combine(AppContext.BaseDirectory, "GlobalItem.xml"), GroupDefinitionPath);
                }

                try
                {
                    HardwareDefinition = HardwareControl.FromFile(App.HardwareDefinitionPath);
                }
                catch
                {
                    HardwareDefinition = new HardwareControl();
                }

                if (!File.Exists(HardwareDefinitionPath))
                {
                    File.Copy(Path.Combine(AppContext.BaseDirectory, "Equipment.xml"), HardwareDefinitionPath);
                }

                try
                {
                    GroupSetting = GlobalGroupSetting.FromFile(App.GroupDefinitionPath);
                }
                catch
                {
                    GroupSetting = GlobalGroupSetting.GetDefault();
                }
            }
            catch
            {
            }
            
            base.OnStartup(e);
        }
    }
}
