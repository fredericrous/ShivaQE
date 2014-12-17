using ShivaQEcommon.Eventdata;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using WindowsInput;
using WindowsInput.Native;

namespace ShivaQEslave
{
    static class MouseNKeySimulator
    {
        private const int MOUSEEVENTF_ABSOLUTE = 0x8000;
        private const int MOUSEEVENTF_MIDDLEUP = 0x0040;

        [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        public static extern void mouse_event(uint dwFlags, int dx, int dy, int dwData, int dwExtraInfo);

        [DllImport("user32.dll")]
        static extern bool SetCursorPos(int X, int Y);

        static InputSimulator inputSimulator = new InputSimulator();

        #region keyboardLang

        const int KL_NAMELENGTH = 9;
        const uint KLF_ACTIVATE = 1;

        [DllImport("user32.dll")]
        public static extern long LoadKeyboardLayout(string pwszKLID, uint Flags);

        private const uint WM_INPUTLANGCHANGEREQUEST = 0x0050;

        [DllImport("user32.dll")]
        public static extern long GetKeyboardLayoutName(System.Text.StringBuilder pwszKLID);

        [DllImport("user32.dll")]
        private static extern bool PostMessage(IntPtr hhwnd, uint msg, IntPtr wparam, IntPtr lparam);

        [DllImport("user32.dll")]
        public static extern IntPtr GetForegroundWindow();

        #endregion


        public static void setMousePosition(MouseNKeyEventArgs mouseNkey)
        {
            //set mouse curstor to position X, Y
            SetCursorPos(mouseNkey.position_x, mouseNkey.position_y);
        }

        public static string getKeyboardLang()
        {
            System.Text.StringBuilder name = new System.Text.StringBuilder(KL_NAMELENGTH);
            GetKeyboardLayoutName(name);
            return name.ToString();
        }

        public static void setKeyboardLang(string code)
        {
            PostMessage(GetForegroundWindow(), WM_INPUTLANGCHANGEREQUEST, IntPtr.Zero, new IntPtr(LoadKeyboardLayout(code, KLF_ACTIVATE)));
        }

        public static void SimulateAction(MouseNKeyEventArgs mouseNkey)
        {
            //because mousepos is delivered by udp and this protocol does'nt garanty all paquets where delivered,
            //re-position mouse before click
            if (mouseNkey.keyCode == (int)VirtualKeyCode.RBUTTON || mouseNkey.keyCode == (int)VirtualKeyCode.LBUTTON
                || mouseNkey.key == "Middle" || mouseNkey.key == "Weel")
            {
                setMousePosition(mouseNkey);
            }

            if (mouseNkey.key == "Middle")
            {
                mouse_event(MOUSEEVENTF_ABSOLUTE | MOUSEEVENTF_MIDDLEUP, mouseNkey.position_x, mouseNkey.position_y, 0, 0);
               // inputSimulator.Mouse.XButtonClick((int)VirtualKeyCode.MBUTTON); //might be 0x40
                return ;
            }
            else if (mouseNkey.key == "Weel")
            {
                inputSimulator.Mouse.VerticalScroll(mouseNkey.keyCode);
                return;
            }

            switch (mouseNkey.keyCode)
            {
                case (int)VirtualKeyCode.LBUTTON:
                    if (mouseNkey.keyData == "down")
                    {
                        inputSimulator.Mouse.LeftButtonDown();
                    }
                    else
                    {
                        inputSimulator.Mouse.LeftButtonUp();
                    }
                    break;
                case (int)VirtualKeyCode.RBUTTON:
                    if (mouseNkey.keyData == "down")
                    {
                        inputSimulator.Mouse.RightButtonDown();
                    }
                    else
                    {
                        inputSimulator.Mouse.RightButtonUp();
                    }
                    break;
                default:
                    if (mouseNkey.keyData.Contains(",") || !mouseNkey.keyData.Equals(mouseNkey.key))
                    {
                        List<VirtualKeyCode> modifierKey = new List<VirtualKeyCode>();

                        if (mouseNkey.keyData.Contains("Control"))
                        {
                            modifierKey.Add(VirtualKeyCode.CONTROL);
                        }
                        if (mouseNkey.keyData.Contains("Alt"))
                        {
                            modifierKey.Add(VirtualKeyCode.MENU);
                        }
                        if (mouseNkey.keyData.Contains("Shift"))
                        {
                            modifierKey.Add(VirtualKeyCode.SHIFT);
                        }
                        if (mouseNkey.keyData.Contains("Sleep")) //not sure
                        {
                            modifierKey.Add(VirtualKeyCode.RWIN);
                        }
                        if (mouseNkey.keyData.Contains("LWin"))
                        {
                            modifierKey.Add(VirtualKeyCode.LWIN);
                        }
                        if (mouseNkey.keyData.Contains("RWin"))
                        {
                            modifierKey.Add(VirtualKeyCode.RWIN);
                        }
                        inputSimulator.Keyboard.ModifiedKeyStroke(modifierKey, (VirtualKeyCode)mouseNkey.keyCode);
                    }
                    else
                    {
                        inputSimulator.Keyboard.KeyPress((VirtualKeyCode)mouseNkey.keyCode);
                    }
                    break;
            }
        }
    }
}
