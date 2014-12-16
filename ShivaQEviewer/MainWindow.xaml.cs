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

namespace ShivaQEviewer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger
        (System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        MainWindowBindings _bindings;

        private string slaveList_save_path = "slavelist.json";

        public MainWindow()
        {
            InitializeComponent();

            _bindings = this.Resources["MainWindowBindingsDataSource"] as MainWindowBindings;
            this.DataContext = this; //deadcode?

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
            if (File.Exists(slaveList_save_path))
            {
                try
                {
                    string slaveListJson = File.ReadAllText(slaveList_save_path);
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
                    log.Error(string.Format("load json {0}", slaveList_save_path), ex);
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
            File.WriteAllText(slaveList_save_path, slaveListJson);
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
                ? hostname.Substring(0, (hostname.IndexOf('.') == 0? hostname.Length : hostname.IndexOf('.')))
                : _bindings.add_friendlyname;
            string login = _bindings.add_login;
            string password = pb_add_password.Password; //binding secure way is long to implement for not so much uses

            Slave slave = new Slave(hostname)
            {
                login = login,
                password = password,
                friendlyName = friendlyname
            };
            _bindings.slaves.Add(slave);

            //save list to file to be reloaded at next startup
            string slaveListJson = JsonConvert.SerializeObject(_bindings.slaves, Formatting.Indented);
            File.WriteAllText(slaveList_save_path, slaveListJson);

            bt_add_cancel_Click(null, e);
        }

        string executablePath = System.IO.Path.GetDirectoryName(System.Windows.Forms.Application.ExecutablePath).ToString() + "\\";
        private async Task<bool> deploy_display_launch(Slave slave)
        {
            _bindings.status += string.Format("{0} : copying slave to this server {1}", slave.hostname, Environment.NewLine);
            if (!copy_files(executablePath + "slave", String.Format(@"\\{0}\c$\ShivaQEslave", slave.hostname), slave.login, slave.password))
                return false;

            //open rdp
            _bindings.status += string.Format("{0} : openning rdp for this server {1}", slave.hostname, Environment.NewLine);
            executeCmd(Environment.ExpandEnvironmentVariables(@"%SystemRoot%\system32\mstsc.exe"),
                string.Format("/v:{0} /h:{1} /w:{2}",
                slave.ipAddress, _bindings.height, _bindings.width), false, slave);

            // wait rdp connection is done before executing psexec.
            // Psexec needs that rdp connection in order to gain access rights
            await Task.Delay(6000); 

            //start slave with psexec
            // option accepteula remove license warning
            // option i 2 tells psexec our software wants a ui
            // d tells not to wait for end of execution
            _bindings.status += string.Format("{0} : launch slave on this server {1}", slave.hostname, Environment.NewLine);
            executeCmd(executablePath + "PsExec.exe",
                string.Format(@"\\{0} -u ""{1}"" -p ""{2}"" -i 2 -accepteula -d ""C:\ShivaQEslave\ShivaQEslave.exe""",
                slave.ipAddress, slave.login, slave.password), true);

            return true;
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
                        tasks.Add(deploy_display_launch(slave));
                    }

                    Task.WaitAll(tasks.ToArray());
                    _bindings.status += string.Format("{0} DONE !!!", Environment.NewLine);
                });

        }

        private bool copy_files(string source_dir, string destination_dir, string login, string password)
        {
            string[] directories = Directory.GetDirectories(source_dir, "*", SearchOption.AllDirectories);
            string[] files = Directory.GetFiles(source_dir, "*", SearchOption.AllDirectories);

            ////doesn't work !?
            //if (destination_dir.Contains(':'))
            //{
            //    destination_dir = destination_dir.Replace(':', '-');
            //    destination_dir = destination_dir.Substring(0, destination_dir.IndexOf('\\', 2)) + ".ipv6-literal.net"
            //        + destination_dir.Substring(destination_dir.IndexOf('\\', 2));
            //}

            string domain = "localhost";
            if (login.IndexOf('\\') != -1)
            {
                string[] domainNlogin = login.Split('\\');
                domain = domainNlogin[0];
                login = domainNlogin[1];
            }

            //log user for distant copy paste
            using (Impersonation.LogonUser(domain, login, password, LogonType.NewCredentials)) // warning static
            {
                try
                {
                    if (Directory.Exists(destination_dir))
                    {
                        Directory.Delete(destination_dir, true);
                    }

                    Directory.CreateDirectory(destination_dir);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error deleting files: " + ex.Message);
                }

                foreach (string dir in directories)
                {
                    Directory.CreateDirectory(destination_dir + dir.Substring(source_dir.Length));
                }

                foreach (string file_name in files)
                {
                    try
                    {
                        File.Copy(file_name, destination_dir + file_name.Substring(source_dir.Length), true);
                    }
                    catch (Exception ex)
                    {
                        log.Error(string.Format("can't copy {0}", file_name), ex);
                    }
                }

                return true;
            }
        }

        private void executeCmd(string filename, string argument, bool hidden = false, Slave slave = null)
        {
            Process process = new Process();

            if (slave != null)
            {
                process.StartInfo.FileName = Environment.ExpandEnvironmentVariables(@"%SystemRoot%\system32\cmdkey.exe");
                process.StartInfo.Arguments = string.Format("/generic:TERMSRV/{0} /user:{1} /pass:{2}", slave.ipAddress, slave.login, slave.password);
                process.Start();
            }

            ProcessStartInfo startInfo = new ProcessStartInfo();
            if (hidden)
            {
                startInfo.WindowStyle = ProcessWindowStyle.Hidden;
                startInfo.CreateNoWindow = true;
            }
            startInfo.FileName = filename;
            startInfo.Arguments = argument;
            process.StartInfo = startInfo;
            process.Start();

            //if (slave != null)
            //{
            //    process.StartInfo.FileName = Environment.ExpandEnvironmentVariables(@"%SystemRoot%\system32\cmdkey.exe");
            //    process.StartInfo.Arguments = string.Format("/delete:TERMSRV/{0}", slave.ipAddress);
            //    process.Start();
            //}
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
