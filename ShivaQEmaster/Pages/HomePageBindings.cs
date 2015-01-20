using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using ShivaQEcommon;
using System.Windows;
using System.Linq;
using System.Collections.Generic;

namespace ShivaQEmaster
{
    public class HomePageBindings : BindingsBase
    {
        public HomePageBindings()
        {

        }

        private bool _checked_broadcast = true;
        public bool checked_broadcast
        {
            get { return _checked_broadcast; }
            set
            {
                if (value != _checked_broadcast)
                {
                    _checked_broadcast = value;
                    NotifyPropertyChanged("checked_broadcast");
                    _checked_broadcast_txt = value ? Languages.language_en_US.homepage_ts_broadcast_on : Languages.language_en_US.homepage_ts_broadcast_off;
                    NotifyPropertyChanged("checked_broadcast_txt");
                }
            }
        }

        private string _checked_broadcast_txt = Languages.language_en_US.homepage_ts_broadcast_on;
        public string checked_broadcast_txt
        {
            get { return _checked_broadcast_txt; }
        }


        private string _error_msg = string.Empty;
        public string error_msg
        {
            get { return _error_msg; }
            set
            {
                //if (value != _error_msg)
                //{
                _error_msg = value;
                NotifyPropertyChanged("error_msg");
                //}
            }
        }
    }
}
