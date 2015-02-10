using System;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace ShivaQEslave
{
    /// <summary>
    /// TCP and UDP are handled here for slaves
    /// to start tcp listening call StartListening
    /// TCPdataReceived will fire every time udp data is received
    /// end stream with StopListening method
    /// 
    /// to start udp listening call StartBroadcastListening
    /// UDPdataReceived will fire every time udp data is received
    /// </summary>
    public class AsynchronousSlave
    {
        private static readonly log4net.ILog _log = log4net.LogManager.GetLogger
        (System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public delegate void dataReceivedEventHandler(string data, NetworkStream networkStream);
        public static event dataReceivedEventHandler TCPdataReceived;

        public delegate void udpDataReceivedEventHandler(string data);
        public static event udpDataReceivedEventHandler UDPdataReceived;

        public delegate void acceptedEventHandler(NetworkStream networkStream);
        public static event acceptedEventHandler TCPaccepted;

        private static readonly string eof_tag = "<EOF>";

        private static TcpListener _tcpListener;
        private static UdpClient _udpListener = new UdpClient();

        private static CancellationTokenSource tcpCancellationToken;

        /// <summary>
        /// Start TCP listener
        /// </summary>
        /// <param name="port">The port that will be listened</param>
        public static void StartListening(int port)
        {
            _tcpListener = TcpListener.Create(port); //(IPAddress.Any, port);
            _tcpListener.Server.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.IPv6Only, false);
            _tcpListener.Start();

            TCPHandler();
        }

        /// <summary>
        /// Cancel tcp stream
        /// </summary>
        public static void StopListening()
        {
            tcpCancellationToken.Cancel();
        }

        /// <summary>
        /// mecanic of how tcp is handled:
        /// wait for accept connection & fire event if there is
        /// register a cancellation token in order to close the stream when it is hanging for transmitted data
        /// be sure all data are received, if not the case, wait for more
        /// format transmitted data, sometimes 2 transmissions can be concatenated to one.
        /// fire TCPdataReceived when data has been formated
        /// </summary>
        /// <param name="sb"></param>
        /// <param name="infinite"></param>
        /// <param name="networkStream"></param>
        private static async void TCPHandler()
        {
            StringBuilder sb = new StringBuilder();
            bool infinite = true;
            NetworkStream networkStream = null;

            if (infinite)
            {
                var tcpClient = await _tcpListener.AcceptTcpClientAsync(); // wait for connection before executing the rest of the code
                _log.Info("[Slave] Master has connected");

                networkStream = tcpClient.GetStream();

                //register a cancellation token, it will stop readAsync method from hanging and will wait for accepttcpclient again 
                tcpCancellationToken = new CancellationTokenSource();
                tcpCancellationToken.Token.Register(() =>
                {
                    infinite = false;
                    TCPHandler();
                });

                //fire tcpaccept event when connection has been accepted
                if (TCPaccepted != null)
                {
                    TCPaccepted(networkStream);
                }
            }
            //keep waiting for data untill cancellation token is fired
            while (infinite)
            {
                var buffer = new byte[4096];

                //wait for data transmitted
                int byteCount = 0;
                try
                {
                    byteCount = await networkStream.ReadAsync(buffer, 0, buffer.Length, tcpCancellationToken.Token);
                }
                catch (Exception e)
                {
                    _log.Warn("Error reading", e);
                    infinite = false;

                    //wait for next connection
                    TCPHandler();
                }

                //format data, wait for more if message is incomplete (no eof_tag)
                //fire TCPdataReceived callback when data is ready
                TCPDataReceivedHandler(byteCount, buffer, sb, networkStream);
            }
        }

        /// <summary>
        /// format tcp received data, wait for more if message is incomplete (no eof_tag)
        /// fire TCPdataReceived callback when data is ready
        /// </summary>
        /// <param name="byteCount"></param>
        /// <param name="buffer"></param>
        /// <param name="sb"></param>
        /// <param name="networkStream"></param>
        private static void TCPDataReceivedHandler(int byteCount, byte[] buffer, StringBuilder sb, NetworkStream networkStream)
        {
            if (byteCount > 0)
            {
                // There  might be more data, so store the data received so far.
                sb.Append(Encoding.UTF8.GetString(buffer, 0, byteCount));

                // Check for end-of-file tag. If it is not there, read 
                // more data.
                string content = sb.ToString();
                if (content.IndexOf(eof_tag) > -1)
                {
                    while (content.IndexOf(eof_tag) > -1) //for double click for instance, packets seems to be concatenated
                    {
                        string data = content.Substring(0, content.IndexOf(eof_tag));

                        _log.Info(string.Format("Received tcp says: {0}", data));

                        //callback
                        if (TCPdataReceived != null)
                        {
                            TCPdataReceived(data, networkStream);
                        }
                        content = sb.Remove(0, data.Length + eof_tag.Length).ToString();
                    }
                }
                //remaining data will be get by the while(true) loop
            }
        }

        /// <summary>
        /// Start UDP listener
        /// </summary>
        /// <param name="port">The port that will be listened</param>
        public static void StartBroadcastListening(int port)
        {
            _udpListener = new UdpClient(AddressFamily.InterNetworkV6);
            // udpListener.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            _udpListener.Client.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.IPv6Only, false);
            _udpListener.Client.Bind(new IPEndPoint(IPAddress.IPv6Any, port));

            UDPreceiveHandler();
        }

        private static void UDPreceiveHandler()
        {
            UDPreceiveHandler(new StringBuilder());
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sb">partial received data</param>
        /// <param name="infinite">if true wait for more</param>
        private static async void UDPreceiveHandler(StringBuilder sb)
        {
            while (true)
            {
                // log.Infp("Waiting for broadcast");
                UdpReceiveResult result = await _udpListener.ReceiveAsync();
                int byteCount = result.Buffer.Length;

                if (byteCount > 0)
                {
                    // There  might be more data, so store the data received so far.
                    sb.Append(Encoding.UTF8.GetString(result.Buffer, 0, byteCount));

                    // Check for end-of-file tag. If it is not there, read 
                    // more data.
                    string content = sb.ToString();
                    if (content.IndexOf(eof_tag) > -1)
                    {
                        string data = content.Substring(0, content.Length - eof_tag.Length);

                        //log.Info(string.Format("Received broadcast says: {0}", data)); -- too many to be interesting to log
                        sb = new StringBuilder(); //seems to correct a bug? where 2 sent strings with <EOF> where concatenated

                        //callback
                        if (UDPdataReceived != null)
                        {
                            UDPdataReceived(data);
                        }
                    }
                    //remaining data will be get by the while(true) loop
                }
            }
        }
    }
}
