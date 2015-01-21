using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShivaQEcommon.Eventdata
{
    class HookEvent
    {
        public enum WndprocCalls
        {
            WM_CAPTURECHANGED = 0x0215,
            WM_CHANGEUISTATE = 0x0127,
            WM_WINDOWPOSCHANGED = 0x0047,
            WM_UPDATEUISTATE = 0x0128,
            WM_SHOWWINDOW = 0x0018
        };

        private const int WM_USER = 0x0400;
        public enum MsgCalls
        {
            RB_BEGINDRAG = WM_USER + 24,
            WM_PAINT = 0x000F
        }

        public enum ShellCalls
        {
            HSHELL_WINDOWCREATED = 1, 
            HSHELL_WINDOWDESTROYED =  2,
            HSHELL_REDRAW = 6
        };

        public enum CbtCalls
        {
            CBTActivate,
            CBTCreateWindow,
            CBTDestroyWindow,
            CBTMinMax,
            CBTSetFocus,
            CBTMoveSize
        };

    }
}
