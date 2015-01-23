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

        static MainWindowBindings _bindings;
        SlaveManager _slaveManager;

        public static MainWindowBindings Bindings
        {
            get { return _bindings; }
        }

        public MainWindow()
        {
            InitializeComponent();

            _bindings = this.Resources["MainWindowBindingsDataSource"] as MainWindowBindings;
            this.DataContext = this; //deadcode?

            //Analytics analytics = new Analytics("ShivaQE Viewer", "1.0");
            //analytics.Exception(new IOException());

            Deployer.CmdLauncherExists();

            //remove rdp certificat warning
            string key = @"Software\Microsoft\Terminal Server Client\AuthenticationLevelOverride";
            if (Registry.CurrentUser.OpenSubKey(key) == null)
            {
                var registryKey = Registry.CurrentUser.CreateSubKey(@"SOFTWARE\Microsoft\Terminal Server Client");
                registryKey.SetValue("AuthenticationLevelOverride", 0x00, RegistryValueKind.DWord);
            }

            //prevent rdp to go in GUI-Less mode
            key = @"Software\Microsoft\Terminal Server Client\RemoteDesktop_SuppressWhenMinimized";
            if (Registry.CurrentUser.OpenSubKey(key) == null)
            {
                var registryKey = Registry.CurrentUser.CreateSubKey(@"SOFTWARE\Microsoft\Terminal Server Client");
                registryKey.SetValue("RemoteDesktop_SuppressWhenMinimized", 0x02, RegistryValueKind.DWord);
            }

            _slaveManager = SlaveManager.Instance;
            _slaveManager.Init(new ObservableCollection<Slave>());

            _NavigationFrame.Navigate(new HomePage());

        }

        /// <summary>
        /// open help window
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void bt_help_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            _NavigationFrame.Navigate(new HelpPage());
        }

    }
}
