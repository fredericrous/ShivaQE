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
