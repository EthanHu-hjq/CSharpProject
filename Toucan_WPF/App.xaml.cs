using MahApps.Metro.Controls.Dialogs;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Documents;
//using ToucanCore.Engine;

namespace Toucan_WPF
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private static Mutex mutex;

        protected override void OnStartup(StartupEventArgs e)
        {
            bool running;
            mutex = new Mutex(true, "Toucan V2R0", out running);

            try
            {
                if (running)
                {
                    //Application.EnableVisualStyles();
                    //Application.SetCompatibleTextRenderingDefault(false);

                    //ToucanCore.Misc.SplashScreen.Show(typeof(ToucanCore.Misc.SplashForm));

                    var args = e.Args;
                    //TestEngine engine = null;
                    //try
                    //{
                    //    if (args.Length > 0)
                    //    {
                    //        if (args[0].ToCharArray()[0] != '-')  // Support File Extension Bundled
                    //        {
                    //            form.BasePath = args[0];
                    //        }
                    //    }

                    //    for (int i = 0; i < args.Length; i++)
                    //    {
                    //        switch (args[i].ToLower())
                    //        {
                    //            case "-path":
                    //                form.BasePath = args[i + 1];
                    //                break;

                    //            case "-model":
                    //                form.ModelPath = args[i + 1];
                    //                break;

                    //            case "-engine":
                    //                switch (args[i + 1].ToLower())
                    //                {
                    //                    case "ap":
                    //                        engine = new TYM_Apx_Engine.TYM_Apx_Engine();
                    //                        break;
                    //                    default:
                    //                        engine = new TYM_TS_Engine(form);
                    //                        break;
                    //                }
                    //                break;

                    //            case "-help":
                    //            case "-h":
                    //                PrintHelp();
                    //                break;
                    //        }
                    //    }
                    //}
                    //catch (Exception ex)
                    //{
                    //    TestCore.TF_Base.StaticLog(ex.ToString());
                    //    form.toolStripStatusLabel_Info.Text = string.Format("Arguments Error. Args: {0}", string.Join(" ", args));
                    //}

                    //if (engine is null)
                    //{
                    //    if (form.BasePath != null)
                    //    {
                    //        var ext = System.IO.Path.GetExtension(form.BasePath).ToLower();
                    //        switch (ext)
                    //        {
                    //            case ".appprojx":
                    //                engine = new TYM_Apx_Engine.TYM_Apx_Engine();
                    //                break;
                    //            default:
                    //                engine = new TYM_TS_Engine(form);
                    //                break;
                    //        }
                    //    }
                    //    else
                    //    {
                    //        engine = new TYM_TS_Engine(form);
                    //    }
                    //}

                    //form.InitializeEngine();
                    
                    base.OnStartup(e);
                }
                else
                {
                    MessageBox.Show("Already running a instance. 程序已打开", "Toucan", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), ex.GetType().ToString(), MessageBoxButton.OK, MessageBoxImage.Error);
                PrintHelp();
            }
        }

        protected override void OnExit(ExitEventArgs e)
        {
            mutex?.ReleaseMutex();
            mutex?.Dispose();
            base.OnExit(e);
        }

        static void PrintHelp()
        {
            Console.WriteLine("-model %path%. specify the model to %path%");
            Console.WriteLine("-path %path%. specify the test sequence file %path% to open");
            Console.WriteLine("-engine %engine%. specify test engine. default is ts (teststand). could be ap");

            Console.WriteLine("");
            Console.WriteLine("the following parameters only works when raven is available");
        }
    }
}
