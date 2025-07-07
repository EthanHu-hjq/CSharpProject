using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting;
using System.Runtime.Serialization.Formatters;
using System.Security;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AudioPrecision.API;
using System.Runtime.Remoting.Channels.Tcp;
using System.Runtime.InteropServices;
using Microsoft.Win32;



namespace ApEngine
{
    //public static class ApxExt
    //{
    //    private static bool isChannelRegistered;
    //    private static string AppFileName
    //    {
    //        get
    //        {
    //            string executablePath = System.Windows.Forms.Application.ExecutablePath;
    //            int num = executablePath.LastIndexOfAny("/\\".ToCharArray());
    //            if (num > 0)
    //            {
    //                return executablePath.Substring(num + 1, executablePath.Length - num - 1);
    //            }
    //            throw new APException("AppHelper.AppFileName could not find a separator in the path", APError.InternalError);
    //        }
    //    }

    //    public static IApplication GetRemoteInstance(bool updateFirmware, APxOperatingMode mode, APxApplicationType appType)
    //    {
    //        string text = "RemotingClient." + AppFileName + ".config";
    //        if (!isChannelRegistered)
    //        {
    //            if (!string.IsNullOrEmpty(text) && File.Exists(text))
    //            {
    //                RemotingConfiguration.Configure(text, false);
    //            }
    //            else
    //            {
    //                BinaryServerFormatterSinkProvider serverSinkProvider = new BinaryServerFormatterSinkProvider
    //                {
    //                    TypeFilterLevel = TypeFilterLevel.Full
    //                };
    //                IDictionary dictionary = new Hashtable();
    //                dictionary["port"] = 0;
    //                dictionary["name"] = "APxClient";
    //                TcpChannel chnl = new TcpChannel(dictionary, null, serverSinkProvider);
    //                ChannelServices.RegisterChannel(chnl, false);
    //            }
    //            isChannelRegistered = true;
    //        }
    //        try
    //        {
    //            Assembly executingAssembly = Assembly.GetExecutingAssembly();
    //            AssemblyName name = executingAssembly.GetName();
    //            string text2 = string.Format("Software\\Audio Precision\\APx500\\{0}.{1}", 5, 0);// name.Version.Major, name.Version.Minor);
    //            if (name.Version.Revision > 0)
    //            {
    //                object[] customAttributes = executingAssembly.GetCustomAttributes(typeof(AssemblyConfigurationAttribute), false);
    //                if (customAttributes.Length == 1)
    //                {
    //                    AssemblyConfigurationAttribute assemblyConfigurationAttribute = (AssemblyConfigurationAttribute)customAttributes[0];
    //                    string text3 = " " + assemblyConfigurationAttribute.Configuration;
    //                    if (text3.Length > 1)
    //                    {
    //                        text2 += text3;
    //                    }
    //                }
    //            }
    //            ApiRegistryInfo apiRegistryInfo = ReadAppRegistryInfo(text2, "Location");
    //            if (string.IsNullOrEmpty(apiRegistryInfo.Path))
    //            {
    //                throw new APException("Could not read registry key.  Application is not properly installed please reinstall APx500.", APError.InternalError);
    //            }
    //            string path = apiRegistryInfo.Path;
    //            Process[] array = Process.GetProcessesByName(apiRegistryInfo.AppName);
    //            if (array.Length == 0)
    //            {
    //                ApiRegistryInfo apiRegistryInfo2 = ReadAppRegistryInfo(text2, "Location2");
    //                if (!string.IsNullOrEmpty(apiRegistryInfo2.Path))
    //                {
    //                    Process[] processesByName = Process.GetProcessesByName(apiRegistryInfo2.AppName);
    //                    if (processesByName.Length != 0)
    //                    {
    //                        array = processesByName;
    //                        path = apiRegistryInfo2.Path;
    //                    }
    //                }
    //            }
    //            if (array.Length == 0)
    //            {
    //                StringBuilder stringBuilder = new StringBuilder("/automation");
    //                if (updateFirmware)
    //                {
    //                    stringBuilder.Append(" /UpgradeBoot0");
    //                }
    //                if (mode != APxOperatingMode.BenchMode)
    //                {
    //                    if (mode != APxOperatingMode.SequenceMode)
    //                    {
    //                        throw new ArgumentOutOfRangeException();
    //                    }
    //                    stringBuilder.Append(" /SequenceMode");
    //                }
    //                else
    //                {
    //                    stringBuilder.Append(" /BenchTestMode");
    //                }
    //                if (appType == APxApplicationType.NoUserInterface)
    //                {
    //                    stringBuilder.Append(" /NoUi");
    //                }
    //                Process process = new Process
    //                {
    //                    StartInfo =
    //                    {
    //                        FileName = path,
    //                        Arguments = stringBuilder.ToString(),
    //                        WorkingDirectory = (Path.GetDirectoryName(path) ?? string.Empty),
    //                        CreateNoWindow = true,
    //                        UseShellExecute = true
    //                    }
    //                };
    //                try
    //                {
    //                    process.Start();
    //                }
    //                catch (Exception ex)
    //                {
    //                    throw new APException(ex.Message, APError.AutomationStartupError, ex);
    //                }
    //                while (!MemoryMappedFile.IsAPxReady("Local\\sharedMemoryAP_APx500_PortNum"))
    //                {
    //                    Thread.Sleep(500);
    //                    if (process.HasExited)
    //                    {
    //                        Thread.Sleep(500);
    //                        if (!MemoryMappedFile.IsAPxReady("Local\\sharedMemoryAP_APx500_PortNum"))
    //                        {
    //                            //throw APx500.MakeAPException(APError.AutomationStartupError);
    //                            throw new InvalidOperationException($"{APError.AutomationStartupError}");
    //                        }
    //                    }
    //                }
    //            }
    //            else
    //            {
    //                Process process2 = array[0];
    //            }
    //            //APx500.CheckVersion("Local\\sharedMemoryAP_APx500_Version", name.Version);
    //        }
    //        catch (SecurityException)
    //        {
    //            throw;
    //        }
    //        catch (IOException)
    //        {
    //            throw;
    //        }
    //        catch (OutOfMemoryException inner)
    //        {
    //            throw new APException("The application is not installed properly", APError.FatalError, inner);
    //        }
    //        catch (ArgumentException inner2)
    //        {
    //            throw new APException("The application is not installed properly", APError.FatalError, inner2);
    //        }
    //        int num = MemoryMappedFile.ReadInt("Local\\sharedMemoryAP_APx500_PortNum", true);
    //        IApplication application = (IApplication)Activator.GetObject(typeof(IApplication), string.Format("tcp://localhost:{0}/AudioPrecision.APx500", num));
    //        application.OperatingMode = mode;
    //        return application;
    //    }

