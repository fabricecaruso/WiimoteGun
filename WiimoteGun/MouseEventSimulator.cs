using System;
using System.Runtime.InteropServices;
using WiimoteGun.Common.Win32;
using WiimoteLib.Geometry;

namespace WiimoteGun
{
    /// <summary>
    /// Input simulator can simulate Mouse moves & clicks + keypresses
    /// </summary>
    public class MouseEventSimulator
    {
        private static bool _leftPressed = false;
        private static bool _rightPressed = false;
        private static bool _middlePressed = false;

        public void ProcessMouseEvent(System.Drawing.Point? point, bool leftDown, bool rightDown, bool middleDown)
        {
            MOUSEEVENTF flags = 0;

            if (leftDown && !_leftPressed)
            {
                _leftPressed = true;
                flags = flags | MOUSEEVENTF.LEFTDOWN;
            }
            else if (!leftDown && _leftPressed)
            {
                _leftPressed = false;
                flags = flags | MOUSEEVENTF.LEFTUP;
            }

            if (rightDown && !_rightPressed)
            {
                _rightPressed = true;
                flags = flags | MOUSEEVENTF.RIGHTDOWN;
            }
            else if (!rightDown && _rightPressed)
            {
                _rightPressed = false;
                flags = flags | MOUSEEVENTF.RIGHTUP;
            }

            if (middleDown && !_middlePressed)
            {
                _middlePressed = true;
                flags = flags | MOUSEEVENTF.MIDDLEDOWN;
            }
            else if (!middleDown && _middlePressed)
            {
                _middlePressed = false;
                flags = flags | MOUSEEVENTF.MIDDLEUP;
            }                       

            if (point.HasValue)
                flags |= MOUSEEVENTF.ABSOLUTE;

            if (flags == 0)
                return;

            if (point.HasValue)
            {               
                User32.SetCursorPos(point.Value.X, point.Value.Y);
                MouseEvent(flags, point.Value.X, point.Value.X);
            }
            else
                MouseEvent(flags, 0, 0);
        }

        private void MouseEvent(MOUSEEVENTF flags, int x, int y)
        {
            User32.mouse_event(flags, x, y, 0, 0);
        }      
    }

}
