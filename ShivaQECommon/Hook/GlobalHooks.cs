using System;
using System.Runtime.InteropServices;
using System.Windows.Interop;

namespace ShivaQEcommon.Hook
{
    /// <summary>
    /// Hooks to windows system events.
    /// This class depends on GlobalCbtHook.dll
    /// it uses the shared memory version from: http://legacyofvoid.wordpress.com/2011/11/16/global-system-hooks-in-c/
    /// original code comes from : http://www.codeproject.com/Articles/18638/Using-Window-Messages-to-Implement-Global-System-H
    /// </summary>
    public class GlobalHooks : IDisposable {
        public delegate void WindowEventHandler(IntPtr Handle);
        public delegate void SysCommandEventHandler(int SysCommand, int lParam);
        public delegate void ActivateShellWindowEventHandler();
        public delegate void TaskmanEventHandler();
        public delegate void BasicHookEventHandler(IntPtr Handle1, IntPtr Handle2);
        public delegate void WndProcEventHandler(IntPtr Handle, IntPtr Message, IntPtr wParam, IntPtr lParam);

        // Functions imported from our unmanaged DLL
        [DllImport("GlobalCbtHook.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern bool InitializeCbtHook(int threadID, IntPtr DestWindow);
        [DllImport("GlobalCbtHook.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern void UninitializeCbtHook();
        [DllImport("GlobalCbtHook.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern bool InitializeShellHook(int threadID, IntPtr DestWindow);
        [DllImport("GlobalCbtHook.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern void UninitializeShellHook();
        [DllImport("GlobalCbtHook.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern void InitializeCallWndProcHook(int threadID, IntPtr DestWindow);
        [DllImport("GlobalCbtHook.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern void UninitializeCallWndProcHook();
        [DllImport("GlobalCbtHook.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern void InitializeGetMsgHook(int threadID, IntPtr DestWindow);
        [DllImport("GlobalCbtHook.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern void UninitializeGetMsgHook();

        // API call needed to retreive the value of the messages to intercept from the unmanaged DLL
        [DllImport("user32.dll")]
        private static extern int RegisterWindowMessage(string lpString);
        [DllImport("user32.dll")]
        private static extern IntPtr GetProp(IntPtr hWnd, string lpString);
        [DllImport("user32.dll")]
        private static extern IntPtr GetDesktopWindow();

        // Handle of the window intercepting messages
        private IntPtr _Handle;

        private CBTHook _CBT;
        private ShellHook _Shell;
        private CallWndProcHook _CallWndProc;
        private GetMsgHook _GetMsg;
        private HwndSource source;
        private bool dispose = false;

        public GlobalHooks(IntPtr Handle) {
            _Handle = Handle;

            source = HwndSource.FromHwnd(Handle);
            source.AddHook(WndProc);

            _CBT = new CBTHook(_Handle);
            _Shell = new ShellHook(_Handle);
            _CallWndProc = new CallWndProcHook(_Handle);
            _GetMsg = new GetMsgHook(_Handle);
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled) {

            _CBT.ProcessWindowMessage(msg, wParam, lParam);
            _Shell.ProcessWindowMessage(msg, wParam, lParam);
            _CallWndProc.ProcessWindowMessage(msg, wParam, lParam);
            _GetMsg.ProcessWindowMessage(msg, wParam, lParam);

            return IntPtr.Zero;
        }

        public void Dispose() {
            if (dispose) return;
            dispose = true;

            _CBT.Stop();
            _Shell.Stop();
            _CallWndProc.Stop();
            _GetMsg.Stop();

            if (source != null) {
                source.RemoveHook(WndProc);
                source.Dispose();
            }
        }

        ~GlobalHooks() {
            Dispose();
        }

        #region Accessors

        public CBTHook CBT {
            get { return _CBT; }
        }

        public ShellHook Shell {
            get { return _Shell; }
        }

        public CallWndProcHook CallWndProc {
            get { return _CallWndProc; }
        }

        public GetMsgHook GetMsg {
            get { return _GetMsg; }
        }

        #endregion

        public abstract class Hook {
            protected bool _IsActive = false;
            protected IntPtr _Handle;

            public Hook(IntPtr Handle) {
                _Handle = Handle;
            }

            public void Start() {
                if (!_IsActive) {
                    _IsActive = true;
                    try
                    {
                        OnStart();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("hook: " + ex.Message);
                    }
                }
            }

            public void Stop() {
                if (_IsActive) {
                    try
                    {
                        OnStop();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                    }
                    _IsActive = false;
                }
            }

            ~Hook() {
                Stop();
            }

            public bool IsActive {
                get { return _IsActive; }
            }

            protected abstract void OnStart();
            protected abstract void OnStop();
            public abstract void ProcessWindowMessage(int msg, IntPtr wParam, IntPtr lParam);
        }

        public class CBTHook : Hook {
            // Values retreived with RegisterWindowMessage
            //private int _MsgID_CBT_HookReplaced;
            private int _MsgID_CBT_Activate;
            private int _MsgID_CBT_CreateWnd;
            private int _MsgID_CBT_DestroyWnd;
            private int _MsgID_CBT_MinMax;
            private int _MsgID_CBT_MoveSize;
            private int _MsgID_CBT_SetFocus;
            private int _MsgID_CBT_SysCommand;

            public event WindowEventHandler Activate;
            public event WindowEventHandler CreateWindow;
            public event WindowEventHandler DestroyWindow;
            public event WindowEventHandler MinMax;
            public event WindowEventHandler MoveSize;
            public event WindowEventHandler SetFocus;
            public event SysCommandEventHandler SysCommand;

            public CBTHook(IntPtr Handle)
                : base(Handle) {
            }

            protected override void OnStart() {
                // Retreive the message IDs that we'll look for in WndProc
                _MsgID_CBT_Activate = RegisterWindowMessage("SHIVAQE_HOOK_HCBT_ACTIVATE");
                _MsgID_CBT_CreateWnd = RegisterWindowMessage("SHIVAQE_HOOK_HCBT_CREATEWND");
                _MsgID_CBT_DestroyWnd = RegisterWindowMessage("SHIVAQE_HOOK_HCBT_DESTROYWND");
                _MsgID_CBT_MinMax = RegisterWindowMessage("SHIVAQE_HOOK_HCBT_MINMAX");
                _MsgID_CBT_MoveSize = RegisterWindowMessage("SHIVAQE_HOOK_HCBT_MOVESIZE");
                _MsgID_CBT_SetFocus = RegisterWindowMessage("SHIVAQE_HOOK_HCBT_SETFOCUS");
                _MsgID_CBT_SysCommand = RegisterWindowMessage("SHIVAQE_HOOK_HCBT_SYSCOMMAND");

                // Start the hook
                InitializeCbtHook(0, _Handle);
            }

            protected override void OnStop() {
                UninitializeCbtHook();
            }

            public override void ProcessWindowMessage(int msg, IntPtr wParam, IntPtr lParam) {
                if (msg == _MsgID_CBT_Activate) {
                    if (Activate != null)
                        Activate(wParam);
                }
                else if (msg == _MsgID_CBT_CreateWnd) {
                    if (CreateWindow != null)
                        CreateWindow(wParam);
                }
                else if (msg == _MsgID_CBT_DestroyWnd) {
                    if (DestroyWindow != null)
                        DestroyWindow(wParam);
                }
                else if (msg == _MsgID_CBT_MinMax) {
                    if (MinMax != null)
                        MinMax(wParam);
                }
                else if (msg == _MsgID_CBT_MoveSize) {
                    if (MoveSize != null)
                        MoveSize(wParam);
                }
                else if (msg == _MsgID_CBT_SetFocus) {
                    if (SetFocus != null)
                        SetFocus(wParam);
                }
                else if (msg == _MsgID_CBT_SysCommand) {
                    if (SysCommand != null)
                        SysCommand(wParam.ToInt32(), lParam.ToInt32());
                }
            }
        }

        public class ShellHook : Hook {
            // Values retreived with RegisterWindowMessage
            private int _MsgID_Shell_ActivateShellWindow;
            private int _MsgID_Shell_GetMinRect;
            private int _MsgID_Shell_Language;
            private int _MsgID_Shell_Redraw;
            private int _MsgID_Shell_Taskman;
            //private int _MsgID_Shell_HookReplaced;
            private int _MsgID_Shell_WindowActivated;
            private int _MsgID_Shell_WindowCreated;
            private int _MsgID_Shell_WindowDestroyed;

            public event ActivateShellWindowEventHandler ActivateShellWindow;
            public event WindowEventHandler GetMinRect;
            public event WindowEventHandler Language;
            public event WindowEventHandler Redraw;
            public event TaskmanEventHandler Taskman;
            public event WindowEventHandler WindowActivated;
            public event WindowEventHandler WindowCreated;
            public event WindowEventHandler WindowDestroyed;

            public ShellHook(IntPtr Handle)
                : base(Handle) {
            }

            protected override void OnStart() {
                // Retreive the message IDs that we'll look for in WndProc
                _MsgID_Shell_ActivateShellWindow = RegisterWindowMessage("SHIVAQE_HOOK_HSHELL_ACTIVATESHELLWINDOW");
                _MsgID_Shell_GetMinRect = RegisterWindowMessage("SHIVAQE_HOOK_HSHELL_GETMINRECT");
                _MsgID_Shell_Language = RegisterWindowMessage("SHIVAQE_HOOK_HSHELL_LANGUAGE");
                _MsgID_Shell_Redraw = RegisterWindowMessage("SHIVAQE_HOOK_HSHELL_REDRAW");
                _MsgID_Shell_Taskman = RegisterWindowMessage("SHIVAQE_HOOK_HSHELL_TASKMAN");
                _MsgID_Shell_WindowActivated = RegisterWindowMessage("SHIVAQE_HOOK_HSHELL_WINDOWACTIVATED");
                _MsgID_Shell_WindowCreated = RegisterWindowMessage("SHIVAQE_HOOK_HSHELL_WINDOWCREATED");
                _MsgID_Shell_WindowDestroyed = RegisterWindowMessage("SHIVAQE_HOOK_HSHELL_WINDOWDESTROYED");

                // Start the hook
                InitializeShellHook(0, _Handle);
            }

            protected override void OnStop() {
                UninitializeShellHook();
            }

            public override void ProcessWindowMessage(int msg, IntPtr wParam, IntPtr lParam) {
                if (msg == _MsgID_Shell_ActivateShellWindow) {
                    if (ActivateShellWindow != null)
                        ActivateShellWindow();
                }
                else if (msg == _MsgID_Shell_GetMinRect) {
                    if (GetMinRect != null)
                        GetMinRect(wParam);
                }
                else if (msg == _MsgID_Shell_Language) {
                    if (Language != null)
                        Language(wParam);
                }
                else if (msg == _MsgID_Shell_Redraw) {
                    if (Redraw != null)
                        Redraw(wParam);
                }
                else if (msg == _MsgID_Shell_Taskman) {
                    if (Taskman != null)
                        Taskman();
                }
                else if (msg == _MsgID_Shell_WindowActivated) {
                    if (WindowActivated != null)
                        WindowActivated(wParam);
                }
                else if (msg == _MsgID_Shell_WindowCreated) {
                    if (WindowCreated != null)
                        WindowCreated(wParam);
                }
                else if (msg == _MsgID_Shell_WindowDestroyed) {
                    if (WindowDestroyed != null)
                        WindowDestroyed(wParam);
                }
            }
        }

        public class CallWndProcHook : Hook {
            // Values retreived with RegisterWindowMessage
            private int _MsgID_CallWndProc;
            private int _MsgID_CallWndProc_Params;

            public event WndProcEventHandler CallWndProc;

            private IntPtr _CacheHandle;
            private IntPtr _CacheMessage;

            public CallWndProcHook(IntPtr Handle)
                : base(Handle) {
            }

            protected override void OnStart() {
                // Retreive the message IDs that we'll look for in WndProc
                _MsgID_CallWndProc = RegisterWindowMessage("SHIVAQE_HOOK_CALLWNDPROC");
                _MsgID_CallWndProc_Params = RegisterWindowMessage("SHIVAQE_HOOK_CALLWNDPROC_PARAMS");

                // Start the hook
                InitializeCallWndProcHook(0, _Handle);
            }

            protected override void OnStop() {
                    UninitializeCallWndProcHook();
            }

            public override void ProcessWindowMessage(int msg, IntPtr wParam, IntPtr lParam) {
                if (msg == _MsgID_CallWndProc) {
                    _CacheHandle = wParam;
                    _CacheMessage = lParam;
                }
                else if (msg == _MsgID_CallWndProc_Params) {
                    if (CallWndProc != null && _CacheHandle != IntPtr.Zero && _CacheMessage != IntPtr.Zero)
                        CallWndProc(_CacheHandle, _CacheMessage, wParam, lParam);
                    _CacheHandle = IntPtr.Zero;
                    _CacheMessage = IntPtr.Zero;
                }
            }
        }

        public class GetMsgHook : Hook {
            // Values retreived with RegisterWindowMessage
            private int _MsgID_GetMsg;
            private int _MsgID_GetMsg_Params;

            public event WndProcEventHandler GetMsg;

            private IntPtr _CacheHandle;
            private IntPtr _CacheMessage;

            public GetMsgHook(IntPtr Handle)
                : base(Handle) {
            }

            protected override void OnStart() {
                // Retreive the message IDs that we'll look for in WndProc
                _MsgID_GetMsg = RegisterWindowMessage("SHIVAQE_HOOK_GETMSG");
                _MsgID_GetMsg_Params = RegisterWindowMessage("SHIVAQE_HOOK_GETMSG_PARAMS");

                // Start the hook
                InitializeGetMsgHook(0, _Handle);
            }

            protected override void OnStop() {
                UninitializeGetMsgHook();
            }

            public override void ProcessWindowMessage(int msg, IntPtr wParam, IntPtr lParam) {
                if (msg == _MsgID_GetMsg) {
                    _CacheHandle = wParam;
                    _CacheMessage = lParam;
                }
                else if (msg == _MsgID_GetMsg_Params) {
                    if (GetMsg != null && _CacheHandle != IntPtr.Zero && _CacheMessage != IntPtr.Zero)
                        GetMsg(_CacheHandle, _CacheMessage, wParam, lParam);
                    _CacheHandle = IntPtr.Zero;
                    _CacheMessage = IntPtr.Zero;
                }
            }
        }
    }
}
