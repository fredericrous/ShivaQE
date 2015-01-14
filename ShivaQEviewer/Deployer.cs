using log4net;
using SimpleImpersonation;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace ShivaQEviewer
{
    class Deployer
    {
        private static readonly ILog _log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        [DllImport("WtsApi32.dll")]
        public static extern bool WTSWaitSystemEvent(IntPtr hServer, int EventMask, out IntPtr pEventFlags);

        [DllImport("WtsApi32.dll")]
        public static extern IntPtr WTSOpenServer(string server);

        [DllImport("wtsapi32.dll")]
        static extern int WTSEnumerateSessions(
                        IntPtr hServer,
                        int Reserved,
                        int Version,
                        ref IntPtr ppSessionInfo,
                        ref int pCount);

        int WTS_EVENT_LOGON = 0x00000020;

        private string executablePath = System.IO.Path.GetDirectoryName(System.Windows.Forms.Application.ExecutablePath).ToString() + "\\";
        private static string _path_cmd_launcher = "PsExec.exe";


        private Slave _slave;
        string _resolution_height;
        string _resolution_width;

        public Deployer(Slave slave, string resolution_height, string resolution_width)
        {
            this._slave = slave;
            this._resolution_height = resolution_height;
            this._resolution_width = resolution_width;
        }

        [DllImport("wtsapi32.dll")]
        static extern void WTSCloseServer(IntPtr hServer);

        [DllImport("wtsapi32.dll")]
        static extern void WTSFreeMemory(IntPtr pMemory);

        [StructLayout(LayoutKind.Sequential)]
        private struct WTS_SESSION_INFO
        {
            public Int32 SessionID;

            [MarshalAs(UnmanagedType.LPStr)]
            public String pWinStationName;

            public WTS_CONNECTSTATE_CLASS State;
        }

        public enum WTS_CONNECTSTATE_CLASS
        {
            WTSActive,
            WTSConnected,
            WTSConnectQuery,
            WTSShadow,
            WTSDisconnected,
            WTSIdle,
            WTSListen,
            WTSReset,
            WTSDown,
            WTSInit
        } 

        public static int CountActiveSessions(String ServerName)
        {
            IntPtr server = IntPtr.Zero;
            int ret = 0;
            server = WTSOpenServer(ServerName);

            try
            {
                IntPtr ppSessionInfo = IntPtr.Zero;

                Int32 count = 0;
                Int32 retval = WTSEnumerateSessions(server, 0, 1, ref ppSessionInfo, ref count);
                Int32 dataSize = Marshal.SizeOf(typeof(WTS_SESSION_INFO));

                Int64 current = (int)ppSessionInfo;

                if (retval != 0)
                {
                    for (int i = 0; i < count; i++)
                    {
                        WTS_SESSION_INFO si = (WTS_SESSION_INFO)Marshal.PtrToStructure((System.IntPtr)current, typeof(WTS_SESSION_INFO));
                        current += dataSize;
                        if (si.State == WTS_CONNECTSTATE_CLASS.WTSActive)
                        {
                            ret++;
                        }
                    }

                    WTSFreeMemory(ppSessionInfo);
                }
            }
            finally
            {
                WTSCloseServer(server);
            }

            return ret;
        }

        public delegate void UpdateStatusEvent(string status);
        public event UpdateStatusEvent UpdateStatus;

        public async Task<bool> Run()
        {
            string status;

            int activeSessionNumber = CountActiveSessions(_slave.ipAddress);

            status = string.Format("{0} : openning rdp for this server {1}", _slave.hostname, Environment.NewLine);
            UpdateStatus(status);

            //open rdp
            IntPtr hServer = executeCmd(Environment.ExpandEnvironmentVariables(@"%SystemRoot%\system32\mstsc.exe"),
                string.Format("/v:{0} /h:{1} /w:{2}",
                _slave.ipAddress, _resolution_height, _resolution_width), false, false, _slave);

            status = string.Format("{0} : copying slave to this server {1}", _slave.hostname, Environment.NewLine);
            UpdateStatus(status);

            //copy slave
            if (!copy_files(executablePath + "slave", String.Format(@"\\{0}\c$\ShivaQEslave", _slave.hostname), _slave.login, _slave.password))
                return false;

            // wait rdp connection is done before executing psexec.
            // Psexec needs that rdp connection in order to gain access rights
            int timeoutCounter = 0;
            int timeoutLimit = 120; //after 2min lets consider this a fail
            while (activeSessionNumber + 1 != CountActiveSessions(_slave.ipAddress))
            {
                await Task.Delay(1000);
                if (timeoutCounter >= timeoutLimit)
                {
                    break;
                }
                timeoutCounter++;
            }

            string username = _slave.login;
            if (username.IndexOf('\\') != -1)
            {
                string[] domainNlogin = username.Split('\\');
                username = domainNlogin[1];
            }

            //get graphical session id

            //http://www.codeproject.com/Articles/111430/Grabbing-Information-of-a-Terminal-Services-Sessio

            //executeCmd(Environment.ExpandEnvironmentVariables(@"%SystemRoot%\system32\cmd.exe"),
            //    string.Format(@"/C "" ""{0}"" \\{1} -u ""{2}"" -p ""{3}"" -accepteula query session ""{4}"" """,
            //    executablePath + _path_cmd_launcher,
            //    slave.ipAddress, slave.login, slave.password, username), true, true);


            //start slave with psexec
            // option accepteula remove license warning
            // option i 2 tells psexec our software wants a ui
            // d tells not to wait for end of execution

            status = string.Format("{0} : launch slave on this server {1}", _slave.hostname, Environment.NewLine);
            UpdateStatus(status);
            executeCmd(executablePath + _path_cmd_launcher,
                string.Format(@"\\{0} -u ""{1}"" -p ""{2}"" -i 2 -accepteula -d ""C:\ShivaQEslave\ShivaQEslave.exe""",
                _slave.ipAddress, _slave.login, _slave.password), true, true);

            return true;
        }

        private bool copy_files(string source_dir, string destination_dir, string login, string password)
        {
            string[] directories = Directory.GetDirectories(source_dir, "*", SearchOption.AllDirectories);
            string[] files = Directory.GetFiles(source_dir, "*", SearchOption.AllDirectories);

            ////doesn't work !?
            //if (destination_dir.Contains(':'))
            //{
            //    destination_dir = destination_dir.Replace(':', '-');
            //    destination_dir = destination_dir.Substring(0, destination_dir.IndexOf('\\', 2)) + ".ipv6-literal.net"
            //        + destination_dir.Substring(destination_dir.IndexOf('\\', 2));
            //}

            string domain = "localhost";
            if (login.IndexOf('\\') != -1)
            {
                string[] domainNlogin = login.Split('\\');
                domain = domainNlogin[0];
                login = domainNlogin[1];
            }

            //log user for distant copy paste
            using (Impersonation.LogonUser(domain, login, password, LogonType.NewCredentials)) // warning static
            {
                try
                {
                    if (Directory.Exists(destination_dir))
                    {
                        Directory.Delete(destination_dir, true);
                    }

                    Directory.CreateDirectory(destination_dir);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error deleting files: " + ex.Message);
                }

                foreach (string dir in directories)
                {
                    Directory.CreateDirectory(destination_dir + dir.Substring(source_dir.Length));
                }

                foreach (string file_name in files)
                {
                    try
                    {
                        File.Copy(file_name, destination_dir + file_name.Substring(source_dir.Length), true);
                    }
                    catch (Exception ex)
                    {
                        _log.Error(string.Format("can't copy {0}", file_name), ex);
                    }
                }

                return true;
            }
        }

        private IntPtr executeCmd(string filename, string argument, bool hidden = false, bool redirectOutput = false, Slave slave = null)
        {
            Process process = new Process();

            if (slave != null)
            {
                process.StartInfo.FileName = Environment.ExpandEnvironmentVariables(@"%SystemRoot%\system32\cmdkey.exe");
                process.StartInfo.Arguments = string.Format("/generic:TERMSRV/{0} /user:{1} /pass:{2}", slave.ipAddress, slave.login, slave.password);
                process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                process.StartInfo.CreateNoWindow = true;
                process.Start();
            }

            ProcessStartInfo startInfo = new ProcessStartInfo();
            if (hidden)
            {
                startInfo.WindowStyle = ProcessWindowStyle.Hidden;
                startInfo.CreateNoWindow = true;
            }

            if (redirectOutput)
            {
                startInfo.UseShellExecute = false;
                startInfo.RedirectStandardOutput = true;
                startInfo.RedirectStandardError = true;
                process.EnableRaisingEvents = true;
                process.OutputDataReceived += (s, e) =>
                {
                    _log.Info(e.Data);
                };
                process.ErrorDataReceived += (s, e) =>
                {
                    _log.Info("error: " + e.Data);
                };
            }

            _log.Info(string.Format("execute: {0} {1}", filename, argument));

            startInfo.FileName = filename;
            startInfo.Arguments = argument;
            process.StartInfo = startInfo;
            process.Start();


            if (redirectOutput)
            {
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
            }

            process.WaitForExit();

            //if (slave != null)
            //{
            //    process.StartInfo.FileName = Environment.ExpandEnvironmentVariables(@"%SystemRoot%\system32\cmdkey.exe");
            //    process.StartInfo.Arguments = string.Format("/delete:TERMSRV/{0}", slave.ipAddress);
            //    process.Start();
            //}
            return process.Handle;
        }


        internal static void CmdLauncherExists()
        {
            if (!File.Exists(_path_cmd_launcher))
            {
                MessageBox.Show(string.Format("ShivaQE Viewer requires {0}. Copy it in same folder as ShivaQEViewer.exe", _path_cmd_launcher));
            }
        }
    }
}
