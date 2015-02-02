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
	public partial class HomePage
	{
        private static readonly ILog _log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        HomePageBindings _bindings;
        SlaveManager _slaveManager;

        public HomePage()
        {
            InitializeComponent();

            _bindings = this.Resources["HomePageBindingsDataSource"] as HomePageBindings;
            this.DataContext = this; //deadcode?

            _slaveManager = SlaveManager.Instance;
            this.lv_slaves.ItemsSource = _slaveManager.slaveList; //slaveManager.Init(_bindings.slave) doesnt work...so we need this
        }

        private void bt_add_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            AddServerPage page = new AddServerPage();
            page.Unloaded += (_s, _e) =>
            {
                lv_slaves.Items.Refresh();
            };
            this.NavigationService.Navigate(page);
        }

        private void bt_remove_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            _slaveManager.Remove(_bindings.slaveSelected);
            string slaveListJson = JsonConvert.SerializeObject(_slaveManager.slaveList, Formatting.Indented);
            File.WriteAllText(AddServerPage._slaveList_save_path, slaveListJson);
        }

        private void bt_view_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            this.NavigationService.Navigate(new ResolutionPage());
        }

        private void bt_edit_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (_bindings.slaveSelected == null)
            {   
                return;
            }

            EditServerPage page = new EditServerPage(_bindings.slaveSelected);
            page.Unloaded += (_s, _e) =>
            {
                lv_slaves.Items.Refresh();
            };
            this.NavigationService.Navigate(page);
        }


	}
}