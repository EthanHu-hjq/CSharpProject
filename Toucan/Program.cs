using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Threading;
using System.IO;
using ToucanCore.Engine;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Toucan
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            bool running;
            //TestCore.TF_Base.StaticLog("Program Starting"); // Will no log in Log File

            Mutex run = null;
            try
            {
                do
                {
                    if (Mutex.TryOpenExisting("Toucan", out run))
                    {
                    }
                    else
                    {
                        run = new Mutex(true, "Toucan", out running);
                    }

                    if (run.WaitOne(3000)) break; // For low performance PC, make the timeout a little longer

                    if (MessageBox.Show("Already running an instance. Click Yes to QUIT, or No to Kill Toucan process and start", "Warning", MessageBoxButtons.YesNo) == DialogResult.Yes)
                    {
                        return;
                    }

                    var curr = Process.GetCurrentProcess();

                    var ps = Process.GetProcessesByName(curr.ProcessName);

                    foreach (var p in ps)
                    {
                        if (p.Id != curr.Id)
                        {
                            using (var process = new Process())
                            {
                                process.StartInfo.FileName = "taskkill";
                                process.StartInfo.Arguments = $"/F /PID {p.Id}";

                                process.Start();
                            }
                        }
                    }

                    run.Dispose();
                }
                while (true);

                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);

                ToucanCore.Misc.SplashScreen.Show(typeof(ToucanCore.Misc.SplashForm));

                var form = new Toucan();
                try
                {
                    if (args.Length > 0)
                    {
                        if (args[0].ToCharArray()[0] != '-')  // Support File Extension Bundled
                        {
                            form.BasePath = args[0];
                        }
                    }

                    //string remoteseq = null;
                    //remote.Station = null;
                    for (int i = 0; i < args.Length; i++)
                    {
                        switch (args[i].ToLower())
                        {
                            case "-path":
                                form.BasePath = args[i + 1];
                                break;

                            //case "-model":
                            //    form.ModelPath = args[i + 1];
                            //    break;

                            //case "-bu":
                            //    remote.BU = args[i + 1];
                            //    break;

                            //case "-customer":
                            //    remote.Customer = args[i + 1];
                            //    break;

                            //case "-project":
                            //    remote.Project = args[i + 1];
                            //    break;

                            //case "-product":
                            //    remote.Product = args[i + 1];
                            //    break;

                            //case "-station":
                            //    remote.Station = args[i + 1];
                            //    break;

                            //case "-seq":
                            //    remoteseq = args[i + 1];
                            //break;

                            case "-engine":
                                switch (args[i + 1].ToLower())
                                {
                                    //case "ap":
                                    //    form.Engine = new TYM_Apx_Engine.TYM_Apx_Engine();
                                    //    break;
                                    default:
                                        form.Engine = new TYM_TS_Engine(form);
                                        break;
                                }
                                break;

                            case "-unlock":
                                form.Unlock = true;
                                break;

                            case "-help":
                            case "-h":
                                PrintHelp();
                                break;
                        }
                    }
                }
                catch (Exception ex)
                {
                    TestCore.TF_Base.StaticLog(ex.ToString());
                    form.toolStripStatusLabel_Info.Text = string.Format("Arguments Error. Args: {0}", string.Join(" ", args));
                }

                if (form.Engine is null)
                {
                    if (form.BasePath != null)
                    {
                        var ext = Path.GetExtension(form.BasePath).ToLower();
                        switch (ext)
                        {
                            //case ".appprojx":
                            //    form.Engine = new TYM_Apx_Engine.TYM_Apx_Engine();
                            //    break;
                            default:
                                form.Engine = new TYM_TS_Engine(form);
                                break;
                        }
                    }
                    else
                    {
                        form.Engine = new TYM_TS_Engine(form);
                    }
                }

                form.InitializeEngine();

                Application.Run(form);
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.ToString(), ex.GetType().ToString(), MessageBoxButtons.OK, MessageBoxIcon.Error);
                PrintHelp();
            }
            finally
            {
                //run?.ReleaseMutex();   //Mutex will be released after process done, skip it for the process might not get the mutex

                //using (System.Diagnostics.Process process = new System.Diagnostics.Process())
                //{
                //    process.StartInfo.FileName = "taskkill.exe";
                //    process.StartInfo.Arguments = string.Format("\\F \\IM \"{0}\"", Application.ProductName);
                //    process.StartInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;

                //    process.Start();
                //}
            }
        }

        static void PrintHelp()
        {
            Console.WriteLine("-hidden. Hide the form when program start");
            Console.WriteLine("-model %path%. specify the model to %path%");
            Console.WriteLine("-path %path%. specify the test sequence file %path% to open");
            Console.WriteLine("-autoexit. application will exit when seq close");
            Console.WriteLine("-engine %engine%. specify test engine. default is ts (teststand). could be ap");

            Console.WriteLine("");
            Console.WriteLine("the following parameters only works when raven is available");

            Console.WriteLine("-bu %bu%. make current BU to be %bu%");
            Console.WriteLine("-customer %customer%. make current CUSTOMER to be %customer%");
            Console.WriteLine("-project %project%. make current PROJECT to be %project%");
            Console.WriteLine("-product %product%. make current PRODUCT to be %product%. if null, the product would be the first product of project");
            Console.WriteLine("-station %station%. make current STATION to be %station%");

            Console.WriteLine("-bu %bu% -customer %customer% -project %project% -product %product% -station %station%. download the effective version of specific config");
            Console.WriteLine("-bu %bu% -customer %customer% -project %project% -station %station% -seq %seqfilename%. download the specific version");
        }
    }
}
