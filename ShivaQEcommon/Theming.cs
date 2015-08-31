using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace ShivaQEcommon
{
    /// <summary>
    /// Change current windows theme
    /// source: http://stackoverflow.com/questions/546818/how-do-i-change-the-current-windows-theme-programatically
    /// </summary>
    public class Theming
    {
        /// Handles to Win 32 API
        [DllImport("user32.dll", EntryPoint = "FindWindow")]
        private static extern IntPtr FindWindow(string sClassName, string sAppName);
        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);
        [DllImport("dwmapi.dll", PreserveSig = false)]
        private static extern int DwmEnableComposition(bool fEnable);

        /// Windows Constants
        private const uint WM_CLOSE = 0x10;

        private static String StartProcessAndWait(string filename, string arguments, int seconds, ref Boolean bExited)
        {
            String msg = String.Empty;
            Process p = new Process();
            p.StartInfo.WindowStyle = ProcessWindowStyle.Minimized;
            p.StartInfo.FileName = filename;
            p.StartInfo.Arguments = arguments;
            p.Start();

            bExited = false;
            int counter = 0;
            /// give it "seconds" seconds to run
            while (!bExited && counter < seconds)
            {
                bExited = p.HasExited;
                counter++;
                System.Threading.Thread.Sleep(1000);
            }

            if (counter == seconds)
            {
                msg = "Program did not close in expected time.";
            }

            return msg;
        }

        private static bool? _isAero = null;
        public static bool? isAero
        {
            set
            {
                if (_isAero == null)
                {
                    _isAero = value;
                }
            }
            get
            {
                return _isAero;
            }
        }

        private static string _themeName = null;
        public static string themeName
        {
            set
            {
                if (_themeName == null)
                {
                    _themeName = value;
                }
            }
            get
            {
                return _themeName;
            }
        }

        public static Boolean SwitchTheme(string themePath)
        {
            themeName = themePath;
            try
            {
                /// Set the theme
                Boolean bExited = false;
                /// essentially runs the command line:  rundll32.exe %SystemRoot%\system32\shell32.dll,Control_RunDLL %SystemRoot%\system32\desk.cpl desk,@Themes /Action:OpenTheme /file:"%WINDIR%\Resources\Ease of Access Themes\classic.theme"
                String ThemeOutput = StartProcessAndWait("rundll32.exe", System.Environment.GetFolderPath(Environment.SpecialFolder.System) + @"\shell32.dll,Control_RunDLL " + System.Environment.GetFolderPath(Environment.SpecialFolder.System) + "\\desk.cpl desk,@Themes /Action:OpenTheme /file:\"" + themePath + "\"", 30, ref bExited);

                Console.WriteLine(ThemeOutput);

                /// Wait for the theme to be set
                System.Threading.Thread.Sleep(1000);

                /// Close the Theme UI Window
                IntPtr hWndTheming = FindWindow("CabinetWClass", null);
                SendMessage(hWndTheming, WM_CLOSE, IntPtr.Zero, IntPtr.Zero);
            }
            catch (Exception ex)
            {
                Console.WriteLine("An exception occured while setting the theme: " + ex.Message);

                return false;
            }
            return true;
        }

        public static Boolean SwitchToClassicTheme()
        {
            string themePath = System.Environment.GetFolderPath(Environment.SpecialFolder.Windows) + @"\Resources\Ease of Access Themes\basic.theme";
            themeName = themePath;
            return SwitchTheme(themePath);
        }

        public static Boolean SwitchToAeroTheme()
        {
            string themePath = System.Environment.GetFolderPath(Environment.SpecialFolder.Windows) + @"\Resources\Themes\aero.theme";
            themeName = themePath;
            return SwitchTheme(themePath);
        }

        public static void setAero(bool activate)
        {
            isAero = activate;
            DwmEnableComposition(activate);
        }
    }
}
