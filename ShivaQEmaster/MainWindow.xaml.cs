using ShivaQEcommon.Hook;
using ShivaQEcommon.Eventdata;
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Timers;
using System.Windows;
using System.Windows.Input;
using System.Diagnostics;
using Newtonsoft.Json;
using System.Net.Sockets;
using MahApps.Metro.Controls;
using log4net;
using System.Reflection;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
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
        UIChangeListener _uichange;
        Recorder _recorder;

        public static MainWindowBindings getBindings
        {
            get { return _bindings; }
        }

        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        static string lastKey = string.Empty; //there's a time out to reset lastKey 3sec after a press key
        static Timer doubleClickReset = new Timer() { Interval = 3000, AutoReset = false };
        
        Tuple<string, int[]> _activeWindowInfo = null;

        public MainWindow()
        {
            InitializeComponent();

            _bindings = this.Resources["MainWindowBindingsDataSource"] as MainWindowBindings;
            this.DataContext = this; //deadcode?

            _NavigationFrame.Navigate(new HomePage());

            //wait window is loaded to subscribe to uichangelistener
            this.Loaded += (s, e) =>
            {
                //listen for windows events related to UI
                _uichange = new UIChangeListener(this);

                //on window creation, sync slaves to be sure all slaves have the same size of window as master
                _uichange.WindowCreated += () =>
                    {
                        try
                        {
                            _activeWindowInfo = _uichange.getActiveWindowInfo();
                        }
                        catch (Exception ex)
                        {
                            log.Warn("error getting active window info", ex);
                            return;
                        }
                    };
            };

            _recorder = Recorder.Instance;
            _recorder.Init();

            //disconnect slaves and remove listerner
            this.Closed += (object sender, EventArgs e) =>
            {
                try
                {
                    _slaveManager.DisconnectAll();
                    _uichange.StopListener();
                    _mouseNKeyListener.DeactiveAll();
                    _uichange = null;
                    _mouseNKeyListener = null;
                }
                catch (Exception ex)
                {
                    log.Error("cant clean properly", ex);
                }
            };

            _mouseNKeyListener = MouseNKeyListener.Instance;
            _slaveManager = SlaveManager.Instance;
            _slaveManager.Init(_bindings.slaves);

            _mouseNKeyListener.Active();
            _mouseNKeyListener.MouseClick += (s, ev) =>
            {
                //send winpos before click

                Task.Run( async () =>
                    {
                        //should be raised by windowcreated event but it doesn't work weel so it's a workaround...
                        try
                        {
                            _activeWindowInfo = _uichange.getActiveWindowInfo();
                        }
                        catch (Exception ex)
                        {
                            log.Warn("error getting active window info", ex);
                            _activeWindowInfo = null;
                        }
                        if (_activeWindowInfo != null)
                        {
                            sendWindowPos(_activeWindowInfo);
                        }

                        //record click
                        _recorder.Record(ev);

                        //send click
                        try
                        {
                            await _slaveManager.Send<MouseNKeyEventArgs>(ev);
                            //checkIdentical();
                        }
                        catch (IOException ex)
                        {
                            log.Error("refresh list because", ex);
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
                            _bindings.checked_broadcast = true;
                            return;
                        }
                        else if (ev.keyData == key_sendmousekey_off)
                        {
                            _mouseNKeyListener.DeactiveAll();
                            _bindings.checked_broadcast = false;
                            return;
                        }
                        else if (ev.keyData == key_force_resize)
                        {
                            actionMethod = ActionType.SetWindowPos;
                            Tuple<string, int[]> activeWindowInfo = null;
                            try
                            {
                                activeWindowInfo = _uichange.getActiveWindowInfo();
                            }
                            catch (Exception ex)
                            {
                                log.Warn("error getting active window info", ex);
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
                        _recorder.Record(ev);

                        //send input
                        await _slaveManager.Send<MouseNKeyEventArgs>(ev);

                        //checkIdentical();
                    }

                }
                catch (IOException ex)
                {
                    log.Error("refresh list because", ex);
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
                    log.Error("refresh list because", ex);
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
                        log.Error("can't send clipboard update", ex);
                    }
                };
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
                System.Threading.Thread.Sleep(1000);
            }
            catch (Exception ex)
            {
                log.Error("error send window created", ex);
            }
        }

        private async void checkIdentical()
        {
            ActionType actionMethod = ActionType.None;
            string actionValue = null;

            actionMethod = ActionType.CheckIdentical;
            actionValue = JsonConvert.SerializeObject(_uichange.getEventCalls);

            ActionMethod action = new ActionMethod()
            {
                method = actionMethod,
                value = actionValue
            };
             await _slaveManager.Send<ActionMethod>(action);
             foreach (Slave slave in _slaveManager.slaveList)
             {
                NetworkStream networkStream = slave.client.GetStream();
                // TODO      networkStream.ReadAsync(
             }

             return;
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
        
        /* 
         * TODO:
         * recorder - behavior testing
         * analytics, zones les plus cliqués
         * 
         */
    }
}
