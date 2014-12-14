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
    public class Slave
    {
        private IPAddress _ipAddress;
        private string _hostname;

        public Slave(string hostname)
        {
            IPAddress ipAddress = null;
            if (hostname.Count(x => x == '.') == 3) //if it's an ip
            {
                ipAddress = IPAddress.Parse(hostname);
            }
            else // else it's a host
            {
                IPHostEntry ipHostInfo = Dns.GetHostEntry(hostname);
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

        public string ipAddress { get { return _ipAddress.ToString(); } }

        public string friendlyName { get; set; }

        public string hostname { get { return _hostname; } }

        public string login { get; set; }

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
