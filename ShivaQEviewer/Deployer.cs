using log4net;
using ShivaQEviewer.TerminalServices;
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

            status = string.Format("{0} : starting {1}", _slave.hostname, Environment.NewLine);
            UpdateStatus(status);

            List<TerminalSessionData> lastSessionList = TermServicesManager.ListSessions(_slave.ipAddress);

            status = string.Format("{0} : openning rdp for this server {1}", _slave.hostname, Environment.NewLine);
            UpdateStatus(status);

            //open rdp
            executeCmd(Environment.ExpandEnvironmentVariables(@"%SystemRoot%\system32\mstsc.exe"),
                string.Format("/v:{0} /h:{1} /w:{2}",
                _slave.ipAddress, _resolution_height, _resolution_width), false, false, _slave);

            status = string.Format("{0} : copying slave to this server {1}", _slave.hostname, Environment.NewLine);
            UpdateStatus(status);

            //copy slave
            try
            {
                copy_files(executablePath + "slave", String.Format(@"\\{0}\c$\ShivaQEslave", _slave.hostname), _slave.login, _slave.password);
            }
            catch (Exception ex)
            {
                status = string.Format("Error while copying ShivaQEslave to {0}: {1}{2}", _slave.hostname, ex.Message, Environment.NewLine);
                UpdateStatus(status);
            }

            // wait rdp connection is done before executing psexec.
            // Psexec needs that rdp connection in order to gain access rights

            int timeoutLimit = 120; //after 2min lets consider this a fail

            UpdateStatus(string.Format("{0}: waiting for rdp completion. If it takes more than {1} min, it will be aborted.", _slave.hostname, 120 / 60));

            TerminalSessionData sessionData = await TermServicesManager.GetNewSession(_slave.ipAddress, lastSessionList, timeoutLimit);	

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
                string.Format(@"\\{0} -u ""{1}"" -p ""{2}"" -i {3} -accepteula -d ""C:\ShivaQEslave\ShivaQEslave.exe {4}""",
                _slave.ipAddress, _slave.login, _slave.password, sessionData.SessionId, _slave.port), true, true);

            return true;
        }

        /// <summary>
        /// copy a directory and its files over the network
        /// </summary>
        /// <param name="source_dir">the source directory</param>
        /// <param name="destination_dir">the destination directory</param>
        /// <param name="login">
        /// windows login credential. specify domain with \. ie: domain\login.
        /// if no domain is suplied. ie: login. domain set is localhost.
        /// </param>
        /// <param name="password">the password of the windows account</param>
        private void copy_files(string source_dir, string destination_dir, string login, string password)
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
                    _log.Warn("Error deleting files: " + ex.Message);
                }

                foreach (string dir in directories)
                {
                    string directory_to_create = destination_dir + dir.Substring(source_dir.Length);
                    try
                    {
                        Directory.CreateDirectory(directory_to_create);
                    }
                    catch (Exception ex)
                    {
                        string error_msg = string.Format("can't create directory {0} on slave", directory_to_create);
                        _log.Error(error_msg, ex);
                        UpdateStatus(error_msg);
                    }
                }

                foreach (string file_name in files)
                {
                    try
                    {
                        File.Copy(file_name, destination_dir + file_name.Substring(source_dir.Length), true);
                    }
                    catch (Exception ex)
                    {
                        string error_msg = string.Format("can't copy {0} on slave", file_name);
                        _log.Error(error_msg, ex);
                        UpdateStatus(error_msg);
                    }
                }
            }
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
