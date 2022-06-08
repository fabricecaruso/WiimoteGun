using System;
using System.Linq;
using System.Windows.Forms;
using WiimoteGun.Common.Win32;
using WiimoteLib.Geometry;

namespace WiimoteGun
{
    class CalibrateForm : Form
    {
        private Screen _screen;

        private static Point2F? mCenter;
        private static Point2F? mTopLeft;

        public CalibrateForm(int screenIndex)
        {
            SetStyle(ControlStyles.OptimizedDoubleBuffer, true);
            SetStyle(ControlStyles.AllPaintingInWmPaint, true);

            if (screenIndex > 0)
                _screen = Screen.AllScreens.Skip(screenIndex).FirstOrDefault();

            if (_screen == null)
                _screen = Screen.PrimaryScreen;

            var bounds = _screen.Bounds;

            Opacity = 0;
            BackColor = System.Drawing.Color.Black;
            FormBorderStyle = FormBorderStyle.None;
            AutoScaleMode = AutoScaleMode.Dpi;
            ShowInTaskbar = false;            
            ControlBox = false;
            MaximizeBox = false;
            MinimizeBox = false;
            Text = null;
            Width = bounds.Width;
            Height = bounds.Height;
            Location = new System.Drawing.Point(bounds.Left, bounds.Top);

            StartPosition = FormStartPosition.Manual;
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);

            User32.SetWindowPos(Handle, User32.HWND_TOPMOST, 0, 0, 0, 0, SWP.NOMOVE | SWP.NOSIZE);
            User32.SetForegroundWindow(Handle);
            User32.SetActiveWindow(Handle);

            Opacity = 0.8;
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == 0x0014) // WM_ERASEBKGND
            {
                m.Result = (IntPtr)1;
                return;
            }

            base.WndProc(ref m);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var gun = Properties.Resources.gun;

            if (mCenter.HasValue && !mTopLeft.HasValue)
                e.Graphics.DrawImage(gun, 2 - (gun.Width / 2), 2 - (gun.Height / 2));
            else
                e.Graphics.DrawImage(gun, (this.Width - gun.Width) / 2, (this.Height - gun.Height) / 2);

            var rect = this.ClientRectangle;
            rect.Height /= 2;

            using (var font = new System.Drawing.Font(System.Drawing.SystemFonts.MessageBoxFont.FontFamily.Name, 16))
            {
                TextRenderer.DrawText(e.Graphics, 
                    "Calibrating WiiMote\r\nFire on targets to calibrate", font,
                    rect, System.Drawing.Color.White, System.Drawing.Color.Transparent, 
                    TextFormatFlags.VerticalCenter | TextFormatFlags.HorizontalCenter);
            }
        }

        public void UpdateState(Point2F pos, Point2F? center, Point2F? topLeft)
        {
            if (mCenter == center && mTopLeft == topLeft)
                return;

            if (this.InvokeRequired)
            {
                this.Invoke(new Action<Point2F, Point2F?, Point2F?>(UpdateState), new object[] { pos, center, topLeft });
                return;
            }

            mCenter = center;
            mTopLeft = topLeft;
            Invalidate();
        }

        public bool IsCalibrated
        {
            get { return mCenter.HasValue && mTopLeft.HasValue; }
        }
    }

}
