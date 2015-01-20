using log4net;
using ShivaQEcommon;
using System.Collections.Generic;
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
        MainWindowBindings _bind_main;

        private static readonly ILog _log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public HomePage()
		{
			this.InitializeComponent();

            _bindings = this.Resources["HomePageBindingsDataSource"] as HomePageBindings;
            _bind_main = MainWindow.Bindings;
            this._mouseNKeyListener = MouseNKeyListener.Instance;
            this._slaveManager = SlaveManager.Instance;
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

            foreach (Slave slave in _bind_main.selectedSlaves)
            {
                tasks.Add(_slaveManager.Reconnect(slave));
            }

            // Task.WaitAll(tasks.ToArray());
            waitTasks(tasks);

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
        /// remove selected slave
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void bt_remove_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            _slaveManager.Remove(_bind_main.selectedSlaves);
        }


        private void bt_disconnect_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            List<Task> tasks = new List<Task>();

            foreach (Slave slave in _bind_main.selectedSlaves)
            {
                tasks.Add(_slaveManager.Disconnect(slave));
            }

            // Task.WaitAll(tasks.ToArray());
            waitTasks(tasks);

            lv_slaves.Items.Refresh();
        }

        private void bt_add_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            //this.NavigationService.Navigate(new Uri("Pages/AddServerPage.xaml", UriKind.Relative));
            AddServerPage page = new AddServerPage();
            AddServerPage.ErrorMsg += (error_msg) =>
                {
                   // _bindings.error_msg = error_msg;
                    MessageBox.Show(error_msg);
                };
            this.NavigationService.Navigate(page);
        }

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

        private void bt_show_record_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            _bind_main.flyout_record = true;
        }

	}
}