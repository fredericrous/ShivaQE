using System;
using System.Net.Sockets;
using System.Text;
using Newtonsoft.Json;
using System.Collections.ObjectModel;
using System.Windows;
using System.Runtime.InteropServices;
using System.Linq;
using System.Threading.Tasks;
using ShivaQEcommon.Eventdata;
using System.IO;
using System.Net;
using System.Collections.Generic;
using ShivaQEcommon;
using log4net;
using System.Reflection;
using System.Threading;

namespace ShivaQEmaster
{
    //add
    //remove
    //connect
    //reconnectAll
    //disconnectAll
    //send
    //activateBroadcast
    //deactivateBroadcast
    //broadcast
    //getSlaves
    /// <summary>
    /// Handle slave connections
    /// there's to way of transmitting data here.
    /// Broadcast which uses UDP, it's used to transfer to slaves the mouse position in real time
    /// Send uses TCP, it's a safer way to transmit keystrockes and clicks but also actions
    /// in ShivaQE, actions are like web services
    /// </summary>
    public class SlaveManager
    {
        private int port;

        private static readonly ILog _log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        const int KL_NAMELENGTH = 9;

        [DllImport("user32.dll")]
        public static extern long GetKeyboardLayoutName(System.Text.StringBuilder pwszKLID);

        private string Lang
        {
            get
            {
                System.Text.StringBuilder name = new System.Text.StringBuilder(KL_NAMELENGTH);
                GetKeyboardLayoutName(name);
                return name.ToString();
            }
        }

        // The response from the remote device.
        private String response = String.Empty;

        //list of slaves computer that this master will send information to
        public ObservableCollection<Slave> slaveList;

        private UdpClient _broadcastChannel;
        private static readonly string _slaveList_save_path = "slavelist.json";

        public string SlaveListPath { get { return _slaveList_save_path; } }

        public delegate void DisconnectedEventHandler();
        public event DisconnectedEventHandler Disconnected;

        public delegate Task IncomingEventHandler(string data, Slave slave, NetworkStream networkStream);
        public event IncomingEventHandler Incoming;


        private static readonly SlaveManager _instance = new SlaveManager();

        private SlaveManager() { }

        public static SlaveManager Instance
        {
            get
            {
                return _instance; 
            }
        }

        public void Init()
        {
            slaveList = new ObservableCollection<Slave>();
            _broadcastChannel = new UdpClient(AddressFamily.InterNetworkV6);
            _broadcastChannel.Client.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.IPv6Only, false);
            //broadcastChannel.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);

            if (File.Exists(_slaveList_save_path))
            {
                try
                {
                    string slaveListJson = File.ReadAllText(_slaveList_save_path);
                    ObservableCollection<Slave> slaveListFromJson = JsonConvert.DeserializeObject<ObservableCollection<Slave>>(slaveListJson);
                    foreach (var item in slaveListFromJson)
                    {
                        slaveList.Add(new Slave(item.ipAddress, item.port) { friendlyName = item.friendlyName });
                    }
                    slaveListFromJson = null;
                }
                catch (Exception ex)
                {
                    _log.Error(string.Format("load json {0}", _slaveList_save_path), ex);
                }
            }
        }

        /// <summary>
        /// Send data as JSON through UDP to each server within slaveList
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="data"></param>
        public void Broadcast<T>(T data)
        {
            string json = JsonConvert.SerializeObject(data);

            foreach (var slave in slaveList)
            {
                if (slave.client.Connected) //broadcast only if slave is also connected with tcp
                {
                    json = string.Format("{0}:{1}<EOF>", slave.token, json);
                    byte[] byteData = Encoding.UTF8.GetBytes(json);
                    _broadcastChannel.SendAsync(byteData, byteData.Length, slave.endpoint);
                }
            }
        }

        List<string> readingList = new List<string>();

        public void Add(Slave slave)
        {
            //add slave to list
            slaveList.Add(slave);

            //save list to file to be reloaded at next startup
            string slaveListJson = JsonConvert.SerializeObject(slaveList, Formatting.Indented);
            File.WriteAllText(_slaveList_save_path, slaveListJson);
        }

