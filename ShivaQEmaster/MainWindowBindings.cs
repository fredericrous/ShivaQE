using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using ShivaQEcommon;

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

        private System.Windows.Visibility _newSlaveIP_label = System.Windows.Visibility.Visible;
        public System.Windows.Visibility newSlaveIP_label
        {
            get { return _newSlaveIP_label; }
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

                    _newSlaveIP_label = (_newSlaveIP == string.Empty) ? System.Windows.Visibility.Visible : System.Windows.Visibility.Hidden;
                    NotifyPropertyChanged("newSlaveIP_label");
                }
            }
        }

        private System.Windows.Visibility _newSlaveName_label = System.Windows.Visibility.Visible;
        public System.Windows.Visibility newSlaveName_label
        {
            get { return _newSlaveName_label; }
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

                    _newSlaveName_label = (_newSlaveName == string.Empty) ? System.Windows.Visibility.Visible : System.Windows.Visibility.Hidden;
                    NotifyPropertyChanged("newSlaveName_label");
                }
            }
        }

        private bool _broadcastMouseMovement = true;
        public bool broadcastMouseMovement
        {
            get { return _broadcastMouseMovement; }
            set
            {
                if (value != _broadcastMouseMovement)
                {
                    _broadcastMouseMovement = value;
                    NotifyPropertyChanged("broadcastMouseMovement");
                }
            }
        }
        
    }
}
