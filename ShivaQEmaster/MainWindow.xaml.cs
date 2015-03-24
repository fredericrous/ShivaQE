using ShivaQEcommon.Eventdata;
using System;
using System.IO;
using System.Timers;
using System.Windows;
using Newtonsoft.Json;
using System.Net.Sockets;
using System.Threading.Tasks;
using ShivaQEcommon;
using System.Drawing;
using log4net;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media.Imaging;
using System.Text;

namespace ShivaQEmaster
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        static MainWindowBindings _bindings;
        MouseNKeyListener _mouseNKeyListener;
        SlaveManager _slaveManager;
        //UIChangeListener _uichange;
        Recorder _recorder;

        public static MainWindowBindings Bindings
        {
            get { return _bindings; }//set { _bindings = value; }
        }

        private static readonly ILog _log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        static string lastKey = string.Empty; //there's a time out to reset lastKey 3sec after a press key
        static Timer doubleClickReset = new Timer() { Interval = 3000, AutoReset = false };
        
        Tuple<string, int[]> _activeWindowInfo = null;

        public delegate void UpdateBroadcastStatusEventHandler(bool status);
        public static event UpdateBroadcastStatusEventHandler UpdateBroadcastStatus;

        public delegate void errorEventHandler(string text);
        public static event errorEventHandler ErrorMsg;

        private static NotifyWindow _notifyWindow = new NotifyWindow();

        bool? _mouseCaptured = null;
        bool? _keyCaptured = null;
        bool? _windowCaptured = null;

        private void ImageReceveid(byte[] notifyIcon)
        {
            if (notifyIcon != null && _notifyWindow != null)
            {
                try
                {
                    _notifyWindow.NotifyIcon = notifyIcon;
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
            else
            {
                _log.Error("can't get compared image");
            }
        }

        private void ErrorNotIdentical(string error_msg, string hostname)
        {
            try
            {
                if (_notifyWindow != null && _notifyWindow.IsLoaded)
                {
                    _notifyWindow.Hide();
                }
            }
            catch { } // isloaded may return an exception

            _notifyWindow.tb_warning.Text = error_msg;
            _notifyWindow.Show();
        }

        public MainWindow()
        {
            InitializeComponent();
            
            Analytics analytics = Analytics.Instance;
            analytics.Init("ShivaQE Master", "1.0");

            _bindings = this.Resources["MainWindowBindingsDataSource"] as MainWindowBindings;
            this.DataContext = this; //deadcode?

            _slaveManager = SlaveManager.Instance;

            _slaveManager.Incoming += async (data, slave, networkStream) =>
                {
                    if (data.Contains("platform") && data.Contains("version"))
                    { //if we have reconnected
                        ServerInfo serverInfo = JsonConvert.DeserializeObject<ServerInfo>(data);
                        slave.token = serverInfo.token;

                        //send a empty request in order to update status of _slave.Connected
                        await networkStream.WriteAsync(new byte[] { 1 }, 0, 1);
                    }
                    else if (data.Contains("method"))
                    {
                        try
                        {
                            ActionMethod action = JsonConvert.DeserializeObject<ActionMethod>(data);

                            switch (action.method)
                            {
                                case ActionType.Disconnect: //not used for disconnect when sent by slave..but to re-give token
                                    slave.token = action.value;
                                    break;
                                case ActionType.CheckIdentical:

                                    ActionMethod<byte[]> receivedAction = JsonConvert.DeserializeObject<ActionMethod<byte[]>>(data);

                                    byte[] result = receivedAction.value;

                                    if (result != null)
                                    {
                                        ImageReceveid(result);
                                    }
                                    break;
                                default:
                                    break;
                            }

                        }
                        catch (Exception ex)
                        {
                            _log.Info("not an image: " + data, ex);
                        }
                    }
                    else if (SettingsManager.ReadSetting("notification_status") == "true")
                    {
                        string[] responseTab = data.Replace("<EOF>", "").Split(':');
                        string time = responseTab[0];
                        string key = responseTab[1];

                        data = string.Format("{0}: error on {1} at time {2}", slave.friendlyName, key, time);

                        ErrorNotIdentical(data, slave.friendlyName);
                    }
                };
            
            _slaveManager.Init();

            _NavigationFrame.Navigate(new HomePage());

            ////wait window is loaded to subscribe to uichangelistener
            //this.Loaded += (s, e) =>
            //{
            //    //listen for windows events related to UI
            //    _uichange = new UIChangeListener(this);

            //    //on window creation, sync slaves to be sure all slaves have the same size of window as master
            //    _uichange.WindowCreated += () =>
            //        {
            //            try
            //            {
            //                _activeWindowInfo = _uichange.getActiveWindowInfo();
            //            }
            //            catch (Exception ex)
            //            {
            //                _log.Warn("error getting active window info", ex);
            //                return;
            //            }
            //        };
            //};

            _recorder = Recorder.Instance;
            _recorder.Init();

            //disconnect slaves and remove listerner
            this.Closed += (object sender, EventArgs e) =>
            {
                try
                {
                    _slaveManager.DisconnectAll();
                    //_uichange.StopListener();
                    _mouseNKeyListener.DeactiveAll();
                    //_uichange = null;
                    _mouseNKeyListener = null;

                    Application.Current.Shutdown(); // Force close because NotifyWindow is opened
                }
                catch (Exception ex)
                {
                    _log.Error("cant clean properly", ex);
                }
            };

            this.GotFocus += (s, e) =>
            {
                _windowCaptured = true;
            };

            this.LostFocus += (s, e) =>
            {
                _windowCaptured = false;
            };

            App.KeyCaptured += (status) =>
            {
                _keyCaptured = status;
            };

            _notifyWindow.MouseEnter += (s, e) =>
            {
                _mouseCaptured = true;
            };

            _notifyWindow.MouseLeave += (s, e) =>
            {
                _mouseCaptured = false;
            };

            _mouseNKeyListener = MouseNKeyListener.Instance;

            _mouseNKeyListener.Active();
            _mouseNKeyListener.MouseClick += (s, ev) =>
            {
                
                Task.Run( async () =>
                    {
                        // if (isWindowClicked(ev.position_x, ev.position_y, Application.Current.MainWindow))
                        if (_mouseCaptured == true)
                        {
                            _log.Info(string.Format("wont transmit {0} click {1} done on master window", ev.key, ev.keyData));
                            return;
                        }

                        bool isLeftClickDown = ev.key == "Left" && ev.keyData == "down";
                        //send winpos before click down
                        if (isLeftClickDown)
                        {
                            try
                            {
                                _activeWindowInfo = getActiveWindowInfo();
                            }
                            catch (Exception ex)
                            {
                                _log.Warn("error getting active window info", ex);
                                _activeWindowInfo = null;
                            }
                            if (_activeWindowInfo != null)
                            {
                                //send window's position
                                //should be raised by windowcreated event but it doesn't work weel so it's a workaround...
                                sendWindowPos(_activeWindowInfo);
                                ev.windowPos = _activeWindowInfo.Item1 + "." + String.Join(".", _activeWindowInfo.Item2);
                            }
                        }

                        try
                        {
                            if (_notifyWindow != null && _notifyWindow.IsLoaded)
                            {
                                //if (isWindowClicked(ev.position_x, ev.position_y, _notifyWindow))
                                if (_notifyWindow.IsMouseCaptured)
                                {
                                    _log.Info(string.Format("wont transmit {0} click {1} done on notify window", ev.key, ev.keyData));
                                    return;
                                }
                            }
                        }
                        catch
                        {
                            //we dont care about this exception. It was put to handle invalidoperation thrown by _notifyWindow.IsLoaded
                        }


                        //record click
                        _recorder.Write(ev);

                        //save picture of click in order to compare
                        if (isLeftClickDown && SettingsManager.ReadSetting("notification_status") == "true")
                        {
                            string comparatorName = string.Format("comparator.{0}.png", 1);
                            int rect_size = 64;
                            Rectangle rect = new Rectangle()
                            {
                                Height = rect_size,
                                Width = rect_size,
                                X = ev.position_x - (rect_size / 2),
                                Y = ev.position_y - (rect_size / 2)
                            };
                            Bitmap comparatorCapture = ScreenCapturePInvoke.CaptureScreen(rect, false);
                            comparatorCapture.Save(comparatorName);

                            byte[] file = File.ReadAllBytes(comparatorName);

                            ev.screenshotBytes = file;
                        }

                        //send click
                        try
                        {
                            List<string> error_hosts = await _slaveManager.Send<MouseNKeyEventArgs>(ev);
                            UpdateSendErrorIfThereIs(error_hosts);
                        }
                        catch (IOException ex)
                        {
                            _log.Error("refresh list because", ex);
                            ErrorMsg("Error sending mouse");
                            //404 lv_slaves.Items.Refresh();
                        }
                    });

            };
            //reset last key pressed to none after 3 seconds
            doubleClickReset.Elapsed += (_s, _e) =>
            {
                lastKey = string.Empty;
            };
            bool _windowInTaskbarState = false;

            _mouseNKeyListener.KeyboadDown += async (s, ev) =>
            {
                try
                {
                    ActionType actionMethod = ActionType.None;
                    string actionValue = null;

                    if (ev.key == lastKey)
                    {
                        string key_sendmousekey_on = SettingsManager.ReadSetting("key_sendmousekey_on");
                        string key_sendmousekey_off = SettingsManager.ReadSetting("key_sendmousekey_off");
                        string key_window_hide_toggle = SettingsManager.ReadSetting("key_window_hide_toggle"); //should be useless because now it's automatic & systematic
                        string key_force_resize = SettingsManager.ReadSetting("key_force_resize");
                        
                        if (ev.keyData == key_sendmousekey_on)
                        {
                            _mouseNKeyListener.Active(true);
                            UpdateBroadcastStatus(true);
                            return;
                        }
                        else if (ev.keyData == key_sendmousekey_off)
                        {
                            _mouseNKeyListener.DeactiveAll();
                            UpdateBroadcastStatus(false);
                            return;
                        }
                        else if (ev.keyData == key_force_resize)
                        {
                            actionMethod = ActionType.SetWindowPos;
                            Tuple<string, int[]> activeWindowInfo = null;
                            try
                            {
                                activeWindowInfo = getActiveWindowInfo();
                            }
                            catch (Exception ex)
                            {
                                _log.Warn("error getting active window info", ex);
                                return;
                            }
                            actionValue = activeWindowInfo.Item1 + "." + String.Join(".", activeWindowInfo.Item2);
                        }
                        else if (ev.keyData == key_window_hide_toggle)
                        {
                            this.ShowInTaskbar = _windowInTaskbarState;
                            _windowInTaskbarState = !_windowInTaskbarState;
                            if (_windowInTaskbarState)
                            {
                                this.Hide();
                            }
                            else
                            {
                                this.Show();
                            }
                        }

                        doubleClickReset.Stop();
                        lastKey = string.Empty;
                    }
                    else
                    {
                        doubleClickReset.Start();
                        lastKey = ev.key;
                    }

                    if (actionMethod != ActionType.None)
                    {
                        ActionMethod action = new ActionMethod()
                        {
                            method = actionMethod,
                            value = actionValue
                        };
                        await _slaveManager.Send<ActionMethod>(action);

                    }
                    else if (_mouseNKeyListener != null && _mouseNKeyListener.isActive && _keyCaptured != true)
                    {
                        //record
                        _recorder.Write(ev);

                        //send input
                        List<string> error_hosts = await _slaveManager.Send<MouseNKeyEventArgs>(ev);
                        UpdateSendErrorIfThereIs(error_hosts);
                    }

                }
                catch (IOException ex)
                {
                    _log.Error("refresh list because", ex);
                    ErrorMsg("Error sending key");
                    //404 lv_slaves.Items.Refresh();
                }
            };
            _mouseNKeyListener.MouseMove += (s, ev) =>
            {
                try
                {
                    _slaveManager.Broadcast<MouseNKeyEventArgs>(ev);

                    //to try:   - with virtalwin!?
                   // inputSimulator.Mouse.MoveMouseToPositionOnVirtualDesktop(mouseNkey.position_x, mouseNkey.position_y);
                }
                catch (Exception ex)
                {
                    _log.Error("refresh list because", ex);
                    // 404 lv_slaves.Items.Refresh();
                }
            };

            ClipboardNotification.ClipboardUpdate += async (s, e) =>
                {
                    if (_windowCaptured == true)
                    {
                        _log.Info("won't send copy/past because key was captured inside shivaqe window");
                        return;
                    }
                    ActionType actionMethod = ActionType.None;
                    string actionValue = null;

                    actionMethod = ActionType.UpdateClipboard;
                    actionValue = JsonConvert.SerializeObject(Clipboard.GetDataObject());

                    ActionMethod action = new ActionMethod()
                    {
                        method = actionMethod,
                        value = actionValue
                    };
                    try
                    {
                        await _slaveManager.Send<ActionMethod>(action);
                    }
                    catch (Exception ex)
                    {
                        _log.Error("can't send clipboard update", ex);
                    }
                };
        }

        private void UpdateSendErrorIfThereIs(List<string> error_hosts)
        {
            if (error_hosts.Count > 0)
            {
                string concat_hosts = string.Empty;
                for (var i = 0; i < error_hosts.Count; i++)
                {
                    concat_hosts += error_hosts[i] + ((error_hosts.Count == i - 1) ? string.Empty : ", ");
                }
                ErrorMsg(string.Format("Error sending key to slaves {0}", concat_hosts));
            }
        }

        [DllImport("user32.dll")]
        static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        [DllImport("user32.dll")]
        public static extern IntPtr GetForegroundWindow();

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

        public Tuple<string, int[]> getActiveWindowInfo()
        {
            RECT rct;
            IntPtr foregroundWindow = GetForegroundWindow();

            if (!GetWindowRect(foregroundWindow, out rct))
            {
                _log.Warn("error getting active window");
                return null;
            }

            int width = rct.Right - rct.Left + 1;
            int height = rct.Bottom - rct.Top + 1;

            int[] rcValues = { rct.Left, rct.Top, width, height };

            string windowName = GetProcessByHandle(foregroundWindow).ProcessName;

            return new Tuple<string, int[]>(windowName, rcValues);
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

        private async void sendWindowPos(Tuple<string, int[]> activeWindowInfo)
        {
            ActionMethod action = new ActionMethod()
            {
                method = ActionType.SetWindowPos,
                value = activeWindowInfo.Item1 + "." + String.Join(".", activeWindowInfo.Item2)
            };
            try
            {
                await _slaveManager.Send<ActionMethod>(action);
                activeWindowInfo = null;
                await Task.Delay(1000); //await position is set before sending click
            }
            catch (Exception ex)
            {
                _log.Error("error send window created", ex);
            }
        }

        /// <summary>
        /// open help window
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void bt_help_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            _NavigationFrame.Navigate(new HelpPage());
        }

        private void bt_settings_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            _NavigationFrame.Navigate(new SettingsPage());
        }

        private void MetroWindow_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            _mouseCaptured = false;
        }

        private void MetroWindow_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
        	_mouseCaptured = true;
        }

    }
}
