using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using ShivaQEcommon;
using System.Windows;
using System.Linq;
using System.Collections.Generic;

namespace ShivaQEviewer
{
    public class AddServerPageBindings : BindingsBase
    {
        public AddServerPageBindings()
        {

        }

        private string _add_hostname = string.Empty;
        public string add_hostname
        {
            get { return _add_hostname; }
            set
            {
                if (value != _add_hostname)
                {
                    _add_hostname = value;
                    NotifyPropertyChanged("add_hostname");
                }
            }
        }

        private string _add_friendlyname = string.Empty;
        public string add_friendlyname
        {
            get { return _add_friendlyname; }
            set
            {
                if (value != _add_friendlyname)
                {
                    _add_friendlyname = value;
                    NotifyPropertyChanged("add_friendlyname");
                }
            }
        }

        private string _add_login = string.Empty;
        public string add_login
        {
            get { return _add_login; }
            set
            {
                if (value != _add_login)
                {
                    _add_login = value;
                    NotifyPropertyChanged("add_login");
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

        private string _error_color = "Red";
        public string error_color
        {
            get { return _error_color; }
            set
            {
                if (value != _error_color)
                {
                    _error_color = value;
                    NotifyPropertyChanged("error_color");
                }
            }
        }
    }
}
