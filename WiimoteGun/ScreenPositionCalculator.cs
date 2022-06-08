using System;
using System.Linq;
using System.Windows.Forms;
using WiimoteLib.Geometry;
using WiimoteLib.DataTypes;

namespace WiimoteGun
{
    class ScreenPositionCalculator
    {
        public ScreenPositionCalculator(int screenIndex)
        {
            _screenIndex = screenIndex;

            if (Options.Instance.CalibrationTop != -1 && Options.Instance.CalibrationLeft != -1 && Options.Instance.CalibrationCenterX != -1 && Options.Instance.CalibrationCenterY != -1)
            {
                _topLeftPt = new Point2F(Options.Instance.CalibrationLeft, Options.Instance.CalibrationTop);
                _centerPt = new Point2F(Options.Instance.CalibrationCenterX, Options.Instance.CalibrationCenterY);
            }
        }

        private int _screenIndex;

        private Point2F _firstSensorPos;
        private Point2F _secondSensorPos;
        private Point2F _midSensorPos;

        private static Point2F? _centerPt;
        private static Point2F? _topLeftPt;

        private CalibrateForm _calibrateForm;

        public bool IsCalibrating { get { return _calibrateForm != null; } }
        public bool IsCalibrated { get { return _centerPt.HasValue && _topLeftPt.HasValue; } }

        public void Calibrate()
        {
            if (_calibrateForm != null)
                return;

            ResetCalibration();

            Program.PostToUIThread(() =>
            {
                _calibrateForm = new CalibrateForm(_screenIndex);
                _calibrateForm.Show();
            });
        }

        public void EndCalibrate()
        {
            if (_calibrateForm == null)
                return;

            Options.Instance.CalibrationTop = _topLeftPt.HasValue ? _topLeftPt.Value.Y : -1;
            Options.Instance.CalibrationLeft = _topLeftPt.HasValue ? _topLeftPt.Value.X : -1;
            Options.Instance.CalibrationCenterX = _centerPt.HasValue ? _centerPt.Value.X : -1;
            Options.Instance.CalibrationCenterY = _centerPt.HasValue ? _centerPt.Value.Y : -1;
            Options.Instance.Save();

            var frm = _calibrateForm;
            _calibrateForm = null;

            Program.PostToUIThread(() => { frm.Dispose(); });
        }

        public void ResetCalibration()
        {
            _centerPt = null;
            _topLeftPt = null;
        }

        /// <summary>
        /// Calculates the Cursor Position on Screen by using the Midpoint of the 2 Leds in the sensor bar
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        public System.Drawing.Point? GetPosition(WiimoteLib.DataTypes.IRState ir, WiimoteLib.DataTypes.ButtonState buttons, WiimoteLib.DataTypes.ButtonState lastState)
        {
            Screen screen = null;

            if (_screenIndex > 0)
                screen = Screen.AllScreens.Skip(_screenIndex).FirstOrDefault();

            if (screen == null)
                screen = Screen.PrimaryScreen;

            var screenWidth = screen.Bounds.Width;
            var screenHeight = screen.Bounds.Height;

            int minXPos = 0;
            int maxXPos = screenWidth;
            int maxWidth = maxXPos - minXPos;
            int minYPos = 0;
            int maxYPos = screenHeight;
            int maxHeight = maxYPos - minYPos;

            Point2F relativePosition = new Point2F();

            bool hasSensor = true;

            if (ir.IRSensor0.Found && ir.IRSensor1.Found)
            {
                relativePosition = ir.Midpoint;
            }
            else if (ir.IRSensor0.Found)
            {
                relativePosition.X = _midSensorPos.X + (ir.IRSensor0.Position.X - _firstSensorPos.X);
                relativePosition.Y = _midSensorPos.Y + (ir.IRSensor0.Position.Y - _firstSensorPos.Y);
            }
            else if (ir.IRSensor1.Found)
            {
                relativePosition.X = _midSensorPos.X + (ir.IRSensor1.Position.X - _secondSensorPos.X);
                relativePosition.Y = _midSensorPos.Y + (ir.IRSensor1.Position.Y - _secondSensorPos.Y);
            }
            else
                hasSensor = false;

            if (hasSensor)
            {
                _firstSensorPos = ir.IRSensor0.Position;
                _secondSensorPos = ir.IRSensor1.Position;
                _midSensorPos = relativePosition;

                relativePosition.X = 1.0f - relativePosition.X;

                if (_calibrateForm != null && ((buttons.A && !lastState.A) || (buttons.B && !lastState.B)) && (!_centerPt.HasValue || !_topLeftPt.HasValue))
                {
                    if (!_centerPt.HasValue)
                        _centerPt = relativePosition;
                    else
                        _topLeftPt = relativePosition;
                }

                if (_topLeftPt.HasValue && _centerPt.HasValue)
                {
                    relativePosition.X = (relativePosition.X - _topLeftPt.Value.X) / ((_centerPt.Value.X - _topLeftPt.Value.X) * 2);
                    relativePosition.Y = (relativePosition.Y - _topLeftPt.Value.Y) / ((_centerPt.Value.Y - _topLeftPt.Value.Y) * 2);
                }
            }

            if (_calibrateForm != null)
            {
                _calibrateForm.UpdateState(relativePosition, _centerPt, _topLeftPt);

                if (buttons.Home && !lastState.Home)
                {
                    ResetCalibration();
                    EndCalibrate();
                }
                else if (IsCalibrated)
                    EndCalibrate();
            }

            if (!hasSensor)
            {
                _lastRelativePosition = null;
                return null;
            }

            var prev = _lastRelativePosition;
            _lastRelativePosition = relativePosition;

            if (_calibrateForm == null && prev.HasValue)
            {
                relativePosition.X = relativePosition.X * 0.7f + (float)prev.Value.X * 0.3f;
                relativePosition.Y = relativePosition.Y * 0.7f + (float)prev.Value.Y * 0.3f;
            }

            int x = Convert.ToInt32((float)screenWidth * relativePosition.X).Clamp(0, screenWidth);
            int y = Convert.ToInt32((float)screenHeight * relativePosition.Y).Clamp(0, screenHeight);

            return new System.Drawing.Point() { X = x + screen.Bounds.Left, Y = y + screen.Bounds.Top };
        }

        Point2F? _lastRelativePosition;
    }
}