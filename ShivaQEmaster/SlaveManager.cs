using System;
using System.Net.Sockets;
using System.Text;
using Newtonsoft.Json;
using System.Collections.ObjectModel;
using System.Windows;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Linq;
using System.Threading.Tasks;
using ShivaQEcommon.Eventdata;
using System.IO;
using System.Net;

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
    public class slaveManager {
        private int port;

        private static readonly log4net.ILog log = log4net.LogManager.GetLogger
        (System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        const int KL_NAMELENGTH = 9;
        const uint KLF_ACTIVATE = 1;

        [DllImport("user32.dll")]
        public static extern long LoadKeyboardLayout(string pwszKLID, uint Flags);
        [DllImport("user32.dll")]
        public static extern long GetKeyboardLayoutName(System.Text.StringBuilder pwszKLID);

        private string lang
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

        UdpClient broadcastChannel;
        private string slaveList_save_path = "slavelist.json";

        public slaveManager(ObservableCollection<Slave> slaves)
        {
            slaveList = slaves;
            broadcastChannel = new UdpClient(AddressFamily.InterNetworkV6);
            broadcastChannel.Client.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.IPv6Only, false);
            //broadcastChannel.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);

            if (File.Exists(slaveList_save_path))
            {
                try
                {
                    string slaveListJson = File.ReadAllText(slaveList_save_path);
                    ObservableCollection<Slave> slaveListFromJson = JsonConvert.DeserializeObject<ObservableCollection<Slave>>(slaveListJson);
                    foreach (var item in slaveListFromJson)
                    {
                        slaveList.Add(new Slave(item.ipAddress, item.port) { name = item.name });
                    }
                    slaveListFromJson = null;
                }
                catch (Exception ex)
                {
                    log.Error(string.Format("load json {0}", slaveList_save_path), ex);
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
            json += "<EOF>";

            foreach (var slave in slaveList)
            {
                if (slave.client.Connected) //broadcast only if slave is also connected with tcp
                {
                    byte[] byteData = Encoding.UTF8.GetBytes(json);
                    broadcastChannel.SendAsync(byteData, byteData.Length, slave.endpoint);
                }
            }
        }

        /// <summary>
        /// Connect to a server and add it to slaveList
        /// </summary>
        /// <param name="hostname"></param>
        /// <param name="friendlyname"></param>
        public async void add(string hostname, int port, string friendlyname)
        {
            this.port = port;
            // Connect to a remote device.
            try
            {
                Slave slave = new Slave(hostname, port);
                slave.name = friendlyname;

                // Connect to the remote endpoint.
                log.Info("[Master] Connecting to slave");
                //IPAddress address = IPAddress.Parse(slave.ipAddress + IPAddress.Loopback.ToString());
                await slave.client.ConnectAsync(slave.ipAddress, port);
                log.Info("[Master] Connected to slave");

                var networkStream = slave.client.GetStream();

                //read server's response
                var buffer = new byte[4096]; //server should send which patform and which version it's on. buffer of 4096 is largely enough
                var byteCount = await networkStream.ReadAsync(buffer, 0, buffer.Length);
                var response = Encoding.UTF8.GetString(buffer, 0, byteCount);
                log.Info(string.Format("[Master] Slave response was {0}", response));

                //add slave to list
                slaveList.Add(slave);

                //save list to file to be reloaded at next startup
                string slaveListJson = JsonConvert.SerializeObject(slaveList, Formatting.Indented);
                File.WriteAllText(slaveList_save_path, slaveListJson);

                //set language on slave
                ServerInfo serverInfo = JsonConvert.DeserializeObject<ServerInfo>(response.Remove(response.LastIndexOf("<EOF>")));
                if (serverInfo.lang != lang)
                {
                    ActionMethod action = new ActionMethod() { method = ActionType.SetLang, value = lang };
                    string json = JsonConvert.SerializeObject(action);
                    json += "<EOF>";
                    byte[] byteData = Encoding.UTF8.GetBytes(json);
                    log.Info(string.Format("[Master] Writing request {0}", byteData));
                    try
                    {
                        await networkStream.WriteAsync(byteData, 0, byteData.Length);
                    }
                    catch (Exception ex)
                    {
                        log.Error("Error writting request", ex);
                        throw new Exception("cant write"); //doesnt work
                    }

                }
            
            }
            catch (Exception ex)
            {
                MessageBox.Show("Can't add host");
                log.Warn("error adding slave", ex);
            }
        }

        /// <summary>
        /// Send data as JSON to slaves contained in slaveList
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="data"></param>
        public async Task Send<T>(T data)
        {
            string json = JsonConvert.SerializeObject(data);
            json += "<EOF>"; //used serverside to know string has been received entirely

            byte[] byteData = Encoding.UTF8.GetBytes(json);

            // send data to all remote devices
            foreach (Slave slave in slaveList)
            {
                if (slave.status == "Connected")
                {
                    var networkStream = slave.client.GetStream();
                    log.Info(string.Format("[Master] Writing request {0}", byteData));
                    try
                    {
                        await networkStream.WriteAsync(byteData, 0, byteData.Length);
                    }
                    catch (Exception ex)
                    {
                        log.Error("Error writing request tcp", ex);
                        slaveList.Where(x => x.ipAddress == slave.ipAddress).First().client.Close();
                        throw;
                    }
                    log.Info("[Master] Written");
                }
            }
            return ;
        }

        public async void remove(string hostname)
        {
            ActionMethod data = new ActionMethod() { method = ActionType.Disconnect };
            string json = JsonConvert.SerializeObject(data);
            json += "<EOF>"; //used serverside to know string has been received entirely
            byte[] byteData = Encoding.UTF8.GetBytes(json);

            foreach (var slave in slaveList)
            {
                if (slave.ipAddress == hostname)
                {
                    var networkStream = slave.client.GetStream();
                    try
                    {
                        await networkStream.WriteAsync(byteData, 0, byteData.Length);
                    }
                    catch (Exception ex)
                    {
                        log.Error(string.Format("Error requesting disconnect from {0}", slave.name), ex);
                        //throw;
                    }
                    slave.client.Close();
                    slaveList.Remove(slave);
                    string slaveListJson = JsonConvert.SerializeObject(slaveList);
                    File.WriteAllText("serverlist.json", slaveListJson);
                    break;
                }
            }
        }

        //public async void reconnectAll()
        //{
        //    foreach (var slave in slaveList)
        //    {
        //        reconnect(slave);
        //    }
        //}

        public void disconnectAll()
        {
            foreach (var slave in slaveList)
            {
                if (slave.client.Connected)
                {
                    slave.client.Close();
                }
            }
        }

        public async void reconnect(Slave slave)
        {
            if (!slave.client.Connected)
            {
                try
                {
                    slave.Renew();
                    await slave.client.ConnectAsync(IPAddress.Parse(slave.ipAddress), slave.port);
                }
                catch (ObjectDisposedException odex)
                {
                    log.Warn("Can't reconnect", odex);
                    MessageBox.Show("Can't reconnect, maybe slave is not launched or has been terminated!?");
                }
                catch (Exception ex)
                {
                    log.Error("Exception while reconnecting", ex);
                }
                log.Info("[Client] re-Connected to server");
            }
        }
    }
}
