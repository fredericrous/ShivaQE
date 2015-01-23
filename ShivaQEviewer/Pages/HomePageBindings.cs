using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using ShivaQEcommon;
using System.Windows;
using System.Linq;
using System.Collections.Generic;

namespace ShivaQEviewer
{
    public class HomePageBindings : BindingsBase
    {
        public HomePageBindings()
        {

        }

        private ObservableCollection<Slave> _slaves = new ObservableCollection<Slave>();
        public ObservableCollection<Slave> slaves
        {
            get { return _slaves; }
            set
            {
                //if (value != _slaves)
                //{
                _slaves = value;
                NotifyPropertyChanged("slaves");
                //}
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
