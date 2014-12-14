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
    public class Slave
    {
        private IPEndPoint _endpoint;
        private TcpClient _client;

        public Slave(string hostname, int port)
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
            _endpoint = new IPEndPoint(ipAddress, port);

            // Create a TCP/IP socket.
            Renew();
        }

        public void Renew()
        {
            _client = new TcpClient(AddressFamily.InterNetworkV6);
            _client.Client.DualMode = true;
            _client.Client.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.IPv6Only, false);
        }

        public string ipAddress { get { return _endpoint.Address.ToString(); } }

        public string name { get; set; }

        public IPEndPoint endpoint { get { return _endpoint; } }

        public TcpClient client { get { return _client; } }

        public string status
        {
            get
            {
                return _client.Connected ? "Connected" : "Not connected";
            }
        }
    }
}
