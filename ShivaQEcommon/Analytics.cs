using log4net;
using System;
using System.Collections.Specialized;
using System.Net;
using System.Reflection;
using System.Text;

namespace ShivaQEcommon
{
    public class Analytics
    {
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        static string _url = "http://www.google-analytics.com/collect";
        static string _guid = Guid.NewGuid().ToString();
        NameValueCollection _data;
        string _request_method = "POST";

        private static readonly Analytics _instance = new Analytics();

        private Analytics() { }

        public static Analytics Instance
        {
            get
            {
                return _instance; 
            }
        }

        public void Init(string app_name, string app_version)
        {
            _data = new NameValueCollection();
            _data["v"] = "1"; //version
            _data["tid"] = "UA-57662843-2"; //token
            _data["cid"] = _guid; //anonymous client id
            _data["an"] = app_name; //app tracking
            _data["av"] = app_version;
        }

        /// <summary>
        /// send data to google analytics
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        private string sendTrace(NameValueCollection data)
        {
            string response = string.Empty;
            using (var wb = new WebClient())
            {
                try
                {
                    byte[] responseBuffer = wb.UploadValues(_url, _request_method, data);
                    response = Encoding.UTF8.GetString(responseBuffer, 0, responseBuffer.Length);
                }
                catch (WebException ex)
                {
                    log.Error(string.Format("analytics event error {0}. Fallback to request with no default proxy selected.", ex.Status), ex);
                    if (ex.InnerException.HResult == -2146232062) //if proxy configuration error
                    {
                        using (var wb2 = new WebClient())
                        {
                            WebRequest.DefaultWebProxy = null; //try by disabling proxy
                            try
                            {
                                byte[] responseBuffer = wb2.UploadValues(_url, _request_method, data);
                                response = Encoding.UTF8.GetString(responseBuffer, 0, responseBuffer.Length);
                            }
                            catch (WebException wex)
                            {
                                log.Error("Proxy error. analytics event error " + ex.Status, wex);
                            }
                        }
                    }
                }
            }
            return response;
        }

        public void Event(string category, string action, string label = null, string value = null)
        {
            if (SettingsManager.ReadSetting("analytics_status") != "true")
            {
                log.Info("analytics deactivated, event not sent");
                return;
            }

            var data = new NameValueCollection(_data);
            data["t"] = "event";            //hit type
            data["ec"] = category;         // Event Category. Required.
            data["ea"] = action;           // Event Action. Required.
            if (label != null)
            {
                data["el"] = label;            // Event label.
            }
            if (value != null)
            {
                data["ev"] = value;         // Event value.
            }

            string response = sendTrace(data);
            log.Info(response);
        }

        public void PageView(string documentTitle)
        {
            if (SettingsManager.ReadSetting("analytics_status") != "true")
            {
                log.Info("analytics deactivated, pageview not sent");
                return;
            }

            var data = new NameValueCollection(_data);
            data["t"] = "pageview"; //hit type
            data["dt"] = documentTitle; // Title of the page.

            string response = sendTrace(data);
            log.Info(response);
        }

        public void Exception(Exception exception)
        {
            if (SettingsManager.ReadSetting("analytics_status") != "true")
            {
                log.Info("analytics deactivated, exception not sent");
                return;
            }

            string exceptionType = exception.GetType().Name;

            var data = new NameValueCollection(_data);
            data["t"] = "exception"; //hit type
            data["exd"] = exceptionType; // Exception description.
            data["exf"] = (exceptionType == "Exception") ? "1" : "0";  // Exception is fatal?

            string response = sendTrace(data);
            log.Info(response);
        }
    }
}
