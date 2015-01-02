using log4net;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace ShivaQEmaster
{
	public partial class RecorderFlyout
	{
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

		public RecorderFlyout()
		{
			this.InitializeComponent();

			// Insert code required on object creation below this point.
		}

		private void ts_broadcast_IsCheckedChanged(object sender, System.EventArgs e)
		{
			// TODO: Add event handler implementation here.
		}

		private void rec_time_bar_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
		{
			log.Info("test");
		}
	}
}