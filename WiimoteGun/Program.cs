using System;
using System.Threading;
using System.Windows.Forms;

/// <summary>
/// Thanks to
/// https://github.com/Jeern/WiiCursor
/// https://github.com/trigger-segfault/WiimoteLib.Net
/// </summary>

namespace WiimoteGun
{
    static class Program
    {
        static NotifyIcon _trayIcon;
        static WiiMoteController _wiiMoteController;
        static ApplicationContext _appContext;
        static Mutex _singleInstanceMutex;

        [STAThread]
        static void Main(string[] args)
        {
            bool createdNew;
            _singleInstanceMutex = new Mutex(true, "WiimoteGun {71916996-F0A0-434C-88CA-41A62B4F9E17}", out createdNew);
            if (!createdNew)
                return;

            SimpleLogger.Instance.Info("---------------------------------------------------------------");
            SimpleLogger.Instance.Info("WiimoteGun startup");

            AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            _appContext = new ApplicationContext();
            _wiiMoteController = new WiiMoteController();
            _synchronizationContext = new WindowsFormsSynchronizationContext();

            InitializeTrayIcon();
           
            Application.Run(_appContext);
        }

        private static void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            var ex = e.ExceptionObject as Exception;
            if (ex != null)
            {
                SimpleLogger.Instance.Error("Unhandled Exception : " + ex.ToString());
                MessageBox.Show("Unhandled Exception : " + ex.ToString());
            }
        }

        private static void InitializeTrayIcon()
        {
            _trayIcon = new NotifyIcon();
            _trayIcon.Icon = Properties.Resources.gray;
            _trayIcon.Visible = true;
            _trayIcon.Text = "Wiimote Gun";

            var menuItems = new MenuItem[]
            {
                new MenuItem("&Options", OnShowOptions),
                new MenuItem("&About", OnShowAbout),
                new MenuItem("-"),
                new MenuItem("&Exit", OnExitClicked)
            };

            _trayIcon.ContextMenu = new ContextMenu(menuItems);
        }

        public static void SetConnectedState(bool connected)
        {
            PostToUIThread(() =>
            {
                _trayIcon.Icon = connected ? Properties.Resources.green : Properties.Resources.gray;
            });
        }

        private static void OnShowAbout(object sender, EventArgs e)
        {
            using (var frm = new AboutBox())
                frm.ShowDialog();
        }

        private static void OnShowOptions(object sender, EventArgs e)
        {
            using (var frm = new OptionsForm())
                frm.ShowDialog();
        }
         
        private static void OnExitClicked(object sender, EventArgs e)
        {
            if (_trayIcon != null)
            {
                _trayIcon.Visible = false;
                _trayIcon.Dispose();
                _trayIcon = null;
            }

            if (_wiiMoteController != null)
            {
                _wiiMoteController.Dispose();
                _wiiMoteController = null;
            }

            Application.Exit();
        }

        private static SynchronizationContext _synchronizationContext;

        public static SynchronizationContext SynchronizationContext
        {
            get { return _synchronizationContext; }
        }


        public static void Notify(string text)
        {
            if (!Options.Instance.ShowNotifications)
                return;

            PostToUIThread(() =>
            {
                var frm = new NotifyForm();
                frm.UpdateState(text);
                frm.Show();
            });

//            m_TrayIcon.ShowBalloonTip(3000, "WiiMote", text, System.Windows.Forms.ToolTipIcon.Info);
        }

        public static void PostToUIThread(System.Action a)
        {
            try
            {
                if (_synchronizationContext == null)
                    return;

                _synchronizationContext.Post(state =>
                {
                    try
                    {
                        a();
                    }
                    catch (Exception e)
                    {

                    }
                }, null);

            }
            catch (Exception e)
            {

            }
        }
    }
}
