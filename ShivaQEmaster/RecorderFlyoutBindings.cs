using System;
using System.Timers;

namespace ShivaQEmaster
{
    public class RecorderFlyoutBindings : BindingsBase
    {
        Timer timer_error_msg;

		public RecorderFlyoutBindings()
        {
            timer_error_msg = new Timer(8000);
            timer_error_msg.AutoReset = false;
            timer_error_msg.Elapsed += (s, e) =>
            {
                error_msg = string.Empty;
            };
        }
		
        private bool _checked_record = false;
        public bool checked_record
        {
            get { return _checked_record; }
            set
            {
                if (value != _checked_record)
                {
                    _checked_record = value;
                    NotifyPropertyChanged("checked_record");
                    _checked_record_txt = value ? Languages.language_en_US.flyout_ts_record_on : Languages.language_en_US.flyout_ts_record_off;
                    NotifyPropertyChanged("checked_record_txt");
                }
            }
        }

        private string _checked_record_txt = Languages.language_en_US.flyout_ts_record_off;
        public string checked_record_txt
        {
            get { return _checked_record_txt; }
        }


        private TimeSpan _time_elapsed;
        public TimeSpan time_elapsed
        {
            get { return _time_elapsed; }
            set
            {
                if (value != _time_elapsed)
                {
                    _time_elapsed = value;
                    NotifyPropertyChanged("time_elapsed");
                }
            }
        }

        private string _error_msg = string.Empty;
        public string error_msg
        {
            get { timer_error_msg.Start(); return _error_msg; }
            set
            {
                //if (value != _error_msg)
                //{
                if (timer_error_msg.Enabled)
                {
                    timer_error_msg.Stop();
                }
                _error_msg = value;
                NotifyPropertyChanged("error_msg");
                //}
            }
        }
    }
}
