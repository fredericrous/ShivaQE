using ShivaQEcommon;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ShivaQEviewer
{
    /// <summary>
    /// Interaction logic for ResolutionPage.xaml
    /// </summary>
    public partial class ResolutionPage : Page
    {
        ResolutionPageBindings _bindings;
        MainWindowBindings _bind_main;
        SlaveManager _slaveManager;

        public ResolutionPage()
        {
            InitializeComponent();

            Analytics analytics = Analytics.Instance;
            analytics.PageView("Resolution");

            _bindings = this.Resources["ResolutionPageBindingsDataSource"] as ResolutionPageBindings;
            _bind_main = MainWindow.Bindings;
            this.DataContext = this;
            _slaveManager = SlaveManager.Instance;

            //auto fill textboxes width/height, for opening computers to view, to the size of the current computer
            System.Drawing.Rectangle resolution = Screen.PrimaryScreen.Bounds;
            _bindings.width = resolution.Width.ToString();
            _bindings.height = resolution.Height.ToString();
            
        }

        private void bt_resolution_cancel_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            this.NavigationService.Navigate(new Uri("Pages/HomePage.xaml", UriKind.Relative));
        }

        public delegate void updateEventHandler(string text);
        public static event updateEventHandler UpdatesStatus;

        static List<Deployer> _deployers;

        private void bt_resolution_ok_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            this.NavigationService.Navigate(new Uri("Pages/HomePage.xaml", UriKind.Relative));
            _bind_main.flyout_log = true;

            //we need a thread to not make the UI hang out
            Task.Run(() =>
            {
                AppDomain.CurrentDomain.SetPrincipalPolicy(PrincipalPolicy.WindowsPrincipal); //for copy paste purpose

                //we list the task only to determine when they'll all be done
                List<Task> tasks = new List<Task>();
                _deployers = new List<Deployer>();
                foreach (var slave in _slaveManager.slaveList)
                {
                    var deploy = new Deployer(slave, _bindings.height, _bindings.width);
                    
                    deploy.UpdateStatus += (status) =>
                    {
                        UpdatesStatus(status);
                    };
                    tasks.Add(deploy.Run());
                    _deployers.Add(deploy);
                }

                Task.WaitAll(tasks.ToArray());
                _bind_main.status += string.Format("{0} DONE !!!", Environment.NewLine);
            });

        }

        internal static void Skip()
        {
            foreach (Deployer deployer in _deployers)
            {
                deployer.Skip();
            }
        }
    }
}
