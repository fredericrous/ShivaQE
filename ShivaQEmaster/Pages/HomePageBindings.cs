using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using ShivaQEcommon;
using System.Windows;
using System.Linq;
using System.Collections.Generic;
using System.Timers;

namespace ShivaQEmaster
{
    public class HomePageBindings : BindingsBase
    {
        Timer t;

        public HomePageBindings()
        {
            t = new Timer(8000);
            t.AutoReset = false;
            t.Elapsed += (s, e) =>
            {
                error_msg = string.Empty;
            };
        }

        private ObservableCollection<Slave> _slaves = new ObservableCollection<Slave>();
        public ObservableCollection<Slave> slaves
        {
            get { return _slaves; }
            set
            {
                if (value != _slaves)
                {
                    _slaves = value;
                    NotifyPropertyChanged("slaves");
                }
            }
        }

        public IEnumerable<Slave> selectedSlaves { get { return slaves.Where(x => x.IsSelected); } }

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
            get { t.Start(); return _error_msg; }
            set
            {
                //if (value != _error_msg)
                //{
                if (t.Enabled)
                {
                    t.Stop();
                }
                _error_msg = value;
                NotifyPropertyChanged("error_msg");
                //}
            }
        }
    }
}
