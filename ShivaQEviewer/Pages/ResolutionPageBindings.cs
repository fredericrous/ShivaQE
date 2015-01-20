using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using ShivaQEcommon;
using System.Windows;
using System.Linq;
using System.Collections.Generic;

namespace ShivaQEviewer
{
    public class ResolutionPageBindings : BindingsBase
    {
        public ResolutionPageBindings()
        {

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
    }
}