        /// <summary>
        /// Connect to a server and add it to slaveList
        /// </summary>
        /// <param name="hostname"></param>
        /// <param name="friendlyname"></param>
        public async Task<bool> Add(string hostname, int port, string friendlyname)
        {
            this.port = port;
            bool ret = true;

            // Connect to a remote device.
            try
            {
                Slave slave = new Slave(hostname, port);
                slave.friendlyName = friendlyname;

                // Connect to the remote endpoint.
                _log.Info("[Master] Connecting to slave");
                //IPAddress address = IPAddress.Parse(slave.ipAddress + IPAddress.Loopback.ToString());
                await slave.client.ConnectAsync(slave.ipAddress, port);
                _log.Info("[Master] Connected to slave");

                var networkStream = slave.client.GetStream();

                //read server's response
                var buffer = new byte[850]; //server should send which patform and which version it's on. buffer should be enough
                var byteCount = await networkStream.ReadAsync(buffer, 0, buffer.Length);
                var response = Encoding.UTF8.GetString(buffer, 0, byteCount);
                _log.Info(string.Format("[Master] Slave response was {0}", response));

                ServerInfo serverInfo = JsonConvert.DeserializeObject<ServerInfo>(response.Remove(response.LastIndexOf("<EOF>")));

                //set token got from slave
                slave.token = serverInfo.token;

                //add slave to list
                slaveList.Add(slave);

                //save list to file to be reloaded at next startup
                string slaveListJson = JsonConvert.SerializeObject(slaveList, Formatting.Indented);
                File.WriteAllText(_slaveList_save_path, slaveListJson);

                //set language on slave

                if (serverInfo.lang != Lang)
                {
                    ActionMethod action = new ActionMethod()
                    {
                        method = ActionType.SetLang,
                        value = Lang
                    };
                    string json = JsonConvert.SerializeObject(action);
                    json = string.Format("{0}:{1}<EOF>", serverInfo.token, json);
                    byte[] byteData = Encoding.UTF8.GetBytes(json);
                    _log.Info(string.Format("[Master] Writing request {0}", byteData));
                    try
                    {
                        await networkStream.WriteAsync(byteData, 0, byteData.Length);
                    }
                    catch (Exception ex)
                    {
                        _log.Error("Error writting request", ex);
                        throw;
                    }
                }

                //async func
                Task task = ReadSlaveIncoming(slave);
                readingList.Add(slave.hostname);

            }
            catch (SocketException ex)
            {
                _log.Warn("error adding slave", ex);
                ret = false;
                throw;
            }

            return ret;
        }

        private static readonly string eof_tag = "<EOF>";

        //public async Task<string> Send<T>(T data, string hostname)
        //{
        //    string receivedData = null;
        //    byte[] byteData;
        //    string json = null;
        //    StringBuilder sb = new StringBuilder();

        //    json = JsonConvert.SerializeObject(data);
        //    json += "<EOF>"; //used serverside to know string has been received entirely

        //    byteData = Encoding.UTF8.GetBytes(json);

        //    NetworkStream networkStream = this.slaveList.Where(x => x.hostname == hostname).First().client.GetStream();
        //    await networkStream.WriteAsync(byteData, 0, byteData.Length);

        //    ActionType? expectedAction = null;
        //    if (typeof(T) == typeof(ActionType))
        //    {
        //        expectedAction = data as ActionType?;
        //    }

        //    //keep waiting for data untill cancellation token is fired
        //    while (true)
        //    {
        //        var buffer = new byte[4096];

        //        //wait for data transmitted
        //        int byteCount = 0;
        //        try
        //        {
        //            byteCount = await networkStream.ReadAsync(buffer, 0, buffer.Length);
        //        }
        //        catch (Exception e)
        //        {
        //            _log.Warn("Error reading answer", e);
        //            break;
        //        }

        //        //read result
        //        if (byteCount > 0)
        //        {
        //            sb.Append(Encoding.UTF8.GetString(buffer, 0, byteCount));

        //            string content = sb.ToString();
        //            if (content.IndexOf(eof_tag) > -1)
        //            {
        //                if (expectedAction != null)
        //                {
        //                    int actionNumber = 0;
        //                    Int32.TryParse(content.Substring(content.IndexOf("action:\""), 2), out actionNumber);
        //                    if ((ActionType)actionNumber == expectedAction)
        //                    {
        //                        receivedData = content.Substring(0, content.IndexOf(eof_tag));
        //                        break;
        //                    }
        //                }
        //                else
        //                {
        //                    receivedData = content.Substring(0, content.IndexOf(eof_tag));
        //                    break;
        //                }
        //            }
        //        }
        //    }
        //    return receivedData;
        //}

        private async Task ReadSlaveIncoming(Slave slave)
        {
            if (slave != null && slave.status == "Connected")
            {
                try
                {
                    var networkStream = slave.client.GetStream();

                    var buffer = new byte[4096];
                    var byteCount = await networkStream.ReadAsync(buffer, 0, buffer.Length);
                    var response = Encoding.UTF8.GetString(buffer, 0, byteCount);

                    _log.Info(string.Format("[Master] Slave response was {0}", response));

                    string content = response.ToString();
                    if (!(content.IndexOf(eof_tag) > -1))
                    {
                        _log.Info("missing eof tag from received data");
                    }
                    else
                    {
                        while (content.IndexOf(eof_tag) > -1) //for double click for instance, packets seems to be concatenated
                        {
                            string data = content.Substring(0, content.IndexOf(eof_tag));

                            _log.Info(string.Format("Received tcp says: {0}", data));

                            //action
                            await Incoming(data, slave, networkStream);
                            content = response.Remove(0, data.Length + eof_tag.Length).ToString();
                        }
                    }
                }
                catch (ObjectDisposedException odex)
                {
                    _log.Warn("When slave is disconnected, it's normal that this function throw an exception", odex);
                }
                catch (Exception ex)
                {
                    _log.Warn(string.Format("error read slave {0}'s incoming data", slave.hostname), ex);
                    Disconnected();
                }
                //wait for next read
                await ReadSlaveIncoming(slave);
            }
            else
            {
                //removing slave from the list which await slave answers
                readingList.Remove(slave.hostname);
            }
        }

