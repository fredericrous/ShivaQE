using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ShivaQEcommon;
using System.Windows;

namespace ShivaQEviewer
{
    public class MainWindowBindings : BindingsBase
    {
        public MainWindowBindings()
        {

        }

        private bool _flyout_log = false;
        public bool flyout_log
        {
            get { return _flyout_log; }
            set
            {
                if (value != _flyout_log)
                {
                    _flyout_log = value;
                    NotifyPropertyChanged("flyout_log");
                }
            }
        }

        private string _status = string.Empty;
        public string status
        {
            get { return _status; }
            set
            {
                //if (value != _status)
                //{
                    _status = value;
                    NotifyPropertyChanged("status");
                //}
            }
        }
    }
}
