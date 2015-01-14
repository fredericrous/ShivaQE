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

        public void Init(string app_name, string app_version)
        {
            _data = new NameValueCollection();
            _data["v"] = "1"; //version
            _data["tid"] = "UA-57662843-2"; //token
            _data["cid"] = _guid; //anonymous client id
            _data["an"] = app_name; //app tracking
            _data["av"] = app_version;
        }

        public void Event(string category, string action, string label, string value)
        {
            if (SettingsManager.ReadSetting("analytics_status") != "true")
            {
                log.Info("analytics deactivated, event not sent");
                return;
            }

            using (var wb = new WebClient())
            {
                var data = new NameValueCollection(_data);
                data["t"] = "event";            //hit type
                data["ec"] = category;         // Event Category. Required.
                data["ea"] = action;           // Event Action. Required.
                data["el"] = label;            // Event label.
                data["ev"] = value;         // Event value.
                try
                {
                    byte[] responseBuffer = wb.UploadValues(_url, _request_method, data);
                    string response = Encoding.UTF8.GetString(responseBuffer, 0, responseBuffer.Length);
                    log.Info(response);
                }
                catch (WebException ex)
                {
                    log.Error("analytics event error " + ex.Status, ex);
                }
            } 
        }

        public void Exception(Exception exception)
        {
            if (SettingsManager.ReadSetting("analytics_status") != "true")
            {
                log.Info("analytics deactivated, exception not sent");
                return;
            }

            string exceptionType = exception.GetType().Name;

            using (var wb = new WebClient())
            {
                var data = new NameValueCollection(_data);
                data["t"] = "exception"; //hit type
                data["exd"] = exceptionType; // Exception description.
                data["exf"] = (exceptionType == "Exception") ? "1" : "0";  // Exception is fatal?

                try
                {
                    byte[] responseBuffer = wb.UploadValues(_url, _request_method, data);
                    string response = Encoding.UTF8.GetString(responseBuffer, 0, responseBuffer.Length);
                    log.Info(response);
                }
                catch (WebException ex)
                {
                    log.Error("analytics exception error " + ex.Status, ex);
                }
            }
        }
    }
}
