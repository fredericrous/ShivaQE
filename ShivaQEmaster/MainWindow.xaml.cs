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
using System.Linq;

namespace ShivaQEmaster
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        private const int port = 1142;

        MainWindowBindings _bindings;
        MouseNKeyListener _mouseNKeyListener;
        slaveManager _slaveManager;
        UIChangeListener _uichange;

        private static readonly log4net.ILog log = log4net.LogManager.GetLogger
                (System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        static string lastKey = string.Empty; //there's a time out to reset lastKey 3sec after a press key
        static Timer doubleClickReset = new Timer() { Interval = 3000, AutoReset = false };

        public MainWindow()
        {
            InitializeComponent();

            _bindings = this.Resources["MainWindowBindingsDataSource"] as MainWindowBindings;
            this.DataContext = this; //deadcode?

            //wait window is loaded to subscribe to uichangelistener
            this.Loaded += (s, e) =>
            {
                //listen for windows events related to UI
                _uichange = new UIChangeListener(this);

                //on window creation, sync slaves to be sure all slaves have the same size of window as master
                _uichange.WindowCreated += async () =>
                    {
                        ActionType actionMethod = ActionType.None;
                        string actionValue = null;

                        actionMethod = ActionType.SetWindowPos;
                        actionValue = String.Join(".", _uichange.getActiveWindowInfo());

                        ActionMethod action = new ActionMethod()
                        {
                            method = actionMethod,
                            value = actionValue
                        };
                        await _slaveManager.Send<ActionMethod>(action);
                    };
            };

            //disconnect slaves and remove listerner
            this.Closed += (object sender, EventArgs e) =>
            {
                try
                {
                    _slaveManager.disconnectAll();
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

            _mouseNKeyListener = new MouseNKeyListener();
            _slaveManager = new slaveManager(_bindings.slaves);

            _mouseNKeyListener.Active();
            _mouseNKeyListener.MouseClick += async(s, ev) =>
            {
                try
                {
                    await _slaveManager.Send<MouseNKeyEventArgs>(ev);
                    //checkIdentical();
                }
                catch (IOException ex)
                {
                    log.Error("refresh list because", ex);
                    lv_slaves.Items.Refresh();
                }
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
                        switch (ev.keyData)
                        {
                            case "F5":

                                _mouseNKeyListener.Active(true);
                                return;
                            case "F6":

                                _mouseNKeyListener.DeactiveAll();
                                return;
                            case "F7": //should be useless because now it's automatic & systematic

                                actionMethod = ActionType.SetWindowPos;
                                actionValue = String.Join(".", _uichange.getActiveWindowInfo());

                                break;
                            case "F8":
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
                                break;
                            default:
                                break;
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
                        await _slaveManager.Send<MouseNKeyEventArgs>(ev);
                        //checkIdentical();
                    }

                }
                catch (IOException ex)
                {
                    log.Error("refresh list because", ex);
                    lv_slaves.Items.Refresh();
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
                    lv_slaves.Items.Refresh();
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
                    await _slaveManager.Send<ActionMethod>(action);
                };
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
        /// add slave to the list and autoconnect to it.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void bt_add_add_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            string hostname = _bindings.newSlaveIP.Trim();
            string friendlyname = _bindings.newSlaveName.Trim();

            //hostname is needed as an identifier on the network
            if (hostname == string.Empty)
            {
                MessageBox.Show("ip (or hostname) must be specified");
                return;
            }

            //a friendly name is otional and takes the name of the hostname if not specified
            if (friendlyname == string.Empty)
            {
                friendlyname = hostname;
            }

            //warning if you try to connect to slave a master
            if (hostname == "localhost" || hostname == "127.0.0.1" || hostname == "::1")
            {
                var dialogResult = System.Windows.Forms.MessageBox.Show("You're about to add your master computer as a slave. It will result in infinite loop when you click or press a key.\n"
                    + "Are you sure you want to do that?", "Warning", System.Windows.Forms.MessageBoxButtons.YesNo);

                if (dialogResult == System.Windows.Forms.DialogResult.No)
                {
                    return;
                }
            }

            //add new slave to list of slaves
            _slaveManager.add(hostname, port, friendlyname);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void cb_broadcast_movement_Checked(object sender, System.Windows.RoutedEventArgs e)
        {
            if (_bindings == null)
                return;
            _mouseNKeyListener.ActivateMouseMove();
        }


        private void cb_broadcast_movement_Unchecked(object sender, System.Windows.RoutedEventArgs e)
        {
            _mouseNKeyListener.DeactiveMouseMove();
        }

        /// <summary>
        /// reconnect slaves
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void bt_reconnect_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            foreach (Slave slave in _bindings.selectedSlaves)
            {
               // _slaveManager.reconnectAll();
                _slaveManager.reconnect(slave);
            }
            

            lv_slaves.Items.Refresh();
        }

        ///// <summary>
        ///// open a RDP connection with selected slave
        ///// </summary>
        ///// <param name="sender"></param>
        ///// <param name="e"></param>
        //private void bt_viewer_Click(object sender, System.Windows.RoutedEventArgs e)
        //{
        //    if (!hasSelectedSlave())
        //    {
        //        return;
        //    }

        //    System.Diagnostics.Process process = new System.Diagnostics.Process();
        //    System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo();
        //    startInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
        //    startInfo.FileName = "cmd.exe";
        //    startInfo.Arguments = string.Format("/C mstsc /v:{0}", _bindings.slaveSelected.ipAddress);
        //    process.StartInfo = startInfo;
        //    process.Start();
        //}

        /// <summary>
        /// open help window
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void bt_help_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            HelpWindow helpWindhow = new HelpWindow();
            helpWindhow.Show();
        }

        /// <summary>
        /// remove selected slave
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void bt_remove_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            foreach (var slave in _bindings.selectedSlaves)
            {
                _slaveManager.remove(slave.ipAddress);
            }
        }

        /// <summary>
        /// open recorder
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void bt_record_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            RecorderWindow recorderWindow = new RecorderWindow();
            recorderWindow.Show();
        }

        private void tb_host_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                tb_host.Focus();
            }
        }

        private void tb_name_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                bt_add_add_Click(sender, e);
            }
        }

        private void bt_close_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            _bindings.window_add = Visibility.Collapsed;
        }

        private void bt_add_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            _bindings.window_add = Visibility.Visible;
        }

        private void ts_broadcast_Checked(object sender, System.Windows.RoutedEventArgs e)
        {
            if (_bindings.checked_broadcast)
            {
                if (!_mouseNKeyListener.isActive)
                {
                    _mouseNKeyListener.Active(true);
                }
            }
            else
            {
                _mouseNKeyListener.DeactiveAll();
            }
        }

        
        /* 
         * TODO:
         * recorder
         * special key to deactivate current computer but not distant ones
         * analytics, zones les plus cliqués
         * 
         */
    }
}
