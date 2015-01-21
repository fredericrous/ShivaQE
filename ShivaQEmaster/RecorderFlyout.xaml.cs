using log4net;
using ShivaQEcommon;
using System;
using System.Reflection;
using System.Windows.Forms;
using System.Windows.Media.Animation;

namespace ShivaQEmaster
{
	public partial class RecorderFlyout
	{
        private static readonly ILog _log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        RecorderFlyoutBindings _bindings;
        Storyboard _sb_beginRecord;
        Storyboard _sb_stopRecord;
        Recorder _recorder;

		public RecorderFlyout()
		{
			this.InitializeComponent();

            _bindings = this.Resources["RecorderFlyoutBindingsDataSource"] as RecorderFlyoutBindings;
            _sb_beginRecord = this.FindResource("sb_beginRecord") as Storyboard;
            _sb_stopRecord = this.FindResource("sb_stopRecord") as Storyboard;
            _recorder = Recorder.Instance;
            _recorder.TimeElapsed += (elapsed) =>
            {
                _bindings.time_elapsed = elapsed;
            };
		}

		private void ts_record_IsCheckedChanged(object sender, System.EventArgs e)
		{
            if (_bindings.checked_record)
            {
                //animation
                _sb_stopRecord.Remove();
                _sb_beginRecord.Begin();

                _recorder.Start();

            }
            else
            {
                _sb_beginRecord.Stop();
                _sb_stopRecord.Begin();
                _recorder.Stop();

                SaveFileDialog saveFileDialog = new SaveFileDialog();
                saveFileDialog.Filter = "Recorded Scenario|*.zip|All Files|*.*";
                saveFileDialog.Title = "Save a record";
                saveFileDialog.ShowDialog();
                if (saveFileDialog.FileName != "")
                {
                    _recorder.Save(saveFileDialog.FileName);
                }

                _bindings.time_elapsed = new TimeSpan();
            }
		}
		

		private void rec_time_bar_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
		{
			_log.Info("test");
		}

		private void bt_load_Click(object sender, System.Windows.RoutedEventArgs e)
		{
            OpenFileDialog openFileDialog_Load = new OpenFileDialog();
            openFileDialog_Load.Filter = "Recorded Scenario|*.zip|All Files|*.*";
            openFileDialog_Load.FilterIndex = 1;

            DialogResult userClickedOK = openFileDialog_Load.ShowDialog();

            if (userClickedOK == DialogResult.OK)
            {
                _recorder.Load(openFileDialog_Load.FileName);
            }
		}

        //private void bt_play_Click(object sender, System.Windows.RoutedEventArgs e)
        //{
        //    _recorder.Play();
        //}

		private void bt_save_Click(object sender, System.Windows.RoutedEventArgs e)
		{
            _recorder.Save();
		}

		private void bt_execute_Click(object sender, System.Windows.RoutedEventArgs e)
		{
            _recorder.Play();
		}

		private void bt_preview_Click(object sender, System.Windows.RoutedEventArgs e)
		{
            _recorder.Preview();
		}
	}
}