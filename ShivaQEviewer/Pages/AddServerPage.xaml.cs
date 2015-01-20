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

namespace ShivaQEviewer
{
    /// <summary>
    /// Interaction logic for AddServerPage.xaml
    /// </summary>
    public partial class AddServerPage : Page
    {
        AddServerPageBindings _bindings;
        SlaveManager _slaveManager;
        private string _slaveList_save_path = "slavelist.json";

        public AddServerPage()
        {
            InitializeComponent();

            _bindings = this.Resources["AddServerPageBindingsDataSource"] as AddServerPageBindings;
            _slaveManager = SlaveManager.Instance;
        }

        private void bt_add_add_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            string hostname = _bindings.add_hostname.Trim();
            if (string.IsNullOrEmpty(hostname))
            {
                MessageBox.Show("specify a hostname first");
                return;
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
                    friendlyName = friendlyname
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
