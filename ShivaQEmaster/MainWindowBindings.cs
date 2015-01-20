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
                    _checked_broadcast_txt = value ? Languages.language_en_US.homepage_ts_broadcast_on : Languages.language_en_US.homepage_ts_broadcast_off;
                    NotifyPropertyChanged("checked_broadcast_txt");
                }
            }
        }

        private string _checked_broadcast_txt = Languages.language_en_US.homepage_ts_broadcast_on;
        public string checked_broadcast_txt
        {
            get { return _checked_broadcast_txt; }
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
