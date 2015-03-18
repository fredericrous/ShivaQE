using System;

namespace ShivaQEcommon.Eventdata
{
    /// <summary>
    /// information on slaves environment
    /// </summary>
    public class ServerInfo : EventArgs
    {
        public string platform { get; set; }
        public string version { get; set; }
        public string lang { get; set; }
        public string token { get; set; }
    }
}
