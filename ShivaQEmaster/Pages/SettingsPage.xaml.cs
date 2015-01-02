using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Configuration;

namespace ShivaQEmaster
{
	public partial class SettingsPage
	{
        SettingsPageBindings _bindings;

		public SettingsPage()
		{
			this.InitializeComponent();

            _bindings = this.Resources["SettingsPageBindingsDataSource"] as SettingsPageBindings;
		}

		private void bt_close_Click(object sender, System.Windows.RoutedEventArgs e)
		{
            this.NavigationService.Navigate(new Uri("Pages/HomePage.xaml", UriKind.Relative));
		}
	}
}