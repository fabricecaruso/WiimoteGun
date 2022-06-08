/*
using System;
using System.Diagnostics;
using vJoyInterfaceWrap;

namespace WiiCursor
{
    class VirtualVJoy : IDisposable, IVirtualJoy
    {

        // https://github.com/njz3/vJoy
        vJoy joystick;
        uint id = 1;
        vJoy.JoystickState iReport;
        long _minX = 0;
        long _maxX = 0;

        // enable(GetvJoyVersion());

        public bool IsEnabled { get { return joystick != null; } }

        public VirtualVJoy()
        {
            // vJoyConfig enable off

            joystick = new vJoy();
            iReport = new vJoy.JoystickState();
            iReport.bDevice = (byte)id;

            if (!joystick.vJoyEnabled())
            {
                Debug.WriteLine("vJoy driver not enabled: Failed Getting vJoy attributes.\n");
                joystick = null;
                return;
            }

            Debug.WriteLine("Vendor: {0}\nProduct :{1}\nVersion Number:{2}", joystick.GetvJoyManufacturerString(), joystick.GetvJoyProductString(), joystick.GetvJoySerialNumberString());

            // Get the state of the requested device
            VjdStat status = joystick.GetVJDStatus(id);
            switch (status)
            {
                case VjdStat.VJD_STAT_OWN:
                    Debug.WriteLine("vJoy Device {0} is already owned by this feeder", id);
                    break;
                case VjdStat.VJD_STAT_FREE:
                    Debug.WriteLine("vJoy Device {0} is free", id);
                    break;
                case VjdStat.VJD_STAT_BUSY:
                    Debug.WriteLine("vJoy Device {0} is already owned by another feeder\nCannot continue", id);
                    joystick = null;
                    return;
                case VjdStat.VJD_STAT_MISS:
                    Debug.WriteLine("vJoy Device {0} is not installed or disabled\nCannot continue", id);
                    joystick = null;
                    return;
                default:
                    Debug.WriteLine("vJoy Device {0} general error\nCannot continue", id);
                    joystick = null;
                    return;
            };

            // Test if DLL matches the driver
            UInt32 DllVer = 0, DrvVer = 0;
            bool match = joystick.DriverMatch(ref DllVer, ref DrvVer);
            if (match)
                Console.WriteLine("Version of Driver Matches DLL Version ({0:X})", DllVer);
            else
                Console.WriteLine("Version of Driver ({0:X}) does NOT match DLL Version ({1:X})", DrvVer, DllVer);

            // Get the number of buttons and POV Hat switchessupported by this vJoy device
            int nButtons = joystick.GetVJDButtonNumber(id);
            int ContPovNumber = joystick.GetVJDContPovNumber(id);
            int DiscPovNumber = joystick.GetVJDDiscPovNumber(id);

            // Acquire the target
            if ((status == VjdStat.VJD_STAT_OWN) || ((status == VjdStat.VJD_STAT_FREE) && (!joystick.AcquireVJD(id))))
            {
                Debug.WriteLine("Failed to acquire vJoy device number {0}.", id);
                return;
            }
            else
                Debug.WriteLine("Acquired: vJoy device number {0}.", id);

            joystick.GetVJDAxisMin(id, HID_USAGES.HID_USAGE_X, ref _minX);
            joystick.GetVJDAxisMax(id, HID_USAGES.HID_USAGE_X, ref _maxX);

            joystick.GetPosition(id, ref iReport);

            SetAxis(true, 0);
            SetAxis(false, 0);

            joystick.ResetVJD(id);
        }

        public void SetButton(uint nButton, bool value)
        {
            if (joystick != null)
            {
                uint nBin = (uint)(1 << (int)(nButton - 1));

                uint btn = iReport.Buttons;

                if (value)
                    btn |= nBin;
                else
                    btn &= ~nBin;

                if (btn != iReport.Buttons)
                {
                    iReport.Buttons = btn;
                    dirty = true;
                }
            }
        }

        public void SetAxis(bool AxisX, int value)
        {
            if (joystick != null)
            {
                if (AxisX)
                {
                    if (value < 0)
                        iReport.AxisX = (int)_maxX;
                    else if (value > 0)
                        iReport.AxisX = (int)_minX;
                    else
                        iReport.AxisX = (int)(_minX + _maxX) / 2;
                }
                else
                {
                    if (value < 0)
                        iReport.AxisY = (int)_maxX;
                    else if (value > 0)
                        iReport.AxisY = (int)_minX;
                    else
                        iReport.AxisY = (int)(_minX + _maxX) / 2;
                }

                dirty = true;
            }
        }

        public void CommitChanges()
        {
            if (!dirty)
                return;

            dirty = false;

            if (joystick != null)
                joystick.UpdateVJD(id, ref iReport);
        }

        public void Dispose()
        {
            if (joystick != null)
            {
                joystick.RelinquishVJD(id);
                joystick = null;
            }
        }

        private bool dirty = false;

    }

}
*/