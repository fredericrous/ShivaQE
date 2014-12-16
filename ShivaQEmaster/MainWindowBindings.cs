using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using ShivaQEcommon;
using System.Windows;
using System.Linq;
using System.Collections.Generic;

namespace ShivaQEmaster
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

        public IEnumerable<Slave> selectedSlaves { get { return slaves.Where(x => x.IsSelected); } }

        public class Scenarios
        {
            public string Name { get; set; }
            public string Time { get; set; }
        }
        private ObservableCollection<Scenarios> _scenarios = new ObservableCollection<Scenarios>();
        public ObservableCollection<Scenarios> scenarios
        {
            get { return _scenarios; }
            set
            {
                if (value != _scenarios)
                {
                    _scenarios = value;
                    NotifyPropertyChanged("scenarios");
                }
            }
        }

        private string _newSlaveIP = string.Empty;
        public string newSlaveIP
        {
            get { return _newSlaveIP; }
            set
            {
                if (value != _newSlaveIP)
                {
                    _newSlaveIP = value;
                    NotifyPropertyChanged("newSlaveIP");
                }
            }
        }

        private string _newSlaveName = string.Empty;
        public string newSlaveName
        {
            get { return _newSlaveName; }
            set
            {
                if (value != _newSlaveName)
                {
                    _newSlaveName = value;
                    NotifyPropertyChanged("newSlaveName");
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
                }
            }
        }
    }
}
