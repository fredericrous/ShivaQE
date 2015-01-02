using Newtonsoft.Json;
using ShivaQEcommon.Eventdata;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShivaQEmaster
{
    public class Recorder
    {
        MemoryStream _memorystream;
        StreamWriter _streamWriter;
        //StreamReader _streamReader;

        private static readonly Recorder _instance = new Recorder();

        private Recorder() { }

        public static Recorder Instance
        {
            get
            {
                return _instance; 
            }
        }

        private bool _isActive;
        public bool isActive
        {
            get { return _isActive; }
            set 
            {
                _isActive = value; 
            }
        }

        public void Init()
        {
            _isActive = true;
            _memorystream = new MemoryStream();
            _streamWriter = new StreamWriter(_memorystream);
        }

        public void Record(MouseNKeyEventArgs ev)
        {
            if (!_isActive)
                return;

            string json = JsonConvert.SerializeObject(ev);

            _streamWriter.Write(json);
        }

        public void Save(string scenarioName = "default")
        {
            _streamWriter.Flush();
            FileStream fileStream = new FileStream(scenarioName + ".record.json", FileMode.Create);
            _memorystream.WriteTo(fileStream);
            fileStream.Close();
            _memorystream.Dispose();
            _isActive = false;
        }
    }
}
