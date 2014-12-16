using Newtonsoft.Json;
using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;

namespace ShivaQEmaster
{
    /// <summary>
    /// Description of Slaves elements.
    /// A slave is a computer that follow master's instructions (execute clicks / keystrokes)
    /// This class instanciate TCP client
    /// </summary>
    [JsonObject(MemberSerialization.OptIn)]
    public class Slave
    {
        private IPEndPoint _endpoint;
        private TcpClient _client;
        private int _port = 0;

        [JsonConstructor]
        public Slave()
        {

        }

        public Slave(string hostname, int port)
        {
            IPAddress ipAddress = null;
            if (hostname.Count(x => x == '.') == 3) //if it's an ip
            {
                try
                {
                    ipAddress = IPAddress.Parse(hostname);
                }
                catch (Exception ex) //may be just a subdomain.domain.com
                {
                    ipAddress = getAddress(hostname);
                }
            }
            else // else it's a host
            {
                ipAddress = getAddress(hostname);
            }

            if (ipAddress == null)
            {
                throw new Exception("Could not find ip address");
            }

            _endpoint = new IPEndPoint(ipAddress, port);
            _port = port;

            // Create a TCP/IP socket.
            Renew();
        }

        private IPAddress getAddress(string hostname)
        {
            IPHostEntry ipHostInfo = Dns.GetHostEntry(hostname);
            foreach (var addr in ipHostInfo.AddressList) //look for first valid ip
            {
                if (addr.AddressFamily == AddressFamily.InterNetwork || addr.AddressFamily == AddressFamily.InterNetworkV6)
                {
                    return addr;
                }
            }
            return null;
        }

        public void Renew()
        {
            _client = new TcpClient(AddressFamily.InterNetworkV6);
            _client.Client.DualMode = true;
            _client.Client.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.IPv6Only, false);
        }

        [JsonProperty]
        public string ipAddress
        {
            get { return _endpoint.Address.ToString(); }
            set
            {
                IPAddress address;
                try
                {
                    address = IPAddress.Parse(value);
                }
                catch (Exception ex)
                {
                    address = getAddress(value);
                }
                _endpoint = new IPEndPoint(address, _port);
            }
        }

        [JsonProperty]
        public string name { get; set; }

        [JsonProperty]
        public int port { get { return _port; } set { _port = value; } }

        [JsonIgnore]
        public IPEndPoint endpoint { get { return _endpoint; } }

        [JsonIgnore]
        public TcpClient client { get { return _client; } }

        [JsonIgnore]
        public string status
        {
            get
            {
                return _client.Connected ? "Connected" : "Not connected";
            }
        }
        
        [JsonIgnore]
        public bool IsSelected { get; set; }
    }
}