        /// <summary>
        /// Send data as JSON to slaves contained in slaveList
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="data"></param>
        public async Task<List<string>> Send<T>(T data)
        {
            List<string> errorList = new List<string>();
            byte[] byteData;
            string json = null;

            // send data to all remote devices
            foreach (Slave slave in slaveList)
            {
                if (slave.status == "Connected")
                {
                    json = JsonConvert.SerializeObject(data);
                    json = string.Format("{0}:{1}<EOF>", slave.token, json);

                    byteData = Encoding.UTF8.GetBytes(json);

                    var networkStream = slave.client.GetStream();
                    _log.Info(string.Format("[Master] Writing request {0}", json));
                    try
                    {
                        await networkStream.WriteAsync(byteData, 0, byteData.Length);
                    }
                    catch (SocketException sex)
                    {
                        _log.Info("Slave closed the connection!?", sex);
                        //if (sex.ErrorCode == 10054)
                        //{
                           Disconnect(slave);
                        //}
                    }
                    catch (Exception ex)
                    {
                        string error_msg = string.Format("Error writing request tcp to {0}", slave.ipAddress);
                        _log.Error(error_msg, ex);
                        //slaveList.Where(x => x.ipAddress == slave.ipAddress).First().client.Close();
                        errorList.Add(error_msg);
                        // throw;
                    }

                    //if (compareON && SettingsManager.ReadSetting("notification_status") == "true")
                    //{
                    //    try
                    //    {
                    //        await IsResultOK(networkStream, slave.hostname);
                    //    }
                    //    catch (Exception ex)
                    //    {
                    //        string error_msg = string.Format("Error writing request tcp to {0}", slave.ipAddress);
                    //        _log.Error(error_msg, ex);
                    //        //errorList.Add(error_msg);
                    //    }
                    //}
                    _log.Info("[Master] Written");
                }
            }
            return errorList;
        }

        public void Remove(IEnumerable<Slave> slaves)
        {
            List<Task> tasks = new List<Task>();
            foreach (var slave in slaves.ToList())
            {
                tasks.Add(Disconnect(slave));
                slaveList.Remove(slave);
            }
            try
            {
                Task.WaitAll(tasks.ToArray());
            }
            catch
            {
                _log.Error("Error while removing slave");
            }

            //save list to file to be reloaded at next startup
            string slaveListJson = JsonConvert.SerializeObject(slaveList);
            File.WriteAllText(_slaveList_save_path, slaveListJson);
        }


        /// <summary>
        /// Disconnect a slave and remove it from the list
        /// </summary>
        /// <param name="slave"></param>
        /// <returns></returns>
        public async Task Remove(Slave slave)
        {
            await Disconnect(slave);
            slaveList.Remove(slave);

            //save list to file to be reloaded at next startup
            string slaveListJson = JsonConvert.SerializeObject(slaveList);
            File.WriteAllText(_slaveList_save_path, slaveListJson);
        }

        //public async void reconnectAll()
        //{
        //    foreach (var slave in slaveList)
        //    {
        //        reconnect(slave);
        //    }
        //}

        public void DisconnectAll()
        {
            foreach (var slave in slaveList)
            {
                if (slave.client.Connected)
                {
                    slave.client.Close();
                }
            }
        }

        public async Task Reconnect(Slave slave)
        {
            if (!slave.client.Connected)
            {
                try
                {
                    slave.Renew();
                    await slave.client.ConnectAsync(IPAddress.Parse(slave.ipAddress), slave.port);
                    _log.Info("[Client] re-Connected to server");

                    bool isAlreadyReading = (from r in readingList where r == slave.hostname select r).Count() > 0;
                    if (!isAlreadyReading)
                    {
                        Task task = ReadSlaveIncoming(slave);
                        readingList.Add(slave.hostname);
                    }
                }
                catch (Exception ex)
                {
                    string error = "Exception while reconnecting (could be timeout)";
                    _log.Error(error, ex);
                    //"Can't reconnect {0}, maybe slave is not launched or has been terminated!?")
                    throw new InvalidOperationException(error);
                }
            }
            else
            {
                throw new InvalidOperationException(string.Format("{0} already connected", slave.friendlyName));
            }
        }

        internal async Task Disconnect(Slave slave)
        {
            ActionMethod data = new ActionMethod() { method = ActionType.Disconnect };
            string json = JsonConvert.SerializeObject(data);
            json = string.Format("{0}:{1}<EOF>", slave.token, json);
            byte[] byteData = Encoding.UTF8.GetBytes(json);

            try
            {
                var networkStream = slave.client.GetStream();
                await networkStream.WriteAsync(byteData, 0, byteData.Length);
                slave.client.Close();
            }
            catch (Exception ex)
            {
                string error = string.Format("Error requesting disconnect from {0}", slave.friendlyName);
                _log.Error(error, ex);
                //throw new InvalidOperationException(error);
            }
        }

    }
}
