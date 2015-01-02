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
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        MainWindowBindings _bindings;

        private string _slaveList_save_path = "slavelist.json";

        public MainWindow()
        {
            InitializeComponent();

            _bindings = this.Resources["MainWindowBindingsDataSource"] as MainWindowBindings;
            this.DataContext = this; //deadcode?

            //Analytics analytics = new Analytics("ShivaQE Viewer", "1.0");
            //analytics.Exception(new IOException());

            Deployer.CmdLauncherExists();

            System.Drawing.Rectangle resolution = Screen.PrimaryScreen.Bounds;
            _bindings.width = resolution.Width.ToString();
            _bindings.height = resolution.Height.ToString();

            //remove rdp certificat warning
            string key = @"Software\Microsoft\Terminal Server Client\AuthenticationLevelOverride";
            if (Registry.CurrentUser.OpenSubKey(key) == null)
            {
                var registryKey = Registry.CurrentUser.CreateSubKey(@"SOFTWARE\Microsoft\Terminal Server Client");
                registryKey.SetValue("AuthenticationLevelOverride", 0x00, RegistryValueKind.DWord);
            }

            //load serverlist json save
            if (File.Exists(_slaveList_save_path))
            {
                try
                {
                    string slaveListJson = File.ReadAllText(_slaveList_save_path);
                    ObservableCollection<Slave> slaveListFromJson = JsonConvert.DeserializeObject<ObservableCollection<Slave>>(slaveListJson);
                   
                    foreach (var slave in slaveListFromJson)
                    {
                        Slave newSlave = new Slave(slave.hostname)
                        {
                            login = slave.login,
                            password = slave.password,
                            friendlyName = slave.friendlyName,
                            ipAddress = slave.ipAddress
                        };
                        _bindings.slaves.Add(slave);
                    }
                    slaveListFromJson = null;
                }
                catch (Exception ex)
                {
                    log.Error(string.Format("load json {0}", _slaveList_save_path), ex);
                }
            }
        }

        private void bt_add_Click(object sender, System.Windows.RoutedEventArgs e)
        {
           _bindings.window_add = Visibility.Visible;
        }

        private void bt_remove_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            _bindings.slaves.Remove(_bindings.slaveSelected);
            string slaveListJson = JsonConvert.SerializeObject(_bindings.slaves, Formatting.Indented);
            File.WriteAllText(_slaveList_save_path, slaveListJson);
        }

        private void bt_view_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            _bindings.window_resolution = Visibility.Visible;
        }

        private void bt_add_cancel_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            _bindings.window_add = Visibility.Collapsed;
        }

        private void bt_add_add_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            string hostname = _bindings.add_hostname.Trim();
            if (string.IsNullOrEmpty(hostname))
            {
                MessageBox.Show("specify a hostname first");
                return ;
            }

            string friendlyname = string.IsNullOrEmpty(_bindings.add_friendlyname) && Char.IsLetter(hostname[0])
                ? hostname.Substring(0, (hostname.IndexOf('.') == -1? hostname.Length : hostname.IndexOf('.')))
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
                _bindings.slaves.Add(slave);

                bt_add_cancel_Click(null, e);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }


            //save list to file to be reloaded at next startup
            string slaveListJson = JsonConvert.SerializeObject(_bindings.slaves, Formatting.Indented);
            File.WriteAllText(_slaveList_save_path, slaveListJson);

        }

        private void bt_resolution_ok_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            _bindings.window_resolution = Visibility.Collapsed;
            _bindings.window_done = Visibility.Visible;

            AppDomain.CurrentDomain.SetPrincipalPolicy(PrincipalPolicy.WindowsPrincipal); //for copy paste purpose

            //we need a thread to not make the UI hang out
            Task.Run(() =>
                {
                    //we list the task only to determine when they'll all be done
                    List<Task> tasks = new List<Task>();

                    foreach (var slave in _bindings.slaves)
                    {
                        var deploy = new Deployer(slave, _bindings.height, _bindings.width);
                        deploy.UpdateStatus += (status) =>
                            {
                                _bindings.status += status;
                            };
                        tasks.Add(deploy.Run());
                    }

                    Task.WaitAll(tasks.ToArray());
                    _bindings.status += string.Format("{0} DONE !!!", Environment.NewLine);
                });

        }

        private void pb_add_password_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                bt_add_add_Click(null, e);
            }
        }

        private void Window_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (_bindings.window_resolution == Visibility.Visible && e.Key == Key.Enter)
            {
                bt_resolution_ok_Click(null, e);
            }
        }

        private void bt_resolution_cancel_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            _bindings.window_resolution = Visibility.Collapsed;
        }

        private void bt_done_cancel_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            _bindings.window_done = Visibility.Collapsed;
            _bindings.status = string.Empty;
        }
    }
}
