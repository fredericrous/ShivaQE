using log4net;
using ShivaQEcommon;
using ShivaQEviewer.TerminalServices;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
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

        public delegate void UpdateStatusEvent(string status);
        public event UpdateStatusEvent UpdateStatus;

        public async Task<bool> Run()
        {
            string status;

            status = string.Format("{0} : starting", _slave.hostname);
            UpdateStatus(status);

            //does slave has framework 4.5
            status = string.Format("{0} : check slave has 4.5 framework or + installed (trully only checking that 4.0 or supperior version is on because 4.5 overrides 4.0)", _slave.hostname);
            UpdateStatus(status);

            bool isFrameworkInstalled = checkFrameworkExists(_slave);
            status = string.Format("{0} : 4.5 framework presence: {1}", _slave.hostname, isFrameworkInstalled ? "OK" : "Not found");
            UpdateStatus(status);

            List<TerminalSessionData> lastSessionList = TermServicesManager.ListSessions(_slave.ipAddress);

            status = string.Format("{0} : openning rdp for this server", _slave.hostname);
            UpdateStatus(status);

            //open rdp
            executeCmd(Environment.ExpandEnvironmentVariables(@"%SystemRoot%\system32\mstsc.exe"),
                string.Format("/v:{0} /h:{1} /w:{2}",
                _slave.hostname, _resolution_height, _resolution_width), false, false, _slave);

            status = string.Format("{0} : copying slave to this server", _slave.hostname);
            UpdateStatus(status);

            //copy slave
            try
            {
                CopyOverNetwork.UpdateStatus += (_status) =>
                    {
                        UpdateStatus(_status);
                    };

                if (string.IsNullOrWhiteSpace(_slave.login)) 
                {
                    CopyOverNetwork.CopyFiles(executablePath + "slave", String.Format(@"\\{0}\c$\ShivaQEslave", _slave.hostname));
                }
                else
                {
                    CopyOverNetwork.CopyFiles(executablePath + "slave", String.Format(@"\\{0}\c$\ShivaQEslave", _slave.hostname), _slave.login, _slave.password);
                }
            }
            catch (Exception ex)
            {
                status = string.Format("Error while copying ShivaQEslave to {0}: {1}", _slave.hostname, ex.Message);
                UpdateStatus(status);
            }

            // wait rdp connection is done before executing psexec.
            // Psexec needs that rdp connection in order to gain access rights

            int timeoutLimit = 120; //after 2min lets consider this a fail

            UpdateStatus(string.Format("{0}: waiting for rdp completion. If it takes more than {1} min, it will be aborted.", _slave.hostname, 120 / 60));

            TerminalSessionData sessionData = await TermServicesManager.GetNewSession(_slave.ipAddress, lastSessionList, timeoutLimit);
            int sessionId = sessionData != null ? sessionData.SessionId : 2;

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

            status = string.Format("{0} : launch slave on this server", _slave.hostname);
            UpdateStatus(status);

            string loginParams = string.Empty;
            if (!string.IsNullOrWhiteSpace(_slave.login))
            {
                loginParams = string.Format(@"-u ""{0}""", _slave.login);

                loginParams += string.IsNullOrWhiteSpace(_slave.password)
                    ? string.Empty : string.Format(@" -p ""{0}""", _slave.password);
            }
            string port = _slave.port == 0 ? string.Empty : _slave.port.ToString();

            string cmd = string.Format(@"\\{0} {1} -i {2} -accepteula -d ""C:\ShivaQEslave\ShivaQEslave.exe {3}""",
                _slave.ipAddress, loginParams, sessionId, port);
            executeCmd(executablePath + _path_cmd_launcher, cmd , true, true);

            status = string.Format("{0} : done{1}", _slave.hostname,
                isFrameworkInstalled ? string.Empty : ". Beware: if slave's launched failed check you have .Net 4.5 installed on it"
            );
            UpdateStatus(status);

            return true;
        }

        /// <summary>
        /// check on slave that .Net 4.0 or sup is installed (4.5 check is a bit harder to check because install dir overrides 4.0...)
        /// </summary>
        /// <param name="_slave"></param>
        /// <returns></returns>
        private bool checkFrameworkExists(Slave _slave)
        {
            string frameworkFolder = @"Windows\Microsoft.NET\Framework";
            bool result = false;

            string[] frameworkDirectories;

            if (string.IsNullOrWhiteSpace(_slave.login))
            {
                frameworkDirectories = CopyOverNetwork.GetFoldersFrom(String.Format(@"\\{0}\c$\{1}", _slave.hostname, frameworkFolder));
            }
            else
            {
                frameworkDirectories = CopyOverNetwork.GetFoldersFrom(String.Format(@"\\{0}\c$\{1}", _slave.hostname, frameworkFolder), _slave.login, _slave.password);
            }

            if (frameworkDirectories.Length > 0)
            {
                foreach (var directory in frameworkDirectories)
                {
                    float version;
                    string dir_version = directory.Substring(directory.LastIndexOf('\\')).Substring(2, 3);
                    if (float.TryParse(dir_version, NumberStyles.AllowDecimalPoint, CultureInfo.CreateSpecificCulture("en-US"), out version))
                    {
                        if (version >= 4)
                        {
                            result = true;
                            break;
                        }
                    }
                }
            }
            return result;
        }

        /// <summary>
        /// launch an executable.
        /// </summary>
        /// <param name="filename">the name of the executable</param>
        /// <param name="argument">the arguments to pass to the executable</param>
        /// <param name="hidden">should the window of the executable be displayed</param>
        /// <param name="redirectOutput">should output be redirected to log</param>
        /// <param name="slave"></param>
        /// <returns></returns>
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

        //check if _path_cmd_launcher (psexec) exists
        internal static void CmdLauncherExists()
        {
            if (!File.Exists(_path_cmd_launcher))
            {
                MessageBox.Show(string.Format("ShivaQE Viewer requires {0}. Copy it in same folder as ShivaQEViewer.exe", _path_cmd_launcher));
            }
        }
    }
}
