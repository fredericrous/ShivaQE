using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using System.Linq;
using System.Threading.Tasks;
using log4net;
using System.Reflection;

namespace ShivaQEviewer.TerminalServices
{
    public class TermServicesManager
    {
        private static readonly ILog _log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        [DllImport("wtsapi32.dll")]
        static extern IntPtr WTSOpenServer([MarshalAs(UnmanagedType.LPStr)] String pServerName);

        [DllImport("wtsapi32.dll")]
        static extern void WTSCloseServer(IntPtr hServer);

        [DllImport("Wtsapi32.dll")]
        public static extern bool WTSQuerySessionInformation(IntPtr hServer, int sessionId, WTS_INFO_CLASS wtsInfoClass,
            out System.IntPtr ppBuffer, out uint pBytesReturned);

        [DllImport("wtsapi32.dll")]
        static extern Int32 WTSEnumerateSessions(IntPtr hServer, [MarshalAs(UnmanagedType.U4)] Int32 Reserved,
            [MarshalAs(UnmanagedType.U4)] Int32 Version, ref IntPtr ppSessionInfo, [MarshalAs(UnmanagedType.U4)] ref Int32 pCount);

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

        [StructLayout(LayoutKind.Sequential)]
        public struct WTS_CLIENT_ADDRESS
        {
            public uint AddressFamily;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 20)]
            public byte[] Address;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct WTS_CLIENT_DISPLAY
        {
            public uint HorizontalResolution;
            public uint VerticalResolution;
            public uint ColorDepth;
        }

        public enum WTS_CONNECTSTATE_CLASS
        {
            Active,
            Connected,
            ConnectQuery,
            Shadow,
            Disconnected,
            Idle,
            Listen,
            Reset,
            Down,
            Init
        }

        public enum WTS_INFO_CLASS
        {
            InitialProgram = 0,
            ApplicationName = 1,
            WorkingDirectory = 2,
            OEMId = 3,
            SessionId = 4,
            UserName = 5,
            WinStationName = 6,
            DomainName = 7,
            ConnectState = 8,
            ClientBuildNumber = 9,
            ClientName = 10,
            ClientDirectory = 11,
            ClientProductId = 12,
            ClientHardwareId = 13,
            ClientAddress = 14,
            ClientDisplay = 15,
            ClientProtocolType = 16
        }

        private static IntPtr OpenServer(string Name)
        {
            IntPtr server = WTSOpenServer(Name);
            return server;
        }

        private static void CloseServer(IntPtr ServerHandle)
        {
            WTSCloseServer(ServerHandle);
        }

        public static List<TerminalSessionData> ListSessions(string ServerName)
        {
            IntPtr server = IntPtr.Zero;
            List<TerminalSessionData> ret = new List<TerminalSessionData>();
            server = OpenServer(ServerName);

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

                        ret.Add(new TerminalSessionData(si.SessionID, si.State, si.pWinStationName));
                    }

