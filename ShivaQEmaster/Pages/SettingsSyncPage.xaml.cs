using log4net;
using ShivaQEcommon;
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
	public partial class SettingsSyncPage
    {
        private static readonly ILog _log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        static string _currentLogin = Environment.UserDomainName + "\\" + Environment.UserName;
        SettingsSyncPageBindings _bindings;
        SlaveManager _slaveManager;

        public delegate void SyncDoneEventHandler();
        public static event SyncDoneEventHandler SyncDone;

		public SettingsSyncPage()
		{
			this.InitializeComponent();

            Analytics analytics = Analytics.Instance;
            analytics.PageView("SettingsSync");

            _bindings = this.Resources["SettingsSyncPageBindingsDataSource"] as SettingsSyncPageBindings;
            _bindings.login = _currentLogin;

            _slaveManager = SlaveManager.Instance;
		}

		private void tb_path_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
		{
            if (e.Key == Key.Enter)
            {
                tb_login.Focus();
            }
		}

		private void tb_login_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
		{
            if (e.Key == Key.Enter)
            {
                pb_add_password.Focus();
            }
		}

        private void pb_add_password_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                bt_sync_Click(sender, e);
            }
        }

		private void bt_close_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            this.NavigationService.Navigate(new Uri("Pages/HomePage.xaml", UriKind.Relative));
		}

		private void bt_sync_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_bindings.path))
            {
                _bindings.error_msg = string.Format("You must specify a path to {0}", _slaveManager.SlaveListPath);
                //UpdateStatus(error_msg);
                return;
            }

            if (_bindings.path.Substring(_bindings.path.LastIndexOf('\\') + 1) != _slaveManager.SlaveListPath)
            {
                _bindings.error_msg = string.Format("Path must end with filename {0}", _slaveManager.SlaveListPath);
                return;
            }

            string login = _bindings.login.Trim();

            try
            {
                CopyOverNetwork.UpdateStatus += (status) =>
                {
                    _bindings.error_msg = status.Contains("success") ? "success" : status.Substring(status.IndexOf(":") + 1) ;
                };
                string destination_path = System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().GetName().CodeBase).Replace("file:\\", "");
                if (login == string.Empty || login == _currentLogin)
                {
                    CopyOverNetwork.CopyFiles(_bindings.path, destination_path);
                }
                else
                {
                    CopyOverNetwork.CopyFiles(_bindings.path, destination_path, login, pb_add_password.Password);
                }
            }
            catch (Exception ex)
            {
                _bindings.error_msg = string.Format("Copy error: {0}", ex.Message);
                _log.Error(_bindings.error_msg);
            }
            SyncDone();
		}
	}
}