using System;

namespace ShivaQEcommon.Eventdata
{
    /// <summary>
    /// Element describing a mouse or keyoard events
    /// </summary>
    public class MouseNKeyEventArgs : EventArgs
    {
        public string key { get; set; }
        public int keyCode { get; set; }
        public string keyData { get; set; }
        public int position_x { get; set; }
        public int position_y { get; set; }
        public long timestamp { get; set; }
    }
}
