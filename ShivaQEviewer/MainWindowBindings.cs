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

        private Visibility _add_window = Visibility.Hidden;
        public Visibility add_window
        {
            get { return _add_window; }
            set
            {
                if (value != _add_window)
                {
                    _add_window = value;
                    NotifyPropertyChanged("add_window");
                }
            }
        }

        private Visibility _list_window = Visibility.Visible;
        public Visibility list_window
        {
            get { return _list_window; }
            set
            {
                if (value != _list_window)
                {
                    _list_window = value;
                    NotifyPropertyChanged("list_window");
                }
            }
        }

        private Visibility _resolution_window = Visibility.Hidden;
        public Visibility resolution_window
        {
            get { return _resolution_window; }
            set
            {
                if (value != _resolution_window)
                {
                    _resolution_window = value;
                    NotifyPropertyChanged("resolution_window");
                }
            }
        }

        private Visibility _done_window = Visibility.Hidden;
        public Visibility done_window
        {
            get { return _done_window; }
            set
            {
                if (value != _done_window)
                {
                    _done_window = value;
                    NotifyPropertyChanged("done_window");
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
