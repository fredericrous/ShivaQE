using System.Collections.ObjectModel;
using System.Linq;
using System.Collections.Generic;
using System.Windows.Media;
using System;

namespace ShivaQEmaster
{
    public class RecordViewerWindowBindings : BindingsBase
    {
        public RecordViewerWindowBindings()
        {

        }
		
		private Uri _img_source = null;
        public Uri img_source
        {
            get { return _img_source; }
            set
            {
                if (value != _img_source)
                {
                    _img_source = value;
                    NotifyPropertyChanged("img_source");
                }
            }
        }
    }
}
