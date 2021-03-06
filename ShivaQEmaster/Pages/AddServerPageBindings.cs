﻿using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using ShivaQEcommon;
using System.Windows;
using System.Linq;
using System.Collections.Generic;

namespace ShivaQEmaster
{
    public class AddServerPageBindings : BindingsBase
    {
        public AddServerPageBindings()
        {

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
