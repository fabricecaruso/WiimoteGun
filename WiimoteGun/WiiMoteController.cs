using WiimoteLib;
using WiimoteLib.Events;
using WiimoteLib.DataTypes;
using System.Threading;
using System;
using System.Linq;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace WiimoteGun
{
    class WiiMoteController : IDisposable
    {
        private WiiMoteMode _mode = WiiMoteMode.Mouse;
        private ScreenPositionCalculator _calculator;
        private MouseEventSimulator _inputSimulator;
        private IVirtualJoy _joy;
        private int ticks = -1;
        private ButtonState _lastState;
        private object _lock = new object();
        private WiimoteHiddenWnd _hiddenWnd;

        internal WiiMoteController()
        {
            ScreenIndex = Options.Instance.MonitorId;

            _joy = new VirtualSendKey();
            _inputSimulator = new MouseEventSimulator();
            _calculator = new ScreenPositionCalculator(ScreenIndex);
            
            WiimoteManager.DolphinBarMode = Options.Instance.DetectDolphinbar;
            WiimoteManager.BluetoothMode = Options.Instance.DetectBlueTooth;
            WiimoteManager.AutoConnect = true;

            WiimoteManager.Connected += OnWiimoteConnected;
            WiimoteManager.Disconnected += OnWiimoteDisconnected;

            WiimoteManager.ManagerException += OnWiimoteManagerException;
            WiimoteManager.WiimoteException += OnWiimoteException;

            WiimoteManager.StartDiscovery();

            _watchDolphinThread = new Thread(CheckDolphin);
            _watchDolphinThread.Start();
        }

        public void Dispose()
        {
            if (_watchDolphinThread != null)
            {
                _watchDolphinfinishEvent.Set();
                _watchDolphinThread.Join();
                _watchDolphinThread = null;
            }
        }

        public int ScreenIndex { get; set; }

        private void OnWiimoteException(object sender, WiimoteExceptionEventArgs e)
        {
            SimpleLogger.Instance.Error("Wiimote Exception " + e.ToString());
        }

        private void OnWiimoteManagerException(object sender, Exception e)
        {
            SimpleLogger.Instance.Error("WiimoteManager Exception " + e.ToString());
        }

        private void OnWiimoteDisconnected(object sender, WiimoteDisconnectedEventArgs e)
        {
            SimpleLogger.Instance.Info("Wiimote disconnected");

            if (_hiddenWnd != null)
            {
                var wnd = _hiddenWnd;
                _hiddenWnd = null;

                Program.PostToUIThread(() =>
                {
                    wnd.Dispose();
                });
            }

            Program.SetConnectedState(false);
            e.Wiimote.StateChanged -= OnWiiMoteStateChanged;

            WiimoteManager.StartDiscovery();
        }




        private void OnWiimoteConnected(object sender, WiimoteEventArgs e)
        {            
            e.Wiimote.SetReportType(ReportType.ButtonsAccelIR10Ext6, (IRSensitivity) Options.Instance.IRSensitivity, true);
            e.Wiimote.SetLEDs(true, false, false, false);
            e.Wiimote.StateChanged += OnWiiMoteStateChanged;

            Program.SetConnectedState(true);
            Program.Notify("Wiimote connected");

            SimpleLogger.Instance.Info("Wiimote connected");

            if (_hiddenWnd == null)
            {
                _hiddenWnd = new WiimoteHiddenWnd();

                Program.PostToUIThread(() =>
                {
                    if (_hiddenWnd == null)
                        return;

                    _hiddenWnd.Create();
                    _hiddenWnd.SetMode(((int)_mode) + 1);
                });
            }

            Vibrate(e.Wiimote);
        }

        private static void Vibrate(Wiimote wm)
        {
            Program.PostToUIThread(() =>
            {
                var timer = new System.Windows.Forms.Timer();
                timer.Interval = 350;
                timer.Tick += (a, b) =>
                {
                    try { wm.SetRumble(false); }
                    catch { }

                    try { timer.Dispose(); }
                    catch { }
                };

                timer.Start();

                wm.SetRumble(true);
            });
        }

        private bool _lockUntilABreleased = false;

        private void OnWiiMoteStateChanged(object sender, WiimoteStateEventArgs e)
        {
            if (e.WiimoteState == null)
                return;

            lock (_lock)
            {
                ButtonState buttons = e.WiimoteState.Buttons;
                IRState ir = e.WiimoteState.IRState;

                if (_runningProcess != null)
                {
                    if (_runningProcess.HasExited)
                    {
                        if (_processLocking && _mode == WiiMoteMode.Mouse)
                        {
                            ThreadPool.QueueUserWorkItem(o =>
                            {
                                try { e.Wiimote.SetReportType(ReportType.ButtonsAccelIR10Ext6, IRSensitivity.Maximum, true); }
                                catch { }
                            }, null);
                        }

                        _processLocking = false;
                        _runningProcess = null;
                    }
                }

                if (_runningProcess != null && _processLocking)
                    return;
                    
                ManageCalibration(e.Wiimote, buttons, _lastState);

                if (_mode == WiiMoteMode.Mouse)
                {
                    bool wasCalibrating = _calculator.IsCalibrating;

                    var pos = _calculator.GetPosition(ir, buttons, _lastState);

                    if (wasCalibrating || _calculator.IsCalibrating)
                    {
                        _inputSimulator.ProcessMouseEvent(pos, false, false, false);
                        _lockUntilABreleased = true;
                    }
                    else
                    {
                        if (_lockUntilABreleased && !buttons.B && !buttons.A)
                            _lockUntilABreleased = false;

                        if (_lockUntilABreleased)
                            _inputSimulator.ProcessMouseEvent(pos, false, false, false);
                        else
                            _inputSimulator.ProcessMouseEvent(pos, buttons.B, buttons.A, buttons.One);
                    }

                    if (_joy != null && _joy.IsEnabled && !_calculator.IsCalibrating)
                    {
                        if (buttons.Up != _lastState.Up)
                            _joy.SetAxis(false, buttons.Up ? 1 : 0);

                        if (buttons.Down != _lastState.Down)
                            _joy.SetAxis(false, buttons.Down ? -1 : 0);

                        if (buttons.Left != _lastState.Left)
                            _joy.SetAxis(true, buttons.Left ? 1 : 0);

                        if (buttons.Right != _lastState.Right)
                            _joy.SetAxis(true, buttons.Right ? -1 : 0);

                        if (buttons.Minus != _lastState.Minus)
                            _joy.SetButton(5, buttons.Minus);

                        if (buttons.Plus != _lastState.Plus)
                            _joy.SetButton(6, buttons.Plus);

                        //     if (buttons.One != _lastState.One)
                        //      _joy.SetButton(1, buttons.One);

                        if (buttons.Two != _lastState.Two)
                            _joy.SetButton(2, buttons.Two);

                        _joy.CommitChanges();
                    }
                }
                else if (_mode == WiiMoteMode.Keyboardpad)
                {
                    if (_joy != null && _joy.IsEnabled && !_calculator.IsCalibrating)
                    {
                        if (buttons.Up != _lastState.Up)
                            _joy.SetAxis(true, buttons.Up ? 1 : 0);

                        if (buttons.Down != _lastState.Down)
                            _joy.SetAxis(true, buttons.Down ? -1 : 0);

                        if (buttons.Left != _lastState.Left)
                            _joy.SetAxis(false, buttons.Left ? -1 : 0);

                        if (buttons.Right != _lastState.Right)
                            _joy.SetAxis(false, buttons.Right ? 1 : 0);

                        if (buttons.A != _lastState.A)
                            _joy.SetButton(3, buttons.A);

                        if (buttons.B != _lastState.B)
                            _joy.SetButton(4, buttons.B);

                        if (buttons.One != _lastState.One)
                            _joy.SetButton(2, buttons.One);

                        if (buttons.Two != _lastState.Two)
                            _joy.SetButton(1, buttons.Two);

                        if (buttons.Minus != _lastState.Minus)
                            _joy.SetButton(5, buttons.Minus);

                        if (buttons.Plus != _lastState.Plus)
                            _joy.SetButton(6, buttons.Plus);

                        //      if (buttons.Home != _lastState.Home)
                        //          _joy.SetButton(7, buttons.Home);

                        _joy.CommitChanges();
                    }
                }

                _lastState = e.WiimoteState.Buttons;
            }
        }

        private void SwitchMode(Wiimote wiimote)
        {
            int mode = (int) _mode;
            mode++;

            if (mode > (int)WiiMoteMode.Disabled)
                mode = 0;

            _mode = (WiiMoteMode)mode;
            
            if (_hiddenWnd != null)
                _hiddenWnd.SetMode(((int)_mode) + 1);

            if (_mode == WiiMoteMode.Mouse)
            {
                ThreadPool.QueueUserWorkItem(o =>
                {
                    try { wiimote.SetReportType(ReportType.ButtonsAccelIR10Ext6, IRSensitivity.Maximum, true); }
                    catch { }
                }, null);
            }

            SimpleLogger.Instance.Info("Wiimote set to mode : " + _mode.ToString());

            if (_mode == WiiMoteMode.Disabled)
                Program.Notify("WiimoteGun disabled");
            else 
                Program.Notify("WiimoteGun "+ _mode.ToString() + " activated");
        }

        private void ManageCalibration(Wiimote wiimote, ButtonState buttons, WiimoteLib.DataTypes.ButtonState lastState)
        {
            if (_calculator.IsCalibrating)
                return;

            if (lastState.Home != buttons.Home)
            {
                if (buttons.Home && ticks < 0)
                    ticks = Environment.TickCount;
                else if (!buttons.Home && ticks > 0)
                {
                    SwitchMode(wiimote);
                    ticks = -1;
                }
            }
            else if (buttons.Home && ticks > 0 && Environment.TickCount - ticks >= 1000)
            {
                ticks = -1;

                if (_mode == WiiMoteMode.Mouse)
                    _calculator.Calibrate();
            }                        
        }

        #region Concurrent processes exclusions

        private Thread _watchDolphinThread;
        private AutoResetEvent _watchDolphinfinishEvent = new AutoResetEvent(false);

        private Process _runningProcess;
        private bool _processLocking;

        private Process GetDolphinProcess(out bool locks)
        {
            locks = false;

            var list = Process.GetProcesses().ToList();

            Process px = list.FirstOrDefault(p => "dolphin".Equals(p.ProcessName, StringComparison.InvariantCultureIgnoreCase));
            if (px != null)
            {
                locks = true;
                return px;
            }

            px = list.FirstOrDefault(p => "retroarch".Equals(p.ProcessName, StringComparison.InvariantCultureIgnoreCase));
            if (px != null)
            {
                var commandLine = px.GetProcessCommandline();
                if (!string.IsNullOrEmpty(commandLine))
                    locks = commandLine.Contains("dolphin_libretro.dll");

                return px;
            }

            px = list.FirstOrDefault(p => "cemu".Equals(p.ProcessName, StringComparison.InvariantCultureIgnoreCase));
            if (px != null)
            {
                locks = true;
                return px;
            }

            return null;
        }

        private void CheckDolphin()
        {
            while (true)
            {
                if (_watchDolphinfinishEvent.WaitOne())
                    break;

                if (_runningProcess == null)
                    _runningProcess = GetDolphinProcess(out _processLocking);

                Thread.Sleep(100);
            }
        }

        #endregion
    }

    enum WiiMoteMode
    {
        Mouse = 0,
        Keyboardpad = 1,
        Disabled = 2
    }

}
