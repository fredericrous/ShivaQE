using log4net;
using ShivaQEcommon;
using System;
using System.Reflection;
using System.Windows.Forms;
using System.Windows.Media.Animation;

namespace ShivaQEviewer
{
	public partial class LogFlyout
	{
        public LogFlyout()
		{
			this.InitializeComponent();
            ResolutionPage.UpdatesStatus += (str, newStart) => //couldnt make work binding so doing it old fashion
                {
                    this.Dispatcher.Invoke(() => {
                        if (newStart)
                        {
                            str = Environment.NewLine + Environment.NewLine + Environment.NewLine + str;
                        }
                        this.tb_status.Text += str + Environment.NewLine;
                    });
                };
		}

        private void Page_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            ResolutionPage.Skip();
        }

	}
}