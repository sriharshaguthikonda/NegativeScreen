using System;
using System.Windows.Forms;

namespace NegativeScreen
{
    public class SettingsForm : Form
    {
        private NumericUpDown numericRefresh;
        private CheckedListBox displayList;
        private Button saveButton;

        private AppConfig config;

        public SettingsForm(AppConfig config)
        {
            this.config = config;
            this.Text = "Settings";
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.ClientSize = new System.Drawing.Size(220, 260);

            numericRefresh = new NumericUpDown();
            numericRefresh.Minimum = 0;
            numericRefresh.Maximum = 1000;
            numericRefresh.Location = new System.Drawing.Point(10, 10);
            numericRefresh.Value = config.RefreshInterval;
            this.Controls.Add(numericRefresh);

            displayList = new CheckedListBox();
            displayList.Location = new System.Drawing.Point(10, 40);
            displayList.Size = new System.Drawing.Size(200, 150);
            foreach (var screen in Screen.AllScreens)
            {
                bool enabled = config.EnabledDisplays.Contains(screen.DeviceName);
                displayList.Items.Add(screen.DeviceName, enabled);
            }
            this.Controls.Add(displayList);

            saveButton = new Button();
            saveButton.Text = "Save";
            saveButton.Location = new System.Drawing.Point(10, 210);
            saveButton.Click += new EventHandler(SaveButton_Click);
            this.Controls.Add(saveButton);
        }

        private void SaveButton_Click(object sender, EventArgs e)
        {
            config.RefreshInterval = (int)numericRefresh.Value;
            config.EnabledDisplays.Clear();
            foreach (var item in displayList.CheckedItems)
            {
                config.EnabledDisplays.Add(item.ToString());
            }
            config.Save();
            this.DialogResult = DialogResult.OK;
            this.Close();
        }
    }
}