                    WTSFreeMemory(ppSessionInfo);
                }
            }
            finally
            {
                CloseServer(server);
            }

            return ret;
        }

        /// <summary>
        /// Get session info. only id is filled for performance purpose
        /// </summary>
        /// <param name="ServerName"></param>
        /// <param name="SessionId"></param>
        /// <returns></returns>
        public static TerminalSessionInfo GetSessionInfo(string ServerName, int SessionId)
        {
            IntPtr server = IntPtr.Zero;
            server = OpenServer(ServerName);
            System.IntPtr buffer = IntPtr.Zero;
            uint bytesReturned;
            TerminalSessionInfo data = new TerminalSessionInfo();

            try
            {
                //bool worked = WTSQuerySessionInformation(server, SessionId,
                //    WTS_INFO_CLASS.ApplicationName, out buffer, out bytesReturned);

                //if (!worked)
                //    return data;

                //string strData = Marshal.PtrToStringAnsi(buffer);
                //data.ApplicationName = strData;

                //worked = WTSQuerySessionInformation(server, SessionId,
                //    WTS_INFO_CLASS.ClientAddress, out buffer, out bytesReturned);

                //if (!worked)
                //    return data;

                //WTS_CLIENT_ADDRESS si = (WTS_CLIENT_ADDRESS)Marshal.PtrToStructure((System.IntPtr)buffer, typeof(WTS_CLIENT_ADDRESS));
                //data.ClientAddress = si;

                //worked = WTSQuerySessionInformation(server, SessionId,
                //    WTS_INFO_CLASS.ClientBuildNumber, out buffer, out bytesReturned);

                //if (!worked)
                //    return data;

                //int lData = Marshal.ReadInt32(buffer);
                //data.ClientBuildNumber = lData;

                //worked = WTSQuerySessionInformation(server, SessionId,
                //    WTS_INFO_CLASS.ClientDirectory, out buffer, out bytesReturned);

                //if (!worked)
                //    return data;

                //strData = Marshal.PtrToStringAnsi(buffer);
                //data.ClientDirectory = strData;

                //worked = WTSQuerySessionInformation(server, SessionId,
                //    WTS_INFO_CLASS.ClientDisplay, out buffer, out bytesReturned);

                //if (!worked)
                //    return data;

                //WTS_CLIENT_DISPLAY cd = (WTS_CLIENT_DISPLAY)Marshal.PtrToStructure((System.IntPtr)buffer, typeof(WTS_CLIENT_DISPLAY));
                //data.ClientDisplay = cd;

                //worked = WTSQuerySessionInformation(server, SessionId,
                //    WTS_INFO_CLASS.ClientHardwareId, out buffer, out bytesReturned);

                //if (!worked)
                //    return data;

                //lData = Marshal.ReadInt32(buffer);
                //data.ClientHardwareId = lData;

                //worked = WTSQuerySessionInformation(server, SessionId,
                //    WTS_INFO_CLASS.ClientName, out buffer, out bytesReturned);
                //strData = Marshal.PtrToStringAnsi(buffer);
                //data.ClientName = strData;

                //worked = WTSQuerySessionInformation(server, SessionId,
                //    WTS_INFO_CLASS.ClientProductId, out buffer, out bytesReturned);
                //Int16 intData = Marshal.ReadInt16(buffer);
                //data.ClientProductId = intData;

                //worked = WTSQuerySessionInformation(server, SessionId,
                //    WTS_INFO_CLASS.ClientProtocolType, out buffer, out bytesReturned);
                //intData = Marshal.ReadInt16(buffer);
                //data.ClientProtocolType = intData;

                //worked = WTSQuerySessionInformation(server, SessionId,
                //    WTS_INFO_CLASS.ConnectState, out buffer, out bytesReturned);
                //lData = Marshal.ReadInt32(buffer);
                //data.ConnectState = (WTS_CONNECTSTATE_CLASS)Enum.ToObject(typeof(WTS_CONNECTSTATE_CLASS), lData);

                //worked = WTSQuerySessionInformation(server, SessionId,
                //    WTS_INFO_CLASS.DomainName, out buffer, out bytesReturned);
                //strData = Marshal.PtrToStringAnsi(buffer);
                //data.DomainName = strData;

                //worked = WTSQuerySessionInformation(server, SessionId,
                //    WTS_INFO_CLASS.InitialProgram, out buffer, out bytesReturned);
                //strData = Marshal.PtrToStringAnsi(buffer);
                //data.InitialProgram = strData;

                //worked = WTSQuerySessionInformation(server, SessionId,
                //    WTS_INFO_CLASS.OEMId, out buffer, out bytesReturned);
                //strData = Marshal.PtrToStringAnsi(buffer);
                //data.OEMId = strData;

                bool worked = WTSQuerySessionInformation(server, SessionId,
                    WTS_INFO_CLASS.SessionId, out buffer, out bytesReturned);
                int lData = Marshal.ReadInt32(buffer);
                data.SessionId = lData;

                //worked = WTSQuerySessionInformation(server, SessionId,
                //    WTS_INFO_CLASS.UserName, out buffer, out bytesReturned);
                //strData = Marshal.PtrToStringAnsi(buffer);
                //data.UserName = strData;

                //worked = WTSQuerySessionInformation(server, SessionId,
                //    WTS_INFO_CLASS.WinStationName, out buffer, out bytesReturned);
                //strData = Marshal.PtrToStringAnsi(buffer);
                //data.WinStationName = strData;

                //worked = WTSQuerySessionInformation(server, SessionId,
                //    WTS_INFO_CLASS.WorkingDirectory, out buffer, out bytesReturned);
                //strData = Marshal.PtrToStringAnsi(buffer);
                //data.WorkingDirectory = strData;
            }
            finally
            {
                WTSFreeMemory(buffer);
                buffer = IntPtr.Zero;
                CloseServer(server);
            }

            return data;
        }

        public static void GetNewSessionAbortDelay()
        {
            _timeoutCounter = _timeoutLimit;
        }

        static int _timeoutCounter = 0;
        static int _timeoutLimit = 0;
        internal async static Task<TerminalSessionData> GetNewSession(string hostname, List<TerminalSessionData> lastSessionList, int timeoutLimit)
        {
            TerminalSessionData newSession = null;
            _timeoutLimit = timeoutLimit;

            while (lastSessionList.Count == CountSessions(hostname)) //when number of active session changes
            {
                await Task.Delay(1000); //wait 1sec
                if (_timeoutCounter >= timeoutLimit)
                {
                    break;
                }
                _timeoutCounter++;
            }

            List<TerminalSessionData> sessionList = TermServicesManager.ListSessions(hostname);
            //IEnumerable<TerminalSessionData> newSessions = sessionList.Except(lastSessionList);

            //if (newSessions.Count() > 0)
            //{
            //    if (newSessions.Count() != 1)
            //    {
            //        _log.Warn(string.Format("{0} rdp sessions detected. selecting the first one.", newSessions.Count()));
            //    }
            //    newSession = newSessions.First();
            //}
            //else
            //{
                newSession = getSessionChanged(sessionList, lastSessionList);
            //}

            return newSession;
        }

        private static TerminalSessionData getSessionChanged(List<TerminalSessionData> sessionList, List<TerminalSessionData> lastSessionList)
        {
            foreach (var session in sessionList)
            {
                if (session.ConnectionState == WTS_CONNECTSTATE_CLASS.Active)
                {
                    foreach (var lastSession in lastSessionList)
                    {
                        if (session.SessionId == lastSession.SessionId && lastSession.ConnectionState != session.ConnectionState)
                            //note: we dont check if session wasnt active before because it could have been already active (on another computer!?)
                        {
                            return session;
                        }
                    }
                }
            }

            return null;
        }

        public static int CountSessions(String ServerName)
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
                        if (si.State == WTS_CONNECTSTATE_CLASS.Active)
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
    }

    public class TerminalSessionData
    {
        public int SessionId;
        public TermServicesManager.WTS_CONNECTSTATE_CLASS ConnectionState;
        public string StationName;

        public TerminalSessionData(int sessionId, TermServicesManager.WTS_CONNECTSTATE_CLASS connState, string stationName)
        {
            SessionId = sessionId;
            ConnectionState = connState;
            StationName = stationName;
        }

        public override string ToString()
        {
            return String.Format("{0} {1} {2}", SessionId, ConnectionState, StationName);
        }
    }

    public class TerminalSessionInfo
    {
        public string InitialProgram;
        public string ApplicationName;
        public string WorkingDirectory;
        public string OEMId;
        public int SessionId;
        public string UserName;
        public string WinStationName;
        public string DomainName;
        public TermServicesManager.WTS_CONNECTSTATE_CLASS ConnectState;
        public int ClientBuildNumber;
        public string ClientName;
        public string ClientDirectory;
        public int ClientProductId;
        public int ClientHardwareId;
        public TermServicesManager.WTS_CLIENT_ADDRESS ClientAddress;
        public TermServicesManager.WTS_CLIENT_DISPLAY ClientDisplay;
        public int ClientProtocolType;
    }
}