    //    private static ApiRegistryInfo ReadAppRegistryInfo(string apiKeyName, string keyName)
    //    {
    //        string text = string.Empty;
    //        RegistryKey registryKey = Registry.LocalMachine.OpenSubKey(apiKeyName, false);
    //        if (registryKey != null)
    //        {
    //            object value = registryKey.GetValue(keyName);
    //            if (value != null)
    //            {
    //                text = value.ToString();
    //            }
    //            registryKey.Close();
    //        }
    //        return new ApiRegistryInfo
    //        {
    //            Path = text,
    //            AppName = (string.IsNullOrEmpty(text) ? string.Empty : Path.GetFileNameWithoutExtension(text))
    //        };
    //    }
    //}

    //public static class MemoryMappedFile
    //{
    //    // Token: 0x0600039E RID: 926
    //    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    //    private static extern IntPtr OpenFileMapping(int dwDesiredAccess, bool bInheritHandle, string lpName);

    //    // Token: 0x0600039F RID: 927
    //    [DllImport("Kernel32.dll")]
    //    private static extern IntPtr MapViewOfFile(IntPtr hFileMappingObject, uint dwDesiredAccess, uint dwFileOffsetHigh, uint dwFileOffsetLow, uint dwNumberOfBytesToMap);

    //    // Token: 0x060003A0 RID: 928
    //    [DllImport("Kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    //    private static extern bool UnmapViewOfFile(IntPtr lpBaseAddress);

    //    // Token: 0x060003A1 RID: 929
    //    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    //    private static extern bool CloseHandle(IntPtr hHandle);

