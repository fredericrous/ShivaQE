using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace ShivaQEmaster
{
	/// <summary>
	/// Interaction logic for NotifyWindow.xaml
	/// </summary>
	public partial class NotifyWindow : Window
	{
        public NotifyWindow()
        {
            this.InitializeComponent();

            setWindowPosition();
        }

		public NotifyWindow(string text)
		{
			this.InitializeComponent();

            setWindowPosition();
            this.tb_warning.Text = text;
		}

        /// <summary>
        /// set the position on screen of the window
        /// always bottom right except when taskbar is at the right of the screen
        /// </summary>
        private void setWindowPosition()
        {
            System.Drawing.Rectangle resolution = Screen.PrimaryScreen.Bounds;

            TaskBarLocation taskBarLocation = GetTaskBarLocation();

            //default taskbar location is bottom
            double left = resolution.Width - Window.Width - 5;
            double top = resolution.Height - Window.Height - 50;

            if (taskBarLocation == TaskBarLocation.LEFT || taskBarLocation == TaskBarLocation.TOP)
            {
                top = resolution.Height - Window.Height - 5;
            }
            else if (taskBarLocation == TaskBarLocation.RIGHT)
            {
                left = 5;
                top = resolution.Height - Window.Height - 5;
            }

            this.Left = left;
            this.Top = top;
        }

		private void Window_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
		{
   			if (e.ChangedButton == MouseButton.Left)
			{
        		this.DragMove();
			}
		}

        private enum TaskBarLocation { TOP, BOTTOM, LEFT, RIGHT }

        private TaskBarLocation GetTaskBarLocation()
        {
            if (SystemParameters.WorkArea.Left > 0)
                return TaskBarLocation.LEFT;
            if (SystemParameters.WorkArea.Top > 0)
                return TaskBarLocation.TOP;
            if (SystemParameters.WorkArea.Left == 0
              && SystemParameters.WorkArea.Width < SystemParameters.PrimaryScreenWidth)
                return TaskBarLocation.RIGHT;
            return TaskBarLocation.BOTTOM;
        }

        private void bt_close_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            //this.Close();
            this.Hide();
        }

        //public ImageSource NotifyIconTest
        //{
        //    set
        //    {
        //        this.img_slave.Source = value;
        //    }
        //}

        public byte[] NotifyIcon
        {
            set
            {
                BitmapImage imgSource = new BitmapImage();
                imgSource.BeginInit();
                imgSource.StreamSource = new MemoryStream(value);
                imgSource.EndInit();
                this.img_slave.Source = imgSource;
            }
        }
    }
}