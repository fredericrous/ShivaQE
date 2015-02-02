using Newtonsoft.Json;
using ShivaQEviewer.Languages;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShivaQEviewer
{
    class EditServerPage : AddServerPage
    {
        private Slave _oldSlave;
        SlaveManager _slaveManager;

        public EditServerPage()
        {
            setLabels();
        }

        private void setLabels()
        {
            lb_header.Text = Languages.language_en_US.editpage_header;
            bt_add_add.Content = Languages.language_en_US.editpage_bt_edit;
        }

        public EditServerPage(Slave slave)
        {
            //set text on labels and buttons
            setLabels();

            //keep original slave we are editing
            _oldSlave = slave;

            //set slave informations already registered
            this.Bindings.add_hostname = slave.hostname;
            this.Bindings.add_friendlyname = slave.friendlyName;
            this.Bindings.add_login = slave.login;

            _slaveManager = SlaveManager.Instance;
        }

        //apply edit
        public override void bt_add_add_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            Slave slave = new Slave(this.Bindings.add_hostname)
            {
                friendlyName =  this.Bindings.add_friendlyname,
                login = this.Bindings.add_login,
            };
            if (pb_add_password.Password != null && pb_add_password.Password.Trim() != string.Empty)
            {
                slave.password = pb_add_password.Password;
            }
            else
            {
                slave.password = _oldSlave.password;
            }
            _slaveManager.Remove(_oldSlave);
            _slaveManager.Add(slave);

            //save list to file to be reloaded at next startup
            string slaveListJson = JsonConvert.SerializeObject(_slaveManager.slaveList, Formatting.Indented);
            File.WriteAllText(_slaveList_save_path, slaveListJson);

            this.NavigationService.Navigate(new Uri("Pages/HomePage.xaml", UriKind.Relative));
        }
    }
}
