using log4net;
using SimpleImpersonation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ShivaQEcommon
{
    public class CopyOverNetwork
    {
        private static readonly ILog _log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public delegate void UpdateStatusEvent(string status);
        public static event UpdateStatusEvent UpdateStatus;

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
        public static void CopyFiles(string source_dir, string destination_dir, string login, string password)
        {
            string domain;
            setDomain_and_login(out domain, ref login);

            //log user for distant copy paste
            using (Impersonation.LogonUser(domain, login, password, LogonType.NewCredentials)) // warning static
            {
                CopyFiles(source_dir, destination_dir);
            }
        }

        private static void setDomain_and_login(out string domain, ref string login)
        {
            if (login.IndexOf('\\') != -1)
            {
                string[] domainNlogin = login.Split('\\');
                domain = domainNlogin[0];
                login = domainNlogin[1];
            }
            else
            {
                domain = Environment.UserDomainName; //"localhost";
            }
        }

        public static void CopyFiles(string source_dir, string destination_dir)
        {
            bool isFile = File.Exists(source_dir);

            if (!Directory.Exists(source_dir) && !isFile)
            {
                _log.Error(string.Format("Can't copy. Source {0} does not exists ", source_dir));
                return;
            }

            string[] directories;
            string[] files;

            if (isFile)
            {
                directories = new string[] { }; //not really clean but quick
                files = new string[] { source_dir };
            }
            else
            {
                directories = Directory.GetDirectories(source_dir, "*", SearchOption.AllDirectories);
                files = Directory.GetFiles(source_dir, "*", SearchOption.AllDirectories);
            }

            string host;
            if (destination_dir.Length > 2 && destination_dir.Substring(0, 2) != "\\\\")
            {
               host = destination_dir.Substring(2, destination_dir.Substring(2).IndexOf('\\'));
            }
            else
            {
                host = "localhost";
            }

            ////doesn't work !?
            //if (destination_dir.Contains(':'))
            //{
            //    destination_dir = destination_dir.Replace(':', '-');
            //    destination_dir = destination_dir.Substring(0, destination_dir.IndexOf('\\', 2)) + ".ipv6-literal.net"
            //        + destination_dir.Substring(destination_dir.IndexOf('\\', 2));
            //}
            if (!isFile)
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
                        string error_msg = string.Format("{0} : can't create directory {1} on slave", directory_to_create, destination_dir);
                        _log.Error(error_msg, ex);
                        UpdateStatus(error_msg);
                    }
                }
            }

            foreach (string file_name in files)
            {
                try
                {
                    File.Copy(file_name, destination_dir + file_name.Substring(isFile ? file_name.LastIndexOf('\\') : source_dir.Length), true);
                }
                catch (Exception ex)
                {
                    string error_msg = string.Format("{0} : can't copy {1} on slave", host, file_name);
                    _log.Error(error_msg, ex);
                    UpdateStatus(error_msg);
                }
            }

            string msg = string.Format("{0} : if copy was successful, slave binary is under {1}", host, destination_dir);
            UpdateStatus(msg);
        }

        public static string[] GetFoldersFrom(string source_dir)
        {
            string[] result = new string[] {};
            try
            {
                result = Directory.GetDirectories(source_dir, "v*", SearchOption.TopDirectoryOnly);
            }
            catch { }
            return result;
        }

        public static string[] GetFoldersFrom(string source_dir, string login, string password)
        {
            string[] result;
            string domain;
            setDomain_and_login(out domain, ref login);

            //log user for distant copy paste
            using (Impersonation.LogonUser(domain, login, password, LogonType.NewCredentials)) // warning static
            {
                result = GetFoldersFrom(source_dir);
            }
            return result;
        }
    }
}
