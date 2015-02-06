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
using ShivaQEviewer;

namespace ShivaQEviewer
{
    /// <summary>
    /// Handle slaves
    /// </summary>
    public class SlaveManager
    {
        private static readonly ILog _log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        //list of slaves computer that this master will send information to
        public ObservableCollection<Slave> slaveList;

        private static readonly string _slaveList_save_path = "slavelist.json";

        public string SlaveListPath { get { return _slaveList_save_path; } }

        private static readonly SlaveManager _instance = new SlaveManager();

        private SlaveManager() { }

        public static SlaveManager Instance
        {
            get
            {
                return _instance; 
            }
        }

        public void Init(ObservableCollection<Slave> slaves)
        {
            slaveList = slaves;

            //load serverlist json save
            if (File.Exists(_slaveList_save_path))
            {
                try
                {
                    string slaveListJson = File.ReadAllText(_slaveList_save_path);
                    ObservableCollection<Slave> slaveListFromJson = JsonConvert.DeserializeObject<ObservableCollection<Slave>>(slaveListJson);

                    foreach (var slave in slaveListFromJson)
                    {
                        Slave newSlave = new Slave(slave.hostname)
                        {
                            friendlyName = slave.friendlyName,
                            ipAddress = slave.ipAddress
                        };
                        if (slave.login != null && slave.password != null)
                        {
                            newSlave.login = slave.login;
                            newSlave.password = slave.password;
                        }
                        slaveList.Add(newSlave);
                    }
                    slaveListFromJson = null;
                }
                catch (Exception ex)
                {
                    _log.Error(string.Format("load json {0}", _slaveList_save_path), ex);
                }
            }
        }

        public void Add(Slave slave)
        {
            slaveList.Add(slave);
        }

        public void Remove(Slave slave)
        {
            slaveList.Remove(slave);
        }
    }
}
