using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using static ToucanCore.Engine.WindowsApi;

namespace ToucanCore.Engine
{
    public static class WindowsApi
    {
        /// <summary>
        /// 找到窗口
        /// </summary>
        /// <param name="lpClassName">窗口类名(例：Button)</param>
        /// <param name="lpWindowName">窗口标题</param>
        /// <returns></returns>
        [DllImport("user32.dll", EntryPoint = "FindWindow")]
        public extern static IntPtr FindWindow(string lpClassName, string lpWindowName);

        /// <summary>
        /// 找到窗口
        /// </summary>
        /// <param name="hwndParent">父窗口句柄（如果为空，则为桌面窗口）</param>
        /// <param name="hwndChildAfter">子窗口句柄（从该子窗口之后查找）</param>
        /// <param name="lpszClass">窗口类名(例：Button</param>
        /// <param name="lpszWindow">窗口标题</param>
        /// <returns></returns>
        [DllImport("user32.dll", EntryPoint = "FindWindowEx")]
        public extern static IntPtr FindWindowEx(IntPtr hwndParent, IntPtr hwndChildAfter, string lpszClass, string lpszWindow);

        /// <summary>
        /// 设置父窗口
        /// </summary>
        /// <param name="hwndChild">子窗口句柄</param>
        /// <param name="hwndParent">父窗口句柄（如果为空，则为桌面窗口）</param>
        /// <returns></returns>
        [DllImport("user32.dll", EntryPoint = "SetParent")]
        public extern static IntPtr SetParent(IntPtr hwndChild, IntPtr hwndParent);

        [DllImport("user32.dll", EntryPoint = "WindowFromPoint")]
        public extern static IntPtr WindowFromPoint(int x, int y);

        /// <summary>
        /// 发送消息
        /// </summary>
        /// <param name="hwnd">消息接受窗口句柄</param>
        /// <param name="wMsg">消息</param>
        /// <param name="wParam">指定附加的消息特定信息</param>
        /// <param name="lParam">指定附加的消息特定信息</param>
        /// <returns></returns>
        [DllImport("user32.dll", EntryPoint = "SendMessageA")]
        public static extern int SendMessage(IntPtr hwnd, uint wMsg, int wParam, int lParam);

        //窗口发送给按钮控件的消息，让按钮执行点击操作，可以模拟按钮点击
        public const int BM_CLICK = 0xF5;

        [StructLayout(LayoutKind.Sequential)]
        public class POINT

        {
            public int x;

            public int y;

        }

        [StructLayout(LayoutKind.Sequential)]
        public class MouseHookStruct
        {

            public POINT pt;

            public int hwnd;

            public int wHitTestCode;

            public int dwExtraInfo;

        }

        public delegate int HookProc(int nCode, IntPtr wParam, IntPtr lParam);

        //安装钩子

        [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]

        public static extern int SetWindowsHookEx(int idHook, HookProc lpfn, IntPtr hInstance, int threadId);

        //卸载钩子

        [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]

        public static extern bool UnhookWindowsHookEx(int idHook);

        //调用下一个钩子

        [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]

        public static extern int CallNextHookEx(int idHook, int nCode, IntPtr wParam, IntPtr lParam);
    }

    public class MouseHook

    {

        private System.Drawing.Point point;

        private System.Drawing.Point Point

        {

            get { return point; }

            set

            {

                if (point != value)

                {

                    point = value;

                    if (MouseMoveEvent != null)

                    {

                        var e = new System.Windows.Forms.MouseEventArgs(MouseButtons.None, 0, point.X, point.Y, 0);

                        MouseMoveEvent(this, e);

                    }

                }

            }

        }

        private int hHook;

        public const int WH_MOUSE_LL = 14;

        public WindowsApi.HookProc hProc;

        public MouseHook() { this.Point = new System.Drawing.Point(); }

        public int SetHook()

        {

            hProc = new WindowsApi.HookProc(MouseHookProc);

            hHook = WindowsApi.SetWindowsHookEx(WH_MOUSE_LL, hProc, IntPtr.Zero, 0);

            return hHook;

        }

        public void UnHook()

        {

            WindowsApi.UnhookWindowsHookEx(hHook);

        }

        private int MouseHookProc(int nCode, IntPtr wParam, IntPtr lParam)

        {

            WindowsApi.MouseHookStruct MyMouseHookStruct = (WindowsApi.MouseHookStruct)Marshal.PtrToStructure(lParam, typeof(WindowsApi.MouseHookStruct));

            if (nCode < 0)

            {

                return WindowsApi.CallNextHookEx(hHook, nCode, wParam, lParam);

            }

            else

            {

                this.Point = new System.Drawing.Point(MyMouseHookStruct.pt.x, MyMouseHookStruct.pt.y);

                return WindowsApi.CallNextHookEx(hHook, nCode, wParam, lParam);

            }

        }

        //委托+事件（把钩到的消息封装为事件，由调用者处理）

        public delegate void MouseMoveHandler(object sender, System.Windows.Forms.MouseEventArgs e);

        public event MouseMoveHandler MouseMoveEvent;

    }

    public abstract class MouseSim
    {
        public abstract string ExePath { get; }
        public abstract string WindowClass { get; }
        public abstract string WindowName { get; }
        public abstract string[] ControlHierarchy { get; }

        protected IntPtr ExeWindow { get; private set; } = IntPtr.Zero;
        public int ShowExeWindow(System.Windows.Forms.Panel panel)
        {
            return 1;
        }

        public int Initialize()
        {
            ExeWindow = WindowsApi.FindWindow(WindowClass, WindowName);

            if(ExeWindow == IntPtr.Zero)
            {
                using (Process process = new Process())
                {
                    process.StartInfo.FileName = ExePath;

                    process.Start();

                    Mouse.SetCursor(System.Windows.Input.Cursors.Wait);

                    do
                    {
                        Thread.Sleep(50);
                        ExeWindow = WindowsApi.FindWindow(WindowClass, WindowName);
                    }
                    while (ExeWindow == IntPtr.Zero);
                    Mouse.SetCursor(System.Windows.Input.Cursors.Arrow);
                }
            }

            return 1;
        }

        public int Clear()
        {
            return 1;
        }

        public int Start()
        {
            return 1;
        }

        public int Stop(int timeout_ms =-1)
        {
            return 1;
        }
    }
}
