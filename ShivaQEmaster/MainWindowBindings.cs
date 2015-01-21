using System.Collections.ObjectModel;
using System.Linq;
using System.Collections.Generic;

namespace ShivaQEmaster
{
    public class MainWindowBindings : BindingsBase
    {
        public MainWindowBindings()
        {

        }
		
		private bool _flyout_record = false;
        public bool flyout_record
        {
            get { return _flyout_record; }
            set
            {
                if (value != _flyout_record)
                {
                    _flyout_record = value;
                    NotifyPropertyChanged("flyout_record");
                }
            }
        }

    }
}
