using System;

namespace ShivaQEmaster
{
	public partial class HelpPage
	{

		public HelpPage()
		{
			this.InitializeComponent();
		}

        private void bt_close_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            this.NavigationService.Navigate(new Uri("Pages/HomePage.xaml", UriKind.Relative));
        }
	}
}