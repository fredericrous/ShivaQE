using System;

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