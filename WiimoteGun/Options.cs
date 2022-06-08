using Microsoft.Win32;
using System.ComponentModel;
using System.IO;
using System.Xml.Serialization;

namespace WiimoteGun
{
    public class Options
    {
        private Options()
        {
            MonitorId = 0;
            CalibrationTop = -1;
            CalibrationLeft = -1;
            CalibrationCenterX = -1;
            CalibrationCenterY = -1;
            IRSensitivity = 5;
            DetectDolphinbar = true;
            DetectBlueTooth = true;
            ShowNotifications = true;
        }

        private static Options _instance;

        public static Options Instance
        {
            get
            {
                if (_instance == null)
                {
                    try { _instance = GetSettingsFilename().FromXml<Options>(); }
                    catch { _instance = new Options(); }
                }

                return _instance;
            }
        }

        public void Save()
        {
            string xml = this.ToXml();

            try { File.WriteAllText(GetSettingsFilename(), xml); }
            catch { }
        }

        private static string GetSettingsFilename()
        {
            return Path.Combine(Path.GetDirectoryName(typeof(VirtualSendKey).Assembly.Location), "settings.cfg");
        }

        [DefaultValue(0)]
        public int MonitorId { get; set; }
        
        [DefaultValue(-1)]
        public float CalibrationTop { get; set; }
        
        [DefaultValue(-1)]
        public float CalibrationLeft { get; set; }

        [DefaultValue(-1)]
        public float CalibrationCenterX { get; set; }

        [DefaultValue(-1)]
        public float CalibrationCenterY { get; set; }

        [DefaultValue(true)]
        public bool DetectDolphinbar { get; set; }

        [DefaultValue(true)]
        public bool DetectBlueTooth { get; set; }

        [DefaultValue(true)]
        public bool ShowNotifications { get; set; }

        [DefaultValue(5)]
        public int IRSensitivity { get; set; }

        [XmlIgnore]
        public bool StartWithWindows
        {
            get
            {
                bool ret = false;

                try
                {
                    RegistryKey rk = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);
                    if (rk != null)
                    {
                        ret = rk.GetValue("WiimoteGun") != null;
                        rk.Close();
                    }
                }
                catch { }

                return ret;
            }
            set
            {
                try
                {
                    RegistryKey rk = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);
                    if (rk != null)
                    {
                        if (value)
                            rk.SetValue("WiimoteGun", typeof(Program).Assembly.Location);
                        else
                            rk.DeleteValue("WiimoteGun", false);

                        rk.Close();
                    }
                }
                catch { }
            }
        }
    }
}
