using ShivaQEcommon;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace ShivaQEmaster
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public delegate void KeyCapturedEventHandler(bool status);
        public static event KeyCapturedEventHandler KeyCaptured;

        protected override void OnStartup(StartupEventArgs e)
        {
            EventManager.RegisterClassHandler(typeof(TextBox),
                TextBox.GotKeyboardFocusEvent, new RoutedEventHandler(TextBox_GotFocus));

            EventManager.RegisterClassHandler(typeof(TextBox),
                TextBox.LostKeyboardFocusEvent, new RoutedEventHandler(TextBox_LostFocus));

            base.OnStartup(e);
        }

        private void TextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            //TODO: set mousecaptured status
            KeyCaptured(true);
        }

        private void TextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            //TODO
            KeyCaptured(false);
        }
    }
}
