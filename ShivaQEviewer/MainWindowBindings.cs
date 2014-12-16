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
    public class MainWindowBindings : INotifyPropertyChanged
    {
        public MainWindowBindings()
        {

        }

        public event PropertyChangedEventHandler PropertyChanged;
        public void NotifyPropertyChanged(String propertyName = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
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

        private Slave _slaveSelected;
        public Slave slaveSelected
        {
            get { return _slaveSelected; }
            set
            {
                if (value != _slaveSelected)
                {
                    _slaveSelected = value;
                    NotifyPropertyChanged("slaveSelected");
                }
            }
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

        private Visibility _window_add = Visibility.Collapsed;
        public Visibility window_add
        {
            get { return _window_add; }
            set
            {
                if (value != _window_add)
                {
                    _window_add = value;
                    NotifyPropertyChanged("window_add");
                }
            }
        }

        private Visibility _window_list = Visibility.Visible;
        public Visibility window_list
        {
            get { return _window_list; }
            set
            {
                if (value != _window_list)
                {
                    _window_list = value;
                    NotifyPropertyChanged("window_list");
                }
            }
        }

        private Visibility _window_resolution = Visibility.Collapsed;
        public Visibility window_resolution
        {
            get { return _window_resolution; }
            set
            {
                if (value != _window_resolution)
                {
                    _window_resolution = value;
                    NotifyPropertyChanged("window_resolution");
                }
            }
        }

        private Visibility _window_done = Visibility.Collapsed;
        public Visibility window_done
        {
            get { return _window_done; }
            set
            {
                if (value != _window_done)
                {
                    _window_done = value;
                    NotifyPropertyChanged("window_done");
                }
            }
        }

        private string _width = string.Empty;
        public string width
        {
            get { return _width; }
            set
            {
                if (value != _width)
                {
                    _width = value;
                    NotifyPropertyChanged("width");
                }
            }
        }

        private string _height = string.Empty;
        public string height
        {
            get { return _height; }
            set
            {
                if (value != _height)
                {
                    _height = value;
                    NotifyPropertyChanged("height");
                }
            }
        }

        private string _status = string.Empty;
        public string status
        {
            get { return _status; }
            set
            {
                if (value != _status)
                {
                    _status = value;
                    NotifyPropertyChanged("status");
                }
            }
        }
        
    }
}
