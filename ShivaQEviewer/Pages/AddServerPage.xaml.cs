using SimpleImpersonation;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Forms;
using MessageBox = System.Windows.MessageBox;
using Microsoft.Win32;
using Newtonsoft.Json;
using System.Collections.ObjectModel;
using log4net;
using System.Reflection;
using ShivaQEcommon;
using System.Net;

namespace ShivaQEviewer
{
    /// <summary>
    /// Interaction logic for AddServerPage.xaml
    /// </summary>
    public partial class AddServerPage : Page
    {
        const int _default_port = 1142;

        private static readonly ILog _log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        AddServerPageBindings _bindings;
        SlaveManager _slaveManager;
        public static string _slaveList_save_path = "slavelist.json";

        public AddServerPage()
        {
            InitializeComponent();

            _bindings = this.Resources["AddServerPageBindingsDataSource"] as AddServerPageBindings;
            _slaveManager = SlaveManager.Instance;
        }

        public AddServerPageBindings Bindings
        {
            get { return _bindings; }
            set { _bindings = value; }
        }

        public virtual void bt_add_add_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            string hostname = _bindings.add_hostname.Trim();
            if (string.IsNullOrEmpty(hostname))
            {
                MessageBox.Show("specify a hostname first");
                return;
            }

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


            string friendlyname = string.IsNullOrEmpty(_bindings.add_friendlyname) && Char.IsLetter(hostname[0])
                ? hostname.Substring(0, (hostname.IndexOf('.') == -1 ? hostname.Length : hostname.IndexOf('.')))
                : _bindings.add_friendlyname;
            string login = _bindings.add_login;
            string password = pb_add_password.Password; //binding secure way is long to implement for not so much uses

            try
            {
                Slave slave = new Slave(hostname)
                {
                    login = login,
                    password = password,
                    friendlyName = friendlyname,
                    port = port
                };
                _slaveManager.Add(slave);

                bt_add_cancel_Click(null, e);
            }
            catch (Exception ex)
            {
               // MessageBox.Show(ex.Message);
                _bindings.error_msg = ex.Message;
            }


            //save list to file to be reloaded at next startup
            string slaveListJson = JsonConvert.SerializeObject(_slaveManager.slaveList, Formatting.Indented);
            File.WriteAllText(_slaveList_save_path, slaveListJson);

        }

        // this function is a doublon with AddServerPage from ShivaQEmaster
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

        private void bt_add_cancel_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            this.NavigationService.Navigate(new Uri("Pages/HomePage.xaml", UriKind.Relative));
        }

        private void pb_add_password_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                bt_add_add_Click(null, e);
            }
        }
    }
}
