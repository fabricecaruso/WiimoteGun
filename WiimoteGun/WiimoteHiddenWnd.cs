using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using WiimoteGun.Common.Win32;

namespace WiimoteGun
{
    class WiimoteHiddenWnd : NativeWindow, IDisposable
    {
        public WiimoteHiddenWnd()
        {
           
        }

        public void Create()
        {
            IntPtr hInstance = Marshal.GetHINSTANCE(typeof(WiimoteHiddenWnd).Module);

            if (wc == null)
            {
                m_wnd_proc_delegate = CustomWndProc;

                wc = new WNDCLASS();
                wc.lpszClassName = "WiimoteGun";
                wc.lpfnWndProc = Marshal.GetFunctionPointerForDelegate(m_wnd_proc_delegate);
                wc.hbrBackground = IntPtr.Zero;
                wc.hCursor = IntPtr.Zero;
                wc.hIcon = IntPtr.Zero;
                wc.hInstance = hInstance;
                User32.RegisterClass(wc);
            }

            RECT rectPos = new RECT();

            IntPtr hWnd = User32.CreateWindowEx(
                (WS_EX)0,
                wc.lpszClassName, null,
                WS.POPUP,
                rectPos.left, rectPos.top,
                rectPos.Width, rectPos.Height,
                IntPtr.Zero,
                IntPtr.Zero,
                hInstance,
                IntPtr.Zero);

            User32.ShowWindow(hWnd, SW.HIDE);

            AssignHandle(hWnd);
        }

        public void Dispose()
        {            
            DestroyHandle();
        }

        public void SetMode(int value)
        {
            User32.SetProp(Handle, "mode", (IntPtr)value);
        }

        private static IntPtr CustomWndProc(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam)
        {
            return User32.DefWindowProcW(hWnd, msg, wParam, lParam);
        }

        private static WndProc m_wnd_proc_delegate;
        private static WNDCLASS wc;
    }


}
