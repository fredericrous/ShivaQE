using Newtonsoft.Json;
using System;
using System.Text;
using System.Net.Sockets;
using System.Diagnostics;
using System.Runtime.InteropServices;
using ShivaQEcommon.Eventdata;
using System.Windows;
using IDataObject = System.Windows.Forms.IDataObject;
using Clipboard = System.Windows.Forms.Clipboard;
using Application = System.Windows.Forms.Application;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Interop;
using ShivaQEcommon.Hook;
using log4net;
using System.Reflection;

namespace ShivaQEslave
{
    class Program
    {
        private static ServerInfo serverInfo;

        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private static int port = 1142;

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int W, int H, uint uFlags);

        public static readonly uint SWP_ASYNCWINDOWPOS = 0x4000;
        public static readonly IntPtr HWND_TOP = new IntPtr(0);

        [DllImport("user32.dll")]
        public static extern IntPtr GetForegroundWindow();

        public static IntPtr HWND_MESSAGE = new IntPtr(-3);

        // See http://msdn.microsoft.com/en-us/library/ms633541%28v=vs.85%29.aspx
        // See http://msdn.microsoft.com/en-us/library/ms649033%28VS.85%29.aspx
        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr SetParent(IntPtr hWndChild, IntPtr hWndNewParent);


        [STAThread]
        static void Main(string[] args)
        {
            try
            {
                serverInfo = new ServerInfo()
                {
                    platform = "windows",
                    version = Environment.OSVersion.ToString(),
                    lang = MouseNKeySimulator.getKeyboardLang()
                };

                string ServerResponseString = JsonConvert.SerializeObject(serverInfo);
                ServerResponseString += "<EOF>"; //used serverside to know string has been received entirely
                byte[] ServerResponseBytes = Encoding.UTF8.GetBytes(ServerResponseString);


                //start listening on tcp
                AsynchronousSlave.StartListening(port);

                //on tcp connection accepted, do..
                AsynchronousSlave.TCPaccepted += async (NetworkStream networkStream) =>
                    {
                        NotifyIconSystray.ChangeStatus(true);
                        //give os version to master. may be usefull in the future for crossplatform update
                        //also give keyboard layout in case we should change it
                        await networkStream.WriteAsync(ServerResponseBytes, 0, ServerResponseBytes.Length);
                    };

                UIChangeListener _uichange = null;

                var hiddenWindow = new Window()
                {
                    Width = 0,
                    Height = 0,
                    WindowStyle = WindowStyle.None,
                    ShowInTaskbar = false,
                    ShowActivated = false
                };
                hiddenWindow.Loaded += (s, e) =>
                    {
                        _uichange = new UIChangeListener(hiddenWindow);
                    };
                hiddenWindow.Show();

                HwndSource hwndSource = PresentationSource.FromVisual(hiddenWindow) as HwndSource;
                if (hwndSource != null)
                {
                    SetParent(hwndSource.Handle, (IntPtr)HWND_MESSAGE);
                }

              
                //on data received (after TCP connection was accepted and client sent message), do..
                AsynchronousSlave.TCPdataReceived += (string data, NetworkStream networkStream) =>
                    {
                        log.Info(data);
                        try
                        {
                            if (data.StartsWith(@"{""method"":"))
                            {
                                ActionMethod action = JsonConvert.DeserializeObject<ActionMethod>(data);

                                switch (action.method)
                                {
                                    case ActionType.SetLang:
                                        MouseNKeySimulator.setKeyboardLang(action.value);
                                        break;
                                    case ActionType.SetWindowPos:
                                        string[] windowPos = action.value.Split('.');
                                        int Left = int.Parse(windowPos[0]);
                                        int Top = int.Parse(windowPos[1]);
                                        int Width = int.Parse(windowPos[2]);
                                        int Heigh = int.Parse(windowPos[3]);

                                        SetWindowPos(GetForegroundWindow(), HWND_TOP, Left, Top, Width, Heigh, SWP_ASYNCWINDOWPOS);
                                        break;
                                    case ActionType.UpdateClipboard:
                                        IDataObject clipboardObject = JsonConvert.DeserializeObject<IDataObject>(action.value);
                                        Clipboard.SetDataObject(clipboardObject);
                                        break;
                                    case ActionType.CheckIdentical:
                                        if (_uichange != null)
                                        {
                                            List<string> eventCalls = _uichange.getEventCalls;
                                            List<string> masterEventCalls = JsonConvert.DeserializeObject<List<string>>(action.value);

                                            var masterDifferentThanSlave = masterEventCalls.Except(eventCalls).ToList().Count > 0;
                                            if (masterDifferentThanSlave)
                                            {
                                                ActionMethod actionIdentical = new ActionMethod()
                                                    {
                                                        method = ActionType.CheckIdentical,
                                                        value = "false"
                                                    };
                                                string actionString = JsonConvert.SerializeObject(actionIdentical);
                                                actionString += "<EOF>"; //used serverside to know string has been received entirely
                                                byte[] actionBytes = Encoding.UTF8.GetBytes(ServerResponseString);
                                                networkStream.WriteAsync(actionBytes, 0, actionBytes.Length);
                                            }
                                        }
                                        break;
                                    case ActionType.Disconnect:                                        
                                        AsynchronousSlave.StopListening();
                                        NotifyIconSystray.ChangeStatus(false);
                                        break;
                                    default:
                                        break;
                                }

                                return;
                            }

                            MouseNKeyEventArgs mouseNkey = JsonConvert.DeserializeObject<MouseNKeyEventArgs>(data);

                            //execute action press, click, scroll
                            MouseNKeySimulator.SimulateAction(mouseNkey);

                        }
                        catch (Exception e)
                        {
                            log.Error("tcp received", e);
                        }
                    };


                //start listening on udp
                AsynchronousSlave.StartBroadcastListening(port);

                //on data received, do..
                AsynchronousSlave.UDPdataReceived += (string data) =>
                    {
                        try
                        {
                            MouseNKeyEventArgs mouseNkey = JsonConvert.DeserializeObject<MouseNKeyEventArgs>(data);

                            //set mouse curstor to position X, Y
                            MouseNKeySimulator.setMousePosition(mouseNkey);
                        }
                        catch (Exception ex)
                        {
                            log.Error("udp received", ex);
                        }
                    };

                //-- -- -- Add notification icon
                NotifyIconSystray.addNotifyIcon("ShivaQE Slave");

                Application.Run();
            }
            catch (Exception ex)
            {
                log.Error("somewhere", ex);
            }
        }
    }
}
