using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace NegativeScreen
{
    public class ConfigForm : Form
    {
        private List<CheckBox> displayChecks = new List<CheckBox>();
        private NumericUpDown refreshBox;
        private Button saveButton;
        private Button cancelButton;
        private Config config;

        public ConfigForm(Config cfg)
        {
            this.config = cfg;
            this.Text = "Settings";
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.StartPosition = FormStartPosition.CenterScreen;
            int y = 10;
            foreach (var screen in Screen.AllScreens)
            {
                CheckBox cb = new CheckBox();
                cb.Text = screen.DeviceName;
                cb.Checked = cfg.EnabledDisplays.Contains(screen.DeviceName);
                cb.Left = 10;
                cb.Top = y;
                this.Controls.Add(cb);
                displayChecks.Add(cb);
                y += 25;
            }
            Label lbl = new Label();
            lbl.Text = "Refresh Interval (ms)";
            lbl.Left = 10;
            lbl.Top = y;
            lbl.Width = 130;
            this.Controls.Add(lbl);
            refreshBox = new NumericUpDown();
            refreshBox.Left = 150;
            refreshBox.Top = y;
            refreshBox.Minimum = 0;
            refreshBox.Maximum = 1000;
            refreshBox.Value = cfg.RefreshInterval;
            this.Controls.Add(refreshBox);
            y += 35;
            saveButton = new Button();
            saveButton.Text = "Save";
            saveButton.Left = 40;
            saveButton.Top = y;
            saveButton.Click += new EventHandler(SaveButton_Click);
            this.Controls.Add(saveButton);
            cancelButton = new Button();
            cancelButton.Text = "Cancel";
            cancelButton.Left = 120;
            cancelButton.Top = y;
            cancelButton.Click += new EventHandler((s,e)=>this.DialogResult = DialogResult.Cancel);
            this.Controls.Add(cancelButton);
            this.ClientSize = new Size(220, y + 40);
        }

        private void SaveButton_Click(object sender, EventArgs e)
        {
            config.EnabledDisplays = new List<string>();
            foreach (CheckBox cb in displayChecks)
            {
                if (cb.Checked)
                    config.EnabledDisplays.Add(cb.Text);
            }
            config.RefreshInterval = (int)refreshBox.Value;
            config.Save();
            this.DialogResult = DialogResult.OK;
            this.Close();
        }
    }
}
