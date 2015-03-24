using log4net;
using ShivaQEcommon;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Reflection;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Documents;

namespace ShivaQEmaster
{
	public partial class HomePage
	{
        HomePageBindings _bindings;
        MouseNKeyListener _mouseNKeyListener;
        SlaveManager _slaveManager;
        Analytics _analytics;

        private static readonly ILog _log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public HomePage()
		{
			this.InitializeComponent();

            _analytics = Analytics.Instance;
            _analytics.PageView("Home");

            _bindings = this.Resources["HomePageBindingsDataSource"] as HomePageBindings;
            this._mouseNKeyListener = MouseNKeyListener.Instance;
            this._slaveManager = SlaveManager.Instance;

            _slaveManager.Disconnected += () =>
            {
                lv_slaves.Items.Refresh();
            };

            _bindings.slaves = _slaveManager.slaveList; //bind slavelist to listview

            //update ui when the add server form raise an error
            AddServerPage.ErrorMsg += (str) => //couldnt make work binding so doing it old fashion
            {
                this.Dispatcher.Invoke(() =>
                {
                    this.lb_error.Text = str;
                });
            };

            EditServerPage.ErrorEditMsg += (str) => //couldnt make work binding so doing it old fashion
            {
                this.Dispatcher.Invoke(() =>
                {
                    this.lb_error.Text = str;
                });
            };

            //update toggleswitch when broadcast is activate or deactivated
            MainWindow.UpdateBroadcastStatus += (status) =>
                {
                    this.Dispatcher.Invoke(() =>
                    {
                        this.ts_broadcast.IsChecked = status;
                    });
                };

            MainWindow.ErrorMsg += (text) =>
                {
                    this.Dispatcher.Invoke(() =>
                    {
                        this.lb_error.Text = text;
                    });
                };
		}

        /// <summary>
        /// wait all tasks are done
        /// </summary>
        /// <param name="tasks"></param>
        private async void waitTasksThenRefresh(List<Task> tasks)
        {
            await Task.WhenAll(tasks);

          //  _bindings.slaves = _slaveManager.slaveList;
            //lv_slaves.ItemsSource = _slaveManager.slaveList; //wont refresh otherwise...items.refresh() doesnt seem to do the work
            lv_slaves.Items.Refresh();
        }

        /// <summary>
        /// reconnect slaves
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void bt_reconnect_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            List<Task> tasks = new List<Task>();

            foreach (Slave slave in _bindings.selectedSlaves)
            {
                tasks.Add(ReconnectServer(slave));
            }

            // Task.WaitAll(tasks.ToArray());
            waitTasksThenRefresh(tasks);

            _analytics.Event("HomePage", "Reconnect");
        }

        private async Task ReconnectServer(Slave slave, bool disconnect = false)
        {
            try
            {
                if (disconnect)
                {
                    await _slaveManager.Disconnect(slave);
                }
                else
                {
                    await _slaveManager.Reconnect(slave);
                }
            }
            catch (Exception ex)
            {
                _bindings.error_msg = ex.Message;
            }
        }

        /// <summary>
        /// remove selected slave
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void bt_remove_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            _slaveManager.Remove(_bindings.selectedSlaves);

            _analytics.Event("HomePage", "Remove");
        }

        /// <summary>
        /// disconnect selected slave
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void bt_disconnect_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            List<Task> tasks = new List<Task>();

            foreach (Slave slave in _bindings.selectedSlaves)
            {
                tasks.Add(ReconnectServer(slave, true));
            }

            waitTasksThenRefresh(tasks);

            _analytics.Event("HomePage", "Disconnect");
        }

        /// <summary>
        /// navigate to the add slave form
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void bt_add_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            this.NavigationService.Navigate(new AddServerPage());

            _analytics.Event("HomePage", "Add");
        }

        /// <summary>
        /// navigate to the edit slave form
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void bt_edit_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            Slave firstSlave = null;
            int slavesCount = 0;
            foreach (Slave slave in _bindings.selectedSlaves)
            {
                firstSlave = slave;
                slavesCount++;
                if (slavesCount > 1)
                {
                    break;
                }
            }

            if (firstSlave == null)
            {
                return;
            }
            if (slavesCount > 1)
            {
                _bindings.error_msg = "Only one slave can be edited at a time";
                return;
            }
            this.NavigationService.Navigate(new EditServerPage(firstSlave));

            _analytics.Event("HomePage", "Edit");
        }

        /// <summary>
        /// Activate or deactivate broadcast of mouse and key input
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ts_broadcast_IsCheckedChanged(object sender, System.EventArgs e)
        {
            //if (_bindings.checked_broadcast)
            if (ts_broadcast.IsChecked == true)
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

            _analytics.Event("HomePage", "Broadcast change");
        }

        /// <summary>
        /// display the record flyout
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void bt_show_record_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            MainWindow.Bindings.flyout_record = true;
        }
    }
}