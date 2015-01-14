using System;

namespace ShivaQEmaster
{
    public class RecorderFlyoutBindings : BindingsBase
    {
		public RecorderFlyoutBindings()
        {

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
    }
}
