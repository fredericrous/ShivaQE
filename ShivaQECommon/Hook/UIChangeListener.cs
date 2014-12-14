using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Windows.Interop;
using System.Diagnostics;
using System.Windows;
using ShivaQEcommon.Eventdata;

namespace ShivaQEcommon.Hook
{
    /// <summary>
    /// Notify whenever there's a windows system events about UI.
    /// See following lists of events for details: wndprocCalls, msgCalls, shellCalls
    /// </summary>
    public class UIChangeListener
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger
        (System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        [DllImport("user32.dll", CharSet = CharSet.Auto, ExactSpelling = true)]
        public static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        private const int SW_MAXIMIZE = 3;

        [DllImport("user32.dll", SetLastError = true)]
        internal static extern bool MoveWindow(IntPtr hWnd, int X, int Y, int nWidth, int nHeight, bool bRepaint);

        public static readonly uint SWP_ASYNCWINDOWPOS = 0x4000;
        public static readonly IntPtr HWND_TOP = new IntPtr(0);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int W, int H, uint uFlags);

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left;        // x position of upper-left corner
            public int Top;         // y position of upper-left corner
            public int Right;       // x position of lower-right corner
            public int Bottom;      // y position of lower-right corner
        }

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect); //IntPtr or HandleRef!?

        public delegate void WindowCreatedEvent();
        public event WindowCreatedEvent WindowCreated;

        private GlobalHooks _globalHook;

        private List<string> _eventCalls = new List<string>();

        public List<string> getEventCalls
        {
            get { return _eventCalls; }
        }

        public UIChangeListener(Window mainWindow)
        {
            _globalHook = new GlobalHooks((new WindowInteropHelper(mainWindow)).Handle);

            _globalHook.CallWndProc.CallWndProc += (IntPtr Handle, IntPtr Message, IntPtr wParam, IntPtr lParam) =>
            {
                if (Enum.GetValues(typeof(HookEvent.WndprocCalls)).Cast<HookEvent.WndprocCalls>()
                    .Any(x => Convert.ToInt32(x) == Message.ToInt32()))
                {
                    _eventCalls.Add(Enum.GetName(typeof(HookEvent.WndprocCalls), Message.ToInt32()));
                }
            };

            _globalHook.GetMsg.GetMsg += (IntPtr Handle, IntPtr Message, IntPtr wParam, IntPtr lParam) =>
            {
                if (Enum.GetValues(typeof(HookEvent.MsgCalls)).Cast<HookEvent.MsgCalls>()
                   .Any(x => Convert.ToInt32(x) == Message.ToInt32()))
                {
                    _eventCalls.Add(Enum.GetName(typeof(HookEvent.MsgCalls), Message.ToInt32()));
                }
            };

            _globalHook.CBT.Activate += (IntPtr ptr) => { _eventCalls.Add(Enum.GetName(typeof(HookEvent.CbtCalls), HookEvent.CbtCalls.CBTActivate)); };
            _globalHook.CBT.CreateWindow += (IntPtr ptr) =>
            {
                _eventCalls.Add(Enum.GetName(typeof(HookEvent.CbtCalls), HookEvent.CbtCalls.CBTCreateWindow));
                WindowCreated();
            };
            _globalHook.CBT.DestroyWindow += (IntPtr ptr) => { _eventCalls.Add(Enum.GetName(typeof(HookEvent.CbtCalls), HookEvent.CbtCalls.CBTDestroyWindow)); };
            _globalHook.CBT.MinMax += (IntPtr ptr) => { _eventCalls.Add(Enum.GetName(typeof(HookEvent.CbtCalls), HookEvent.CbtCalls.CBTMinMax)); };
            _globalHook.CBT.SetFocus += (IntPtr ptr) => { _eventCalls.Add(Enum.GetName(typeof(HookEvent.CbtCalls), HookEvent.CbtCalls.CBTSetFocus)); };
            _globalHook.CBT.MoveSize += (IntPtr ptr) => { _eventCalls.Add(Enum.GetName(typeof(HookEvent.CbtCalls), HookEvent.CbtCalls.CBTMoveSize)); };
            
            //globalHook.Shell.ActivateShellWindow += () => { eventCalls.Add("SHELLActivateShellWindow"); };
            //globalHook.Shell.GetMinRect += (IntPtr ptr) => { eventCalls.Add("SHELLGetMinRect"); };
            _globalHook.Shell.Redraw += (IntPtr ptr) => { _eventCalls.Add(Enum.GetName(typeof(HookEvent.ShellCalls), HookEvent.ShellCalls.HSHELL_REDRAW)); };
            //globalHook.Shell.WindowActivated += (IntPtr ptr) => { eventCalls.Add("SHELLWindowActivated"); };
            _globalHook.Shell.WindowCreated += (IntPtr ptr) => { _eventCalls.Add(Enum.GetName(typeof(HookEvent.ShellCalls), HookEvent.ShellCalls.HSHELL_WINDOWCREATED)); };
            _globalHook.Shell.WindowDestroyed += (IntPtr ptr) => { _eventCalls.Add(Enum.GetName(typeof(HookEvent.ShellCalls), HookEvent.ShellCalls.HSHELL_WINDOWDESTROYED)); };


            _globalHook.CallWndProc.Start();
            _globalHook.GetMsg.Start();
            _globalHook.CBT.Start();
            _globalHook.Shell.Start();
        }

        public void StopListener()
        {
            _globalHook.CallWndProc.Stop();
            _globalHook.GetMsg.Stop();
            _globalHook.CBT.Stop();
            _globalHook.Shell.Stop();
        }


        public void maximizeWindow()
        {
            ShowWindow(GetForegroundWindow(), SW_MAXIMIZE);
        }

        public void setActiveWindowInfo(string[] windowPos)
        {
            int Left = int.Parse(windowPos[0]);
            int Top = int.Parse(windowPos[1]);
            int Width = int.Parse(windowPos[2]);
            int Heigh = int.Parse(windowPos[3]);
            SetWindowPos(GetForegroundWindow(), HWND_TOP, Left, Top, Width, Heigh, SWP_ASYNCWINDOWPOS);
        }

        public int[] getActiveWindowInfo()
        {
            RECT rct;

            if (!GetWindowRect(GetForegroundWindow(), out rct))
            {
                log.Warn("error getting active window");
                return null;
            }

            int width = rct.Right - rct.Left + 1;
            int height = rct.Bottom - rct.Top + 1;

            int[] rcValues = { rct.Left, rct.Top, width, height };
            return rcValues;
        }
    }
}
