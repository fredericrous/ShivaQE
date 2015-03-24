using log4net;
using ShivaQEcommon;
using System;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Linq;

namespace ShivaQEmaster
{
    public partial class EditServerPage : AddServerPage
    {
        private Slave _oldSlave;

        private static readonly ILog _log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        SlaveManager _slaveManager;
        Analytics _analytics;

        public EditServerPage()
        {
            setLabels();

            _analytics = Analytics.Instance;
            _analytics.PageView("EditServer");

            _slaveManager = SlaveManager.Instance;
        }

        public EditServerPage(Slave slave)
        {
            setLabels();

            _analytics = Analytics.Instance;
            _analytics.PageView("EditServer");

            //keep original slave we are editing
            _oldSlave = slave;

            //set slave informations already registered
            this.Bindings.newSlaveIP = slave.hostname;
            this.Bindings.newSlaveName = slave.friendlyName;

            _slaveManager = SlaveManager.Instance;
        }

        private void setLabels()
        {
            tb_header.Text = Languages.language_en_US.editpage_header;
            bt_add_add.Content = Languages.language_en_US.editpage_bt_edit;
        }

        public static event errorEventHandler ErrorEditMsg;

        public override async void AddServer(string hostname, int port, string friendlyname)
        {
            try
            {
                await _slaveManager.Remove(_oldSlave);
                Slave slave = new Slave(hostname, port);
                slave.friendlyName = friendlyname;
                _slaveManager.Add(slave);
                _analytics.Event("EditServer", "edited");
            }
            catch
            {
                ErrorEditMsg("Can't add host. Is slave activated?");
            }
        }
    }
}