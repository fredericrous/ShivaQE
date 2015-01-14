
namespace ShivaQEmaster
{
    public class SettingsPageBindings : BindingsBase
    {
        public SettingsPageBindings()
        {

        }

        private string _key_sendmousekey_on = SettingsManager.ReadSetting("key_sendmousekey_on").Trim();
        public string key_sendmousekey_on
        {
            get { return _key_sendmousekey_on; }
            set
            {
                if (value != _key_sendmousekey_on)
                {
                    SettingsManager.AddUpdateAppSettings("key_sendmousekey_on", value);
                    _key_sendmousekey_on = value;
                    NotifyPropertyChanged("key_sendmousekey_on");
                }
            }
        }

        private string _key_sendmousekey_off = SettingsManager.ReadSetting("key_sendmousekey_off").Trim();
        public string key_sendmousekey_off
        {
            get { return _key_sendmousekey_off; }
            set
            {
                if (value != _key_sendmousekey_off)
                {
                    SettingsManager.AddUpdateAppSettings("key_sendmousekey_off", value);
                    _key_sendmousekey_off = value;
                    NotifyPropertyChanged("key_sendmousekey_off");
                }
            }
        }

        private string _key_window_hide_toggle = SettingsManager.ReadSetting("key_window_hide_toggle").Trim();
        public string key_window_hide_toggle
        {
            get { return _key_window_hide_toggle; }
            set
            {
                if (value != _key_window_hide_toggle)
                {
                    SettingsManager.AddUpdateAppSettings("key_window_hide_toggle", value);
                    _key_window_hide_toggle = value;
                    NotifyPropertyChanged("key_window_hide_toggle");
                }
            }
        }

        private bool _trace_status = SettingsManager.ReadSetting("trace_status").Trim().ToLower() == "true";
        public bool trace_status
        {
            get { return _trace_status; }
            set
            {
                if (value != _trace_status)
                {
                    SettingsManager.AddUpdateAppSettings("trace_status", value.ToString());
                    _trace_status = value;
                    NotifyPropertyChanged("trace_status");
                }
            }
        }

        private bool _anonymous_tracking = SettingsManager.ReadSetting("analytics_status").Trim().ToLower() == "true";
        public bool anonymous_tracking
        {
            get { return _anonymous_tracking; }
            set
            {
                if (value != _anonymous_tracking)
                {
                    SettingsManager.AddUpdateAppSettings("analytics_status", value.ToString());
                    _anonymous_tracking = value;
                    NotifyPropertyChanged("analytics_status");
                }
            }
        }
    }
}
