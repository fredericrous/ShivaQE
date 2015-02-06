using ShivaQEcommon;
using System;

namespace ShivaQEmaster
{
    public class SettingsSyncPageBindings : BindingsBase
    {
        public SettingsSyncPageBindings()
        {

        }

        private string _path = string.Empty;
        public string path
        {
            get { return _path; }
            set
            {
                if (value != _path)
                {
                    _path = value;
                    NotifyPropertyChanged("path");
                }
            }
        }

        private string _login = string.Empty;
        public string login
        {
            get { return _login; }
            set
            {
                if (value != _login)
                {
                    _login = value;
                    NotifyPropertyChanged("login");
                }
            }
        }

        private string _error_msg = string.Empty;
        public string error_msg
        {
            get { return _error_msg; }
            set
            {
                if (value != _error_msg)
                {
                    _error_msg = value;
                    NotifyPropertyChanged("error_msg");
                }
            }
        }

    }
}
