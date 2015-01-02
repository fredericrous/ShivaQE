using log4net;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace ShivaQEmaster
{
	public partial class AddServerPage
	{
        const int _default_port = 1142;

        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        AddServerPageBindings _bindings;
        SlaveManager _slaveManager;

		public AddServerPage()
		{
			this.InitializeComponent();

            _bindings = this.Resources["AddServerPageBindingsDataSource"] as AddServerPageBindings;
            _slaveManager = SlaveManager.Instance;
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
            this.NavigationService.Navigate(new Uri("Pages/HomePage.xaml", UriKind.Relative));
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

            int port = _default_port;
            try
            {
                port = Int32.Parse(SettingsManager.ReadSetting("port"));
            }
            catch (Exception ex)
            {
                log.Error("cant parse port, using default " + _default_port, ex);
            }
            _slaveManager.Add(hostname, port, friendlyname);
        }
	}
}