using log4net;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ShivaQEcommon;
using ShivaQEcommon.Eventdata;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace ShivaQEmaster
{
    public class Recorder
    {
        private static readonly ILog _log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        List<MouseNKeyEventArgs> _eventList;
        Timer _timerRecording;
        Stopwatch _watchRecording;

        public delegate void TimeElapsedEvent(TimeSpan elapsed);
        public event TimeElapsedEvent TimeElapsed;

        string sourceRecordedImageDirectory = Environment.ExpandEnvironmentVariables("%tmp%");

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
            _timerRecording = new Timer();
            _timerRecording.Elapsed += (_s, _e) =>
            {
                TimeElapsed(TimeSpan.FromMilliseconds(_watchRecording.ElapsedMilliseconds));
            };
            _timerRecording.Interval = 1;
        }

        public void Start()
        {
            _isActive = true;

            _watchRecording = Stopwatch.StartNew();

            //if (_eventList == null)
            //{
                _eventList = new List<MouseNKeyEventArgs>();
            //}

            _timerRecording.Start();
        }

        private ImageCodecInfo GetEncoder(ImageFormat format)
        {

            ImageCodecInfo[] codecs = ImageCodecInfo.GetImageDecoders();

            foreach (ImageCodecInfo codec in codecs)
            {
                if (codec.FormatID == format.Guid)
                {
                    return codec;
                }
            }
            return null;
        }

        public void Write(MouseNKeyEventArgs ev)
        {
            if (!_isActive)
                return;

            ev.timestamp = _watchRecording.ElapsedMilliseconds;

            try
            {
                string screenName = string.Format("record.{0}.jpg", ev.timestamp);
                Bitmap screenCapture = ScreenCapturePInvoke.CaptureFullScreen(true);
                Bitmap reducedCapture = ReduceBitmap(screenCapture, (int)(screenCapture.Width * 0.7), (int)(screenCapture.Height * 0.7));

                System.Drawing.Imaging.Encoder myEncoder = System.Drawing.Imaging.Encoder.Quality;
                EncoderParameters myEncoderParameters = new EncoderParameters(1);
                EncoderParameter myEncoderParameter = new EncoderParameter(myEncoder, 50L);
                myEncoderParameters.Param[0] = myEncoderParameter;

                reducedCapture.Save(sourceRecordedImageDirectory + "\\" + screenName, GetEncoder(ImageFormat.Jpeg), myEncoderParameters);

                ev.screenshot = screenName;
            }
            catch (Exception ex)
            {
                _log.Error(ex.Message, ex);
            }

            try
            {
                _eventList.Add(ev);
            }
            catch (Exception ex)
            {
                _log.Error(ex.Message, ex);
            }
        }

        private Bitmap ReduceBitmap(Bitmap original, int reducedWidth, int reducedHeight)
        {
            var reduced = new Bitmap(reducedWidth, reducedHeight);
            using (var dc = Graphics.FromImage(reduced))
            {
                // you might want to change properties like
                dc.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                dc.DrawImage(original, new Rectangle(0, 0, reducedWidth, reducedHeight), new Rectangle(0, 0, original.Width, original.Height), GraphicsUnit.Pixel);
            }

            return reduced;
        }

        public void Save(string scenarioName = "default")
        {
            _timerRecording.Stop();

            string json = JsonConvert.SerializeObject(_eventList);

            string recordFile = string.Format("{0}\\record.json", sourceRecordedImageDirectory);

            using (MemoryStream memorystream = new MemoryStream())
            {
                using (StreamWriter streamWriter = new StreamWriter(memorystream, Encoding.UTF8))
                {
                    streamWriter.Write(json);
                    streamWriter.Flush();
                    using (FileStream fileStream = new FileStream(recordFile, FileMode.Create))
                    {
                        memorystream.WriteTo(fileStream);
                    }
                }
            }

            using (FileStream zipToOpen = new FileStream(scenarioName + ".zip", FileMode.Create))
            {
                using (ZipArchive archive = new ZipArchive(zipToOpen, ZipArchiveMode.Update))
                {
                    var file = recordFile;

                    archive.CreateEntryFromFile(file, file.Substring(file.LastIndexOf("\\") + 1));

                    foreach (var item in _eventList)
                    {
                        file = string.Format("{0}\\{1}", sourceRecordedImageDirectory, item.screenshot);

                        archive.CreateEntryFromFile(file, file.Substring(file.LastIndexOf("\\") + 1));
                    }
                }
            }

            File.Delete(recordFile);
            foreach (var item in _eventList)
            {
                File.Delete(string.Format("{0}\\{1}", sourceRecordedImageDirectory, item.screenshot));
            }

            _isActive = false;
        }

        public void Load(string scenarioName = "default")
        {
            //if (!_isActive)
            //    return;


            //clean just in case
            string recordFile = string.Format("{0}\\record.json", sourceRecordedImageDirectory);
            try
            {
                File.Delete(recordFile);
            }
            catch { }
            List<string> recordImages = Directory.EnumerateFiles(sourceRecordedImageDirectory, "record.*.jpg").ToList();
            foreach (var item in recordImages)
            {
                File.Delete(item);
            }


            using (FileStream zipToOpen = new FileStream(scenarioName, FileMode.Open))
            {
                using (ZipArchive archive = new ZipArchive(zipToOpen, ZipArchiveMode.Read))
                {
                    archive.ExtractToDirectory(sourceRecordedImageDirectory);
                }
            }

            StreamReader reader = new StreamReader(recordFile);
            string value = reader.ReadToEnd();

            _eventList = JsonConvert.DeserializeObject<List<MouseNKeyEventArgs>>(value);
        }

        public void Preview()
        {
            try
            {
                if (_eventList != null && _eventList.Count > 0)
                {
                    RecordViewerWindow recordViewer = new RecordViewerWindow();
                    recordViewer.Show();

                    List<Task> taskList = new List<Task>();
                    for (var i = 0; i < _eventList.Count; i++)
                    {
                        string fileName = string.Format("{0}\\{1}", sourceRecordedImageDirectory, _eventList[i].screenshot);
                        TimeSpan ts = TimeSpan.FromMilliseconds(_eventList[i].timestamp);
                        taskList.Add(
                            recordViewer.UpdateImg(ts, fileName,
                                string.Format("{0:mm\\:ss\\.ff}: {1} ({2})", ts, _eventList[i].key, _eventList[i].keyData))
                        );
                    }
                }
            }
            catch (Exception ex)
            {
                _log.Error(ex.Message, ex);
            }

        }

        private async Task<bool> ExecuteEvent(MouseNKeyEventArgs ev)
        {
            SlaveManager slaveManager = SlaveManager.Instance;

            bool result = false;
            try
            {
                await Task.Delay((int)ev.timestamp);
                /*await*/ slaveManager.Send<MouseNKeyEventArgs>(ev);
                result = true;
            }
            catch (TaskCanceledException ex)
            {
                _log.Info("canceled");
            }
            catch (Exception ex)
            {
                _log.Error("error display delayed img", ex);
            }
            return result;
        }

        public void Play()
        {
            Preview();
            SlaveManager slaveManager = SlaveManager.Instance;
            foreach (MouseNKeyEventArgs mnk_event in _eventList)
            {
                foreach (Slave slave in slaveManager.slaveList)
                {
                    ExecuteEvent(mnk_event);
                }
            }
        }
    }
}
