using log4net;
using ShivaQEcommon;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Documents;

namespace ShivaQEmaster
{
	public partial class HomePage
	{
        HomePageBindings _bindings;
        MouseNKeyListener _mouseNKeyListener;
        SlaveManager _slaveManager;

        private static readonly ILog _log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public HomePage()
		{
			this.InitializeComponent();

            _bindings = this.Resources["HomePageBindingsDataSource"] as HomePageBindings;
            this._mouseNKeyListener = MouseNKeyListener.Instance;
            this._slaveManager = SlaveManager.Instance;

            _bindings.slaves = _slaveManager.slaveList; //bind slavelist to listview

            //update ui when the add server form raise an error
            AddServerPage.ErrorMsg += (str) => //couldnt make work binding so doing it old fashion
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
		}

        /// <summary>
        /// wait all tasks are done
        /// </summary>
        /// <param name="tasks"></param>
        private async void waitTasks(List<Task> tasks)
        {
            await Task.WhenAll(tasks);
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
            waitTasks(tasks);

            lv_slaves.Items.Refresh();
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

            waitTasks(tasks);

            lv_slaves.Items.Refresh();
        }

        /// <summary>
        /// navigate to the add slave form
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void bt_add_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            this.NavigationService.Navigate(new AddServerPage());
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