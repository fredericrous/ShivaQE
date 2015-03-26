using log4net;
using ShivaQEcommon;
using ShivaQEviewer.TerminalServices;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
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

        public delegate void UpdateStatusEvent(string status, bool newStart = false);
        public event UpdateStatusEvent UpdateStatus;

        /// <summary>
        /// abort waiting delay for GetNewSession
        /// </summary>
        public void Skip()
        {
            TermServicesManager.GetNewSessionAbortDelay();
        }

        public static bool PingHost(string nameOrAddress)
        {
            bool pingable = false;
            Ping pinger = new Ping();

            try
            {
                PingReply reply = pinger.Send(nameOrAddress);

                pingable = reply.Status == IPStatus.Success;
            }
            catch (PingException)
            {
                // Discard PingExceptions and return false;
            }

            return pingable;
        }

        /// <summary>
        /// run the deployment tasks. check if .net is present. open rdp. copy ShivaQEslave to distant computer that will become a slave. launch ShivaQEslave
        /// </summary>
        /// <returns></returns>
        public async Task<bool> Run()
        {
            string status;
            bool result = true;

            status = string.Format("{0} : starting", _slave.hostname);
            UpdateStatus(status, true);

            //is host reachable
            status = string.Format("{0} : check if reachable, in order to deploy slave on it.", _slave.hostname);
            UpdateStatus(status);
            result = PingHost(_slave.hostname);
            if (!result)
            {
                return result;
            }

            //does slave has framework 4.5
            status = string.Format("{0} : check slave has 4.5 framework or + installed (trully only checking that 4.0 or supperior version is on because 4.5 overrides 4.0)", _slave.hostname);
            UpdateStatus(status);

            bool isFrameworkInstalled = checkFrameworkExists(_slave);
            status = string.Format("{0} : 4.5 framework presence: {1}", _slave.hostname, isFrameworkInstalled ? "OK" : "Not found");
            UpdateStatus(status);
            result = isFrameworkInstalled;

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
                result = false;
            }

            // wait rdp connection is done before executing psexec.
            // Psexec needs that rdp connection in order to gain access rights

            int timeoutLimit = 120; //after 2min lets consider this a fail

            string waitSentence = string.Format("If it takes more than {0} min, it will be aborted.", timeoutLimit / 60);
            if (lastSessionList.Count == 0) //if ListSessions failed
            {
                timeoutLimit = 60;
                waitSentence = "Can't detect if you are/will loggin successfuly.";
                waitSentence += Environment.NewLine;
                waitSentence += string.Format("Waiting {0} min in case of a login prompt, to let you time to enter your credentials.", timeoutLimit / 60);
                waitSentence += Environment.NewLine;
                waitSentence += "Press [Enter] to skip waiting.";
            }

            UpdateStatus(string.Format("{0}: waiting for rdp completion. {1}", _slave.hostname, waitSentence));

            TerminalSessionData sessionData = await TermServicesManager.GetNewSession(_slave.ipAddress, lastSessionList, timeoutLimit);
 
            int sessionId = sessionData != null ? sessionData.SessionId : 2;

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

            if (string.IsNullOrEmpty(_slave.password))
            {
                status = "Because of WindowsOS restrictions when password is null, remote launch of slave may fail";
                UpdateStatus(status);
            }

            //try
            //{
            //    wmiCmd(_slave.ipAddress, _slave.login, _slave.password, @"C:\ShivaQEslave\ShivaQEslave.exe");
            //}
            //catch (Exception ex)
            //{
            //    _log.Error(ex.Message, ex);
            //}
            executeCmd(executablePath + _path_cmd_launcher, cmd , true, true);

            status = string.Format("{0} : done{1}", _slave.hostname,
                isFrameworkInstalled ? string.Empty : ". Beware: if slave's launched failed check you have .Net 4.5 installed on it"
            );
            UpdateStatus(status);

            return result;
        }

        /// <summary>
        /// WMI could replace psexec but I have problems making it work
        /// </summary>
        /// <param name="ipAddress"></param>
        /// <param name="login"></param>
        /// <param name="password"></param>
        /// <param name="cmd"></param>
        private void wmiCmd(string ipAddress, string login, string password, string cmd)
        {
            System.Management.ConnectionOptions connOptions =
                new System.Management.ConnectionOptions();

            string domain;
            CopyOverNetwork.setDomain_and_login(out domain, ref login);

            connOptions.Impersonation = System.Management.ImpersonationLevel.Impersonate;
            //connOptions.Authentication = System.Management.AuthenticationLevel.Packet;
            connOptions.EnablePrivileges = true;

            connOptions.Authority = "ntlmdomain:" + domain;
            connOptions.Username = login;
            if (!string.IsNullOrWhiteSpace(password))
            {
                connOptions.Password = password;
            }

            System.Management.ManagementScope manScope =
                new System.Management.ManagementScope(
                    String.Format(@"\\{0}\ROOT\CIMV2", ipAddress), connOptions);
            manScope.Connect();

            System.Management.ObjectGetOptions objectGetOptions =
                new System.Management.ObjectGetOptions();

            System.Management.ManagementPath managementPath =
                new System.Management.ManagementPath("Win32_Process");

            System.Management.ManagementClass processClass =
                new System.Management.ManagementClass(manScope, managementPath, objectGetOptions);

            System.Management.ManagementBaseObject inParams =
                processClass.GetMethodParameters("Create");

            inParams["CommandLine"] = cmd;

            System.Management.ManagementBaseObject outParams =
                processClass.InvokeMethod("Create", inParams, null);
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
                process.StartInfo.Arguments = string.Format("/generic:{0} /user:{1}{2}", slave.ipAddress, slave.login, slave.password ?? " /pass:" + _slave.password);
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
                //startInfo.RedirectStandardInput = true;
                process.EnableRaisingEvents = true;
                process.OutputDataReceived += (s, e) =>
                {
                    _log.Info(e.Data);
                    UpdateStatus(e.Data);
                };
                process.ErrorDataReceived += (s, e) =>
                {
                    _log.Info(e.Data);
                    UpdateStatus(e.Data);
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

            process.WaitForExit(12000); //timeout 12sec

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
                string error_requierment = string.Format("ShivaQE Viewer requires {0}. Copy it in same folder as ShivaQEViewer.exe", _path_cmd_launcher);
                _log.Info(error_requierment);
                MessageBox.Show(error_requierment);
            }
        }
    }
}
