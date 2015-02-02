using Newtonsoft.Json;
using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;

namespace ShivaQEviewer
{
    /// <summary>
    /// Description of Slaves elements.
    /// A slave is a computer that follow master's instructions (execute clicks / keystrokes)
    /// This class looks like the one described in master but doesn't intenciate tcpclient
    /// </summary>
    [JsonObject(MemberSerialization.OptIn)]
    public class Slave
    {
        private IPAddress _ipAddress;
        private string _hostname;

        [JsonConstructor]
        public Slave()
        {

        }

        public Slave(string hostname)
        {
            IPAddress ipAddress = null;
            if (hostname.Count(x => x == '.') == 3) //if it's an ip
            {
                ipAddress = IPAddress.Parse(hostname);
            }
            else // else it's a host
            {
                IPHostEntry ipHostInfo;
                try
                {
                    ipHostInfo = Dns.GetHostEntry(hostname);
                }
                catch
                {
                    throw new Exception("Could not find host");
                }
                foreach (var addr in ipHostInfo.AddressList) //look for first valid ip
                {
                    if (addr.AddressFamily == AddressFamily.InterNetwork || addr.AddressFamily == AddressFamily.InterNetworkV6)
                    {
                        ipAddress = addr;
                        break;
                    }
                }
                if (ipAddress == null)
                {
                    throw new Exception("Could not find ip address");
                }
            }
            _hostname = hostname;
            _ipAddress = ipAddress;
        }

        [JsonProperty]
        public string ipAddress
        {
            get { return _ipAddress.ToString(); }
            set { _ipAddress = IPAddress.Parse(value); }
        }

        [JsonProperty]
        public int port { get; set; }

        [JsonProperty]
        public string friendlyName { get; set; }

        [JsonProperty]
        public string hostname
        {
            get { return _hostname; }
            set { _hostname = value; }
        }

        [JsonProperty]
        public string login { get; set; }

        [JsonProperty]
        public string password { get; set; }

        //public string status
        //{
        //    get
        //    {
        //        return _client.Connected ? "Connected" : "Not connected";
        //    }
        //}
    }
}
