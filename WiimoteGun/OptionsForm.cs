using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using WiimoteLib;

namespace WiimoteGun
{
    public partial class OptionsForm : Form
    {
        public OptionsForm()
        {
            InitializeComponent();
            Font = SystemFonts.MessageBoxFont;

            numericUpDown1.Minimum = 0;  
            numericUpDown1.Maximum = Screen.AllScreens.Length - 1;
            numericUpDown1.Value = Options.Instance.MonitorId;

            cbStartWithWindows.Checked = Options.Instance.StartWithWindows;
            chkNotifications.Checked = Options.Instance.ShowNotifications;
            
            if (Options.Instance.DetectBlueTooth && Options.Instance.DetectDolphinbar)
                rbBoth.Checked = true;
            else if (Options.Instance.DetectDolphinbar)
                rbDolphinbar.Checked = true;
            else
                rbBlueTooth.Checked = true;

            trackBar1.SetRange(0, 5);
            trackBar1.Value = Options.Instance.IRSensitivity;
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void btnOk_Click(object sender, EventArgs e)
        {
            Options.Instance.DetectDolphinbar = rbBoth.Checked || rbDolphinbar.Checked;
            Options.Instance.DetectBlueTooth = rbBoth.Checked || rbBlueTooth.Checked;
            Options.Instance.StartWithWindows = cbStartWithWindows.Checked;
            Options.Instance.ShowNotifications = chkNotifications.Checked;
            Options.Instance.MonitorId = (int) numericUpDown1.Value;
            Options.Instance.IRSensitivity = trackBar1.Value;
            Options.Instance.Save();

            WiimoteManager.DolphinBarMode = Options.Instance.DetectDolphinbar;
            WiimoteManager.BluetoothMode = Options.Instance.DetectBlueTooth;

            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
