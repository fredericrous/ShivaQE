using ShivaQEcommon;
using System;

namespace ShivaQEviewer
{
	public partial class HelpPage
	{

		public HelpPage()
		{
			this.InitializeComponent();

            Analytics analytics = Analytics.Instance;
            analytics.PageView("Help");
		}

        private void bt_close_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            this.NavigationService.Navigate(new Uri("Pages/HomePage.xaml", UriKind.Relative));
        }
	}
}