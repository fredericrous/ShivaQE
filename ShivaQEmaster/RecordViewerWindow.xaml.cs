using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Timers;
using log4net;
using System.Reflection;
using ShivaQEcommon.Eventdata;
using ShivaQEcommon;

namespace ShivaQEmaster
{
	/// <summary>
	/// Interaction logic for RecordViewer.xaml
	/// </summary>
	public partial class RecordViewerWindow
    {
        private static readonly ILog _log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        RecordViewerWindowBindings _bindings;

        public RecordViewerWindow(bool execute = true)
		{
			this.InitializeComponent();

            Analytics analytics = Analytics.Instance;
            analytics.PageView("RecorderViewer");

            _bindings = this.Resources["RecordViewerWindowBindingsDataSource"] as RecordViewerWindowBindings;

            if (execute)
            {
                AddTrace("Note: execution occurs only on slaves.");
            }

            AddTrace("Starting...");
		}

        /// <summary>
        /// Write text to the list of action at the right of the preview screen
        /// </summary>
        /// <param name="text"></param>
        public void AddTrace(string text)
        {
            TextBlock tb = new TextBlock();
            tb.Text = text;
            tb.Foreground = Brushes.White;
            this.sp_events.Children.Add(tb);
        }

        /// <summary>
        /// Update text and image on the preview window at time given by 'timestamp'
        /// </summary>
        /// <param name="timespan">when to update preview screen</param>
        /// <param name="uriImage">image to display</param>
        /// <param name="key">text to display</param>
        /// <param name="endline">if true write a message to indicate record ended</param>
        /// <returns></returns>
        public async Task UpdateImg(TimeSpan timespan, string uriImage, string key, bool endline)
        {
            try
            {
                await Task.Delay(timespan);
                _bindings.img_source = new Uri(uriImage, UriKind.Absolute);
                AddTrace(key);
            }
            catch (TaskCanceledException)
            {
                _log.Info("canceled");
                throw new TaskCanceledException("canceled");
            }
            catch (Exception ex)
            {
                _log.Error("error display delayed img", ex);
                throw new InvalidOperationException("error display delayed img");
            }

            if (endline)
            {
                AddTrace("...Record Ends");
            }
        }
    }
}