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
using log4net;
using System.Reflection;
using System.IO;
using System.Windows.Media.Imaging;
using System.Drawing;
using ShivaQEcommon;
using System.Threading;
using System.IO.Pipes;

namespace ShivaQEslave
{
    class Program
    {
        private static ServerInfo _serverInfo;

        private static readonly ILog _log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private static int _default_port = 1142;

        [DllImport("user32.dll")]
        public static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int W, int H, uint uFlags);

        public static readonly uint SWP_ASYNCWINDOWPOS = 0x4000;
        public static readonly IntPtr HWND_TOP = new IntPtr(0);

        [DllImport("user32.dll")]
        public static extern IntPtr GetForegroundWindow();

        public static IntPtr HWND_MESSAGE = new IntPtr(-3);

        // See http://msdn.microsoft.com/en-us/library/ms633541%28v=vs.85%29.aspx
        // See http://msdn.microsoft.com/en-us/library/ms649033%28VS.85%29.aspx
        [DllImport("user32.dll")]
        public static extern IntPtr SetParent(IntPtr hWndChild, IntPtr hWndNewParent);

        [DllImport("user32.dll")]
        static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);


        [STAThread]
        static void Main(string[] args)
        {
            try
            {
                _serverInfo = new ServerInfo()
                {
                    platform = "windows",
                    version = Environment.OSVersion.ToString(),
                    lang = MouseNKeySimulator.getKeyboardLang()
                };

                int port = _default_port;
                try
                {
                    port = Int32.Parse(SettingsManager.ReadSetting("port"));
                }
                catch (Exception ex)
                {
                    _log.Warn("cant parse port, using default " + _default_port, ex);
                }

                if (args.Length > 0)
                {
                    int tmp_port;
                    if (Int32.TryParse(args[0], out tmp_port))
                    {
                        port = tmp_port;
                    }
                    else
                    {
                        Console.WriteLine(string.Format("Can't set port {0}", args[0]));
                    }
                }
                //try
                //{
                //    using (NamedPipeServerStream pipeServer =
                //        new NamedPipeServerStream("testpipe", PipeDirection.Out))
                //    {
                //        Console.WriteLine("NamedPipeServerStream object created.");

                //        Console.Write("Waiting for client connection...");
                //        pipeServer.BeginWaitForConnection((c) =>
                //            {
                //                _log.Info("connection callback");
                //            }, "wait_pipe_connection");

                //        Console.WriteLine("Client connected.");
                //        try
                //        {
                //            // Read user input and send that to the client process. 
                //            using (StreamWriter sw = new StreamWriter(pipeServer))
                //            {
                //                sw.AutoFlush = true;
                //                sw.WriteLine("test namedpipe");
                //            }
                //        }
                //        catch (IOException ex)
                //        {
                //            _log.Error(ex.Message);
                //        }
                //    }
                //}
                //catch
                //{
                //    _log.Info("already running");
                //    return;
                //}

                startServer(port);

                //-- -- -- Add notification icon
                NotifyIconSystray.addNotifyIcon("ShivaQE Slave");

                Application.Run();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message);
                _log.Error("somewhere", ex);
            }
        }

        private static void startServer(int port)
        {
            string ServerResponseString = JsonConvert.SerializeObject(_serverInfo);
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

            //global ui change listener behind an hidden window
            // UIChangeListener _uichange = null;

            //var hiddenWindow = new Window()
            //{
            //    Width = 0,
            //    Height = 0,
            //    WindowStyle = WindowStyle.None,
            //    ShowInTaskbar = false,
            //    ShowActivated = false
            //};
            //hiddenWindow.Loaded += (s, e) =>
            //    {
            //        _uichange = new UIChangeListener(hiddenWindow);
            //    };
            //hiddenWindow.Show();

            //HwndSource hwndSource = PresentationSource.FromVisual(hiddenWindow) as HwndSource;
            //if (hwndSource != null)
            //{
            //    SetParent(hwndSource.Handle, (IntPtr)HWND_MESSAGE);
            //}


            //on data received (after TCP connection was accepted and client sent message), do..
            AsynchronousSlave.TCPdataReceived += (string data, NetworkStream networkStream) =>
            {
                _log.Info(data);
                try
                {
                    if (data.StartsWith(@"{""method"":"))
                    {
                        ActionMethod action = JsonConvert.DeserializeObject<ActionMethod>(data);
                        handleAction(action);

                        return;
                    }

                    MouseNKeyEventArgs mouseNkey = JsonConvert.DeserializeObject<MouseNKeyEventArgs>(data);

                    if (mouseNkey.screenshotBytes != null)
                    {
                        _log.Info("received a png image to compare");

                        Stream imageStreamSource = new MemoryStream(mouseNkey.screenshotBytes);
                        Bitmap bmp = new Bitmap(imageStreamSource);
                        //PngBitmapDecoder decoder = new PngBitmapDecoder(imageStreamSource, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.Default);
                        //BitmapSource bitmapSource = decoder.Frames[0];

                        int rect_size = 64;
                        Rectangle rect = new Rectangle()
                        {
                            Height = rect_size,
                            Width = rect_size,
                            X = mouseNkey.position_x - (rect_size / 2),
                            Y = mouseNkey.position_y - (rect_size / 2)
                        };
                        Bitmap comparatorCapture = ScreenCapturePInvoke.CaptureScreen(rect, false);

                        double resultCompare = CompareImages.Compare(bmp, comparatorCapture, 0);

                        if (resultCompare > 1)
                        {
                           // string except_msg = "NOTIDENTICAL:";
                            //ActionMethod actionIdentical = new ActionMethod()
                            //    {
                            //        method = ActionType.CheckIdentical,
                            //        value = mouseNkey.timestamp + ":" + mouseNkey.key
                            //    };
                            //string actionString = JsonConvert.SerializeObject(actionIdentical);
                            string actionString = mouseNkey.timestamp + ":" + mouseNkey.key;
                            actionString += "<EOF>"; //used serverside to know string has been received entirely
                            byte[] actionBytes = Encoding.UTF8.GetBytes(actionString);
                            networkStream.WriteAsync(actionBytes, 0, actionBytes.Length);
                        }

                        _log.Info("image comparator" + CompareImages.Compare(bmp, comparatorCapture, 0));
                    }

                    //execute action press, click, scroll
                    MouseNKeySimulator.SimulateAction(mouseNkey);

                }
                catch (Exception e)
                {
                    _log.Error("tcp received", e);
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
                    _log.Error("udp received", ex);
                }
            };
        }

        private static void handleAction(ActionMethod action)
        {
            switch (action.method)
            {
                case ActionType.SetLang:
                    MouseNKeySimulator.setKeyboardLang(action.value);
                    break;
                case ActionType.SetWindowPos:
                    string[] windowPos = action.value.Split('.');
                    int Left = int.Parse(windowPos[1]);
                    int Top = int.Parse(windowPos[2]);
                    int Width = int.Parse(windowPos[3]);
                    int Heigh = int.Parse(windowPos[4]);

                    IntPtr foregroundWindow = GetForegroundWindow();

                    string windowName = GetProcessByHandle(foregroundWindow).ProcessName;
                    if (windowName == windowPos[0])
                    {
                        SetWindowPos(foregroundWindow, HWND_TOP, Left, Top, Width, Heigh, SWP_ASYNCWINDOWPOS);
                    }
                    break;
                case ActionType.UpdateClipboard:
                    IDataObject clipboardObject = JsonConvert.DeserializeObject<IDataObject>(action.value);
                    Clipboard.SetDataObject(clipboardObject);
                    break;
                //case ActionType.CheckIdentical:
                //    if (_uichange != null)
                //    {
                //        List<string> eventCalls = _uichange.getEventCalls;
                //        List<string> masterEventCalls = JsonConvert.DeserializeObject<List<string>>(action.value);

                //        var masterDifferentThanSlave = masterEventCalls.Except(eventCalls).ToList().Count > 0;
                //        if (masterDifferentThanSlave)
                //        {
                //            ActionMethod actionIdentical = new ActionMethod()
                //                {
                //                    method = ActionType.CheckIdentical,
                //                    value = "false"
                //                };
                //            string actionString = JsonConvert.SerializeObject(actionIdentical);
                //            actionString += "<EOF>"; //used serverside to know string has been received entirely
                //            byte[] actionBytes = Encoding.UTF8.GetBytes(ServerResponseString);
                //            networkStream.WriteAsync(actionBytes, 0, actionBytes.Length);
                //        }
                //    }
                //    break;
                case ActionType.Disconnect:
                    AsynchronousSlave.StopListening();
                    NotifyIconSystray.ChangeStatus(false);
                    break;
                default:
                    break;
            }
        }

        //private static bool isPng(byte[] databytes)
        //{
        //    bool ret = true;
        //    byte[] pngHeader = new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 };
        //    for (var i = 0; i < pngHeader.Length; i++)
        //    {
        //        if (pngHeader[i] != databytes[i])
        //        {
        //            ret = false;
        //            break;
        //        }
        //    }
        //    return ret;
        //}

        private static Process GetProcessByHandle(IntPtr hwnd)
        {
            try
            {
                uint processID;
                GetWindowThreadProcessId(hwnd, out processID);
                return Process.GetProcessById((int)processID);
            }
            catch { return null; }
        }

    }
}

