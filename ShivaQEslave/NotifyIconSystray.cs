using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Windows.Forms;

namespace ShivaQEslave
{
    /// <summary>
    /// Handles the systray icon
    /// </summary>
    static class NotifyIconSystray
    {
        private static NotifyIcon notifyIcon;
        public delegate void Status(bool status);
        private static string _name;

        /// <summary>
        /// This method allows to change the state of icon and tooltip
        /// </summary>
        /// <param name="status">true = Log Active: the  detected the client and is active.</param>
        public static void Status_DelegateMethod(bool status)
        {
            notifyIcon.Text = String.Format("{0}\nstatus: {1}", _name, status ? "on" : "off");

            string nameIcon = status ? "shivaqe_logo_slave.ico" : "shivaqe_logo_slave_off.ico";

            string currDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string iconPath = String.Format(@"{0}\{1}", currDirectory, nameIcon); //icon in base folder

            notifyIcon.Icon = new Icon(iconPath);
        }
        /// <summary>
        /// This delegate allows us to call Status_DelegateMethod in the backgroundworker
        /// It changes the indicator that displays the state of the app.
        /// </summary>
        public static Status ChangeStatus = Status_DelegateMethod;

        /// <summary>
        /// add notification icon to system tray bar (near the clock)
        /// quit option is available by default
        /// </summary>
        /// <param name="name">name displayed on mouse hover</param>
        /// <param name="items">items to add to the context menu</param>
        public static void addNotifyIcon(String name, MenuItem[] items = null)
        {
            _name = name;
            notifyIcon = new System.Windows.Forms.NotifyIcon();
            notifyIcon.Visible = true;

            Status_DelegateMethod(false); //set name and icon

            ContextMenu contextMenu1 = new ContextMenu();
            if (items != null)
            {
                contextMenu1.MenuItems.AddRange(items);
            }
            contextMenu1.MenuItems.Add(new MenuItem("Quit", (s, e) =>
            {
                notifyIcon.Dispose();
                foreach (ProcessThread thread in Process.GetCurrentProcess().Threads)
                {
                    thread.Dispose();
                }
                Process.GetCurrentProcess().Kill();
            }));
            notifyIcon.ContextMenu = contextMenu1;

        }
    }
}
