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

        private NotifyWindow _notifyWindow;

        public MainWindow()
        {
            InitializeComponent();

            //new Analytics()  .Init("ShivaQE Viewer", "1.0");

            _bindings = this.Resources["MainWindowBindingsDataSource"] as MainWindowBindings;
            this.DataContext = this; //deadcode?

            _slaveManager = SlaveManager.Instance;
            _slaveManager.Init();
            _slaveManager.ErrorNotIdentical += (error_msg) =>
                {
                    if (_notifyWindow != null /* && _notifyWindow.IsLoaded */)
                    {
                        _notifyWindow.Close();
                    }
                    _notifyWindow = new NotifyWindow(error_msg);
                    _notifyWindow.Show();
                };

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
                }
                catch (Exception ex)
                {
                    _log.Error("cant clean properly", ex);
                }
            };

            _mouseNKeyListener = MouseNKeyListener.Instance;

            _mouseNKeyListener.Active();
            _mouseNKeyListener.MouseClick += (s, ev) =>
            {
                if (isWindowClicked(ev.position_x, ev.position_y, Application.Current.MainWindow))
                {
                    _log.Info("wont transmit click done on master window");
                    return;
                }

                if (_notifyWindow != null && _notifyWindow.IsLoaded)
                {
                    if (isWindowClicked(ev.position_x, ev.position_y, _notifyWindow))
                    {
                        _log.Info("wont transmit click done on notify window");
                        return;
                    }
                }

                Task.Run( async () =>
                    {

                        //send winpos before click
                        //should be raised by windowcreated event but it doesn't work weel so it's a workaround...
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
                            sendWindowPos(_activeWindowInfo);
                            ev.windowPos = _activeWindowInfo.Item1 + "." + String.Join(".", _activeWindowInfo.Item2);
                        }

                        //record click
                        _recorder.Write(ev);

                        //save picture of click in order to compare
                        string comparatorName = string.Format("camparator.{0}.png", 1);
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
                    else if (_mouseNKeyListener.isActive)
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

        private bool isWindowClicked(int x, int y, Window window)
        {
            return (x < window.Left && x > window.Left - window.Width)
                && (y < window.Top && x > window.Top - window.Height);
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
        
    }
}
