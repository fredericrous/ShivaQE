﻿using Newtonsoft.Json;
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

        static string _tmp_screenshot_path = "tmp.bmp";

        [STAThread]
        static void Main(string[] args)
        {
            try
            {
                _serverInfo = new ServerInfo()
                {
                    platform = "windows",
                    version = Environment.OSVersion.ToString(),
                    lang = MouseNKeySimulator.getKeyboardLang(),
                    token = Guid.NewGuid().ToString().Substring(0, 8)
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

        private static string disociateDataFromToken(string data)
        {
            if (data.IndexOf(':') == -1)
            {
                _log.Info("data is not correctly formated: {0}");
                return string.Empty;
            }
            string token = data.Substring(0, data.IndexOf(':'));
            var charIndex = from ch in token.ToArray() where Char.IsLetterOrDigit(ch) select token.IndexOf(ch); //I dont know why but master sends token and slave receives %01token
            if (charIndex.Count() > 0)
                token = token.Substring(charIndex.First());

            if (!_serverInfo.token.Equals(token))
            {
                _log.Info(string.Format("incorrect token: {0}. Expected was {1}", token, _serverInfo.token));
                return string.Empty;
            }

            return data.Substring(data.IndexOf(':') + 1);
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

            AsynchronousSlave.TCPclosed += () =>
            {
                NotifyIconSystray.ChangeStatus(false);
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
            AsynchronousSlave.TCPdataReceived += (string rawData, NetworkStream networkStream) =>
            {
                _log.Info(rawData);

                try
                {
                    //if token is ok, get data, else return empty string
                    string data = disociateDataFromToken(rawData);

                    //if no token, notify master
                    if (data == string.Empty)
                    {
                        ActionMethod action = new ActionMethod()
                        {
                            method = ActionType.Disconnect,
                            value = _serverInfo.token
                        };

                        string actionString = JsonConvert.SerializeObject(action);
                        actionString += "<EOF>";
                        byte[] actionBytes = Encoding.UTF8.GetBytes(actionString);
                        networkStream.WriteAsync(actionBytes, 0, actionBytes.Length);
                        return;
                    }

                    //all action that are not click, key are handled here: setWindowPosition, Disconnection
                    if (data.StartsWith(@"{""method"":"))
                    {
                        ActionMethod action = JsonConvert.DeserializeObject<ActionMethod>(data);
                        try
                        {
                            handleAction(action, networkStream);
                        }
                        catch (Exception ex)
                        {
                            _log.Error(string.Format("can't execute action {0} with value {1}", action.method, action.value), ex);
                        }

                        return;
                    }

                    MouseNKeyEventArgs mouseNkey = JsonConvert.DeserializeObject<MouseNKeyEventArgs>(data);

                    if (mouseNkey.windowPos != null)
                    {
                        setWindowPos(mouseNkey.windowPos);
                        //Thread.Sleep(100); // wait the window to resize
                    }

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
                        comparatorCapture.Save(_tmp_screenshot_path);

                        double resultCompare = CompareImages.Compare(bmp, comparatorCapture, 0);

                        if (resultCompare > 1)
                        {
                            ActionMethod actionIdentical = new ActionMethod()
                            {
                                method = ActionType.CheckIdentical,
                                value = mouseNkey.timestamp + ":" + mouseNkey.key
                            };
                            //string actionString = JsonConvert.SerializeObject(actionIdentical);
                            string actionString = mouseNkey.timestamp + ":" + mouseNkey.key;
                            actionString += "<EOF>"; //used serverside to know string has been received entirely
                            byte[] actionBytes = Encoding.UTF8.GetBytes(actionString);
                            networkStream.WriteAsync(actionBytes, 0, actionBytes.Length);

                            ActionMethod<byte[]> actionIdenticalImage = new ActionMethod<byte[]>()
                            {
                                method = ActionType.CheckIdentical,
                                value = File.ReadAllBytes(_tmp_screenshot_path)
                            };
                            actionString = JsonConvert.SerializeObject(actionIdenticalImage);
                            actionString += "<EOF>"; //used serverside to know string has been received entirely
                            actionBytes = Encoding.UTF8.GetBytes(actionString);
                            networkStream.WriteAsync(actionBytes, 0, actionBytes.Length);
                        }

                        _log.Info("image comparator" + CompareImages.Compare(bmp, comparatorCapture, 0));
                    }

                    //execute action press, click, scroll
                    MouseNKeySimulator.SimulateAction(mouseNkey);

                }
                catch (Exception ex)
                {
                    if (ex.Message.Contains("User Interface Privacy Isolation"))
                    {
                        _log.Warn("Can't simulate action click/key because of Windows protection : User Interface Privacy Isolation.", ex);

                        //try to wait for a graphical session
                        Microsoft.Win32.SystemEvents.SessionSwitch -= SystemEvents_SessionSwitch;
                        Microsoft.Win32.SystemEvents.SessionSwitch += SystemEvents_SessionSwitch;
                    }
                    else
                    {
                        _log.Error("tcp received", ex);
                    }
                }
            };


            //start listening on udp
            AsynchronousSlave.StartBroadcastListening(port);

            //on data received, do..
            AsynchronousSlave.UDPdataReceived += (string data) =>
            {
                try
                {
                    //if token is ok, get data, else return empty string
                    string dataFormated = disociateDataFromToken(data);

                    //if no token, no action
                    if (dataFormated == string.Empty)
                    {
                        return;
                    }

                    MouseNKeyEventArgs mouseNkey = JsonConvert.DeserializeObject<MouseNKeyEventArgs>(dataFormated);

                    //set mouse curstor to position X, Y
                    MouseNKeySimulator.setMousePosition(mouseNkey);
                }
                catch (Exception ex)
                {
                    _log.Error(String.Format("udp received: {0}. Exception:", data), ex);
                }
            };
        }

        static void SystemEvents_SessionSwitch(object sender, Microsoft.Win32.SessionSwitchEventArgs e)
        {
            //if a rdp session opens
            if (e.Reason == Microsoft.Win32.SessionSwitchReason.RemoteConnect)
            {
                _log.Info("app restarted because a rdp session was opened");
                //restart app
                Application.Restart();
            }
        }

        private static void setWindowPos(string windowPosString)
        {
            string[] windowPos = windowPosString.Split('*');
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
        }

        private static void handleAction(ActionMethod action, NetworkStream networkStream)
        {
            switch (action.method)
            {
                case ActionType.SetLang:
                    MouseNKeySimulator.setKeyboardLang(action.value);
                    break;
                case ActionType.SetWindowPos:
                    setWindowPos(action.value);
                    break;
                case ActionType.UpdateClipboard:
                    IDataObject clipboardObject = JsonConvert.DeserializeObject<IDataObject>(action.value);
                    Clipboard.SetDataObject(clipboardObject);
                    break;
                //case ActionType.CheckIdentical:

                //    break;
                case ActionType.Disconnect:
                    AsynchronousSlave.StopListening();
                    NotifyIconSystray.ChangeStatus(false);
                    break;
                default:
                    break;
            }
        }

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

