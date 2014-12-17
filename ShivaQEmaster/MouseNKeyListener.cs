using MouseKeyboardActivityMonitor;
using MouseKeyboardActivityMonitor.WinApi;
using ShivaQEcommon.Eventdata;
using System;
using System.Windows.Forms;

namespace ShivaQEmaster
{
    class MouseNKeyListener
    {
        private MouseHookListener m_mouseListener;
        private KeyboardHookListener k_keyboardListener;

        public event EventHandler<MouseNKeyEventArgs> KeyboadDown;
        public event EventHandler<MouseNKeyEventArgs> MouseClick;
        public event EventHandler<MouseNKeyEventArgs> MouseMove;

        // Subroutine for activating the hook
        public void ActivateKeyboard()
        {
            // Note: for an application hook, use the AppHooker class instead
            k_keyboardListener = new KeyboardHookListener(new GlobalHooker());

            // The listener is not enabled by default
            k_keyboardListener.Enabled = true;

            k_keyboardListener.KeyDown += k_keyboardListener_KeyboardDown;
        }

        private void k_keyboardListener_KeyboardDown(object sender, System.Windows.Forms.KeyEventArgs e)
        {
            //get char of key. if key is a special key such as shift, because it doesn't have any char, we set it's name 
            KeyEventArgsExt eExt = e as KeyEventArgsExt;
            string key = (eExt.UnicodeChar == 0) ? e.KeyData.ToString() : eExt.UnicodeChar.ToString();
            
            // override UnicodeChar for keys: backspace, space, enter, tab (ommiting capslock on purpose)
            if (e.KeyData == Keys.Return || e.KeyData == Keys.Space || e.KeyData == Keys.Back || e.KeyData == Keys.Tab)
            {
                key = e.KeyData.ToString();
            }

            // workaround. win+key not working
            int keyCode = key.Length == 1 && char.IsLetter(key[0]) ? (int)char.ToUpper(key[0]) : (int)e.KeyCode;

            string keyData = e.KeyData.ToString();
            if (keyData.Contains(","))
            {
                string keyFromData = keyData.Split()[0];
                if (keyFromData.Length == 2)
                    keyCode = (int)char.ToUpper(keyFromData[0]);
            }
            
            Console.WriteLine(string.Format("KeyDown: \t{0}; \t data: \t{1}\t code: {2}\tvalue: {3}", key, e.KeyData, keyCode, e.KeyValue));

            //raise key event
            OnEvent(KeyboadDown, new MouseNKeyEventArgs() {
                key = key,
                keyCode = keyCode,
                keyData = e.KeyData.ToString(),
                timestamp = DateTime.Now.Ticks
            });
        }

        protected virtual void OnEvent(EventHandler<MouseNKeyEventArgs> handler, MouseNKeyEventArgs e)
        {
            if (handler != null)
            {
                handler(this, e);
            }
        }

        // Subroutine for activating the hook
        public void ActivateMouse(bool mouseMovement)
        {
            // Note: for an application hook, use the AppHooker class instead
            m_mouseListener = new MouseHookListener(new GlobalHooker());

            // The listener is not enabled by default
            m_mouseListener.Enabled = true;

            //event on mouse down 
            m_mouseListener.MouseDownExt += m_mouseListener_MouseDownExt;

            //event on mouse up 
            m_mouseListener.MouseUp += m_mouseListener_MouseUp;

            //event on mouse move
            if (mouseMovement)
            {
                m_mouseListener.MouseMoveExt += m_mouseListener_MouseMoveExt;
            }

            m_mouseListener.MouseWheel += m_mouseListener_MouseWheel;
        }

        void m_mouseListener_MouseWheel(object sender, MouseEventArgs e)
        {
            int weelClicks = e.Clicks == 0 ? 1 : e.Clicks;
            int weelDirection = e.Delta >= 0 ? 1 : -1;

            OnEvent(MouseClick, new MouseNKeyEventArgs()
            {
                key = "Weel",
                keyCode = weelClicks * weelDirection,
                position_x = e.X,
                position_y = e.Y
            });
        }

        void m_mouseListener_MouseUp(object sender, MouseEventArgs e)
        {
            OnEvent(MouseClick, new MouseNKeyEventArgs()
            {
                key = e.Button.ToString(),
                keyCode = getMouseVirtualCode(e.Button),
                keyData = "up",
                position_x = e.X,
                position_y = e.Y
            });
        }

        void m_mouseListener_MouseMoveExt(object sender, MouseEventExtArgs e)
        {
            OnEvent(MouseMove,new MouseNKeyEventArgs()
            {
                position_x = e.Location.X,
                position_y = e.Location.Y
            });
        }

        private void m_mouseListener_MouseDownExt(object sender, MouseEventExtArgs e)
        {
            // log the mouse click
            //Debug.WriteLine(string.Format("MouseDown: \t{0}; \t Timestamp: \t{1}; \t Position {2},{3}",
            //    e.Button, e.Timestamp, e.Location.X, e.Location.Y));

            OnEvent(MouseClick, new MouseNKeyEventArgs()
            {
                key = e.Button.ToString(),
                keyCode = getMouseVirtualCode(e.Button),
                keyData = "down",
                timestamp = e.Timestamp,
                position_x = e.X,
                position_y = e.Y
            });

        }

        /// <summary>
        /// converts mouses keyCode from
        /// mousebuttons http://msdn.microsoft.com/en-us/library/ms839531.aspx
        /// to
        /// virtualkey http://msdn.microsoft.com/en-us/library/windows/desktop/dd375731(v=vs.85).aspx
        /// </summary>
        /// <param name="mouseButton"></param>
        /// <returns></returns>
        private int getMouseVirtualCode(MouseButtons mouseButton)
        {
            int buttonCode;
            switch (mouseButton)
            {
                case MouseButtons.Left:
                    buttonCode = 0x01;
                    break;
                case MouseButtons.Right:
                    buttonCode = 0x02;
                    break;
                case MouseButtons.Middle:
                    buttonCode = 0x04;
                    break;
                case MouseButtons.XButton1:
                    buttonCode = 0x05;
                    break;
                case MouseButtons.XButton2:
                    buttonCode = 0x06;
                    break;
                default:
                    buttonCode = 0;
                    break;
            }
            return buttonCode;
        }

        private bool _isActive = false;
        public bool isActive { get { return _isActive; } }

        public void Active()
        {
            Active(true);
        }

        public void Active(bool mouseMovements)
        {
            ActivateMouse(mouseMovements);
            if (k_keyboardListener == null)
            {
                ActivateKeyboard();
            }
            _isActive = true;
        }

        public void DeactiveAll()
        {
            m_mouseListener.Dispose();
            // k_keyboardListener.Dispose(); --if we deactivate this, shortcut wont work

            _isActive = false;
        }

        internal void DeactiveMouseMove()
        {
            if (m_mouseListener != null)
            {
                //event on mouse move
                m_mouseListener.MouseMoveExt -= m_mouseListener_MouseMoveExt;
            }

        }

        internal void ActivateMouseMove()
        {
            if (m_mouseListener == null)
            {
                m_mouseListener = new MouseHookListener(new GlobalHooker());

                // The listener is not enabled by default
                m_mouseListener.Enabled = true;
            }

            //event on mouse move
            m_mouseListener.MouseMoveExt += m_mouseListener_MouseMoveExt;
        }
    }
}