    //    // Token: 0x060003A2 RID: 930 RVA: 0x0000A55D File Offset: 0x0000955D
    //    [SecuritySafeCritical]
    //    public static bool IsAPxReady(string fileName)
    //    {
    //        return MemoryMappedFile.ReadInt(fileName, false) != 0;
    //    }

    //    // Token: 0x060003A3 RID: 931 RVA: 0x0000A56C File Offset: 0x0000956C
    //    [SecuritySafeCritical]
    //    public static int ReadInt(string fileName, bool throwException)
    //    {
    //        IntPtr intPtr = MemoryMappedFile.OpenFileMapping(4, false, fileName);
    //        int result;
    //        try
    //        {
    //            if (intPtr == IntPtr.Zero)
    //            {
    //                if (throwException)
    //                {
    //                    //throw new SharedMemoryException("Opening the Shared Memory for Read failed.");
    //                }
    //                return 0;
    //            }
    //            else
    //            {
    //                IntPtr intPtr2 = MemoryMappedFile.MapViewOfFile(intPtr, 4U, 0U, 0U, 4U);
    //                if (intPtr2 == IntPtr.Zero)
    //                {
    //                    if (throwException)
    //                    {
    //                        //throw new SharedMemoryException("Creating a view of Shared Memory failed.");
    //                    }
    //                    return 0;
    //                }
    //                else
    //                {
    //                    result = Marshal.ReadInt32(intPtr2);
    //                    MemoryMappedFile.UnmapViewOfFile(intPtr2);
    //                }
    //            }
    //        }
    //        finally
    //        {
    //            if (intPtr != IntPtr.Zero)
    //            {
    //                bool flag = MemoryMappedFile.CloseHandle(intPtr);
    //            }
    //        }
    //        return result;
    //    }

    //    // Token: 0x060003A4 RID: 932 RVA: 0x0000A604 File Offset: 0x00009604
    //    [SecuritySafeCritical]
    //    public static int[] ReadInts(string fileName, int count, bool throwException)
    //    {
    //        int[] array = new int[count];
    //        IntPtr intPtr = MemoryMappedFile.OpenFileMapping(4, false, fileName);
    //        try
    //        {
    //            if (intPtr == IntPtr.Zero)
    //            {
    //                if (throwException)
    //                {
    //                    //throw new SharedMemoryException("Opening the Shared Memory for Read failed.");
    //                }
    //                return new int[1];
    //            }
    //            else
    //            {
    //                IntPtr intPtr2 = MemoryMappedFile.MapViewOfFile(intPtr, 4U, 0U, 0U, (uint)(count * 4));
    //                if (intPtr2 == IntPtr.Zero)
    //                {
    //                    if (throwException)
    //                    {
    //                        //throw new SharedMemoryException("Creating a view of Shared Memory failed.");
    //                    }
    //                    return new int[1];
    //                }
    //                else
    //                {
    //                    for (int i = 0; i < count; i++)
    //                    {
    //                        array[i] = Marshal.ReadInt32(intPtr2, i * 4);
    //                    }
    //                    MemoryMappedFile.UnmapViewOfFile(intPtr2);
    //                }
    //            }
    //        }
    //        finally
    //        {
    //            if (intPtr != IntPtr.Zero)
    //            {
    //                bool flag = MemoryMappedFile.CloseHandle(intPtr);
    //            }
    //        }
    //        return array;
    //    }

    //    // Token: 0x04000F50 RID: 3920
    //    private const int FILE_MAP_READ = 4;

    //    // Token: 0x0200043F RID: 1087
    //    public enum FileAccess
    //    {
    //        // Token: 0x04001238 RID: 4664
    //        ReadOnly = 2,
    //        // Token: 0x04001239 RID: 4665
    //        ReadWrite = 4
    //    }
    //}

    //public struct ApiRegistryInfo
    //{
    //    // Token: 0x04001235 RID: 4661
    //    public string Path;

    //    // Token: 0x04001236 RID: 4662
    //    public string AppName;
    //}

    //public enum APxApplicationType
    //{
    //    // Token: 0x04000F49 RID: 3913
    //    Normal,
    //    // Token: 0x04000F4A RID: 3914
    //    NoUserInterface
    //}
}
