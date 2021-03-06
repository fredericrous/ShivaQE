﻿using log4net;
using ShivaQEcommon;
using System;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Linq;

namespace ShivaQEmaster
{
	public partial class AddServerPage
	{
        const int _default_port = 1142;

        private static readonly ILog _log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        AddServerPageBindings _bindings;
        SlaveManager _slaveManager;
        Analytics _analytics;

		public AddServerPage()
		{
			this.InitializeComponent();

            _analytics = Analytics.Instance;
            _analytics.PageView("AddServer");

            _bindings = this.Resources["AddServerPageBindingsDataSource"] as AddServerPageBindings;
            _slaveManager = SlaveManager.Instance;
		}

        public AddServerPageBindings Bindings
        {
            get { return _bindings; }
            set { _bindings = value; }
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

        private bool add_local_warning = false;

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
                _bindings.error_color = "Red";
                string error_msg = "ip (or hostname) must be specified";
                _bindings.error_msg = error_msg;
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
                _bindings.error_color = "#FF0062AE";
                _bindings.error_msg = "You're about to add your master computer as a slave.\n"
                    + "It will result in infinite loop when you click or press a key.\n"
                    + "Click Add again to validate this choice?";

                if (!add_local_warning)
                {
                    add_local_warning = true;
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
                _log.Warn("cant parse port, using default " + _default_port, ex);
            }

            int portInHostname = getPortFromHostname(hostname);
            if (portInHostname != -1)
            {
                port = portInHostname;
                hostname = hostname.Substring(0, hostname.LastIndexOf(':'));
            }

            AddServer(hostname, port, friendlyname);

            //go back to homepage
            this.NavigationService.Navigate(new Uri("Pages/HomePage.xaml", UriKind.Relative));
        }

        private int getPortFromHostname(string hostname)
        {
            int port = -1;

            int indexColon = hostname.LastIndexOf(':');

            //has port
            if (indexColon == -1)
            {
                return port;
            }

            //is not a comma part of an ipv6 address
            if (hostname.Count(x => x == ':') > 1)
            {
                IPAddress address;
                if (!IPAddress.TryParse(hostname.Substring(0, indexColon), out address))
                {
                    return port;
                }
            }

            Int32.TryParse(hostname.Substring(indexColon + 1), out port);

            return port;
        }

        public delegate void errorEventHandler(string text);
        public static event errorEventHandler ErrorMsg;

        public virtual async void AddServer(string hostname, int port, string friendlyname)
        {
            try
            {
                await _slaveManager.Add(hostname, port, friendlyname);
                _analytics.Event("AddServer", "connected");
            }
            catch
            {
                ErrorMsg("Can't add host. Is slave activated?");
            }
        }
	}
}