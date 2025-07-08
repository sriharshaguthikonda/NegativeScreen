using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Drawing;

namespace NegativeScreen
{
    internal class MonitorSelectionForm : Form
    {
        private CheckedListBox listBox = new CheckedListBox();
        private Button okButton = new Button();
        private Button cancelButton = new Button();
        private List<string> deviceNames = new List<string>();

        public List<string> Selected { get; private set; }

        public MonitorSelectionForm(List<string> current)
        {
            this.Text = "Select monitors";
            this.Size = new Size(400, 300);
            listBox.Dock = DockStyle.Top;
            listBox.Height = 200;
            int i = 0;
            foreach (var screen in Screen.AllScreens)
            {
                string display = OverlayManager.GetMonitorDetail(screen);
                listBox.Items.Add(display);
                deviceNames.Add(screen.DeviceName);
                if (current.Contains(screen.DeviceName))
                {
                    listBox.SetItemChecked(i, true);
                }
                i++;
            }
            okButton.Text = "OK";
            okButton.DialogResult = DialogResult.OK;
            okButton.Dock = DockStyle.Bottom;
            cancelButton.Text = "Cancel";
            cancelButton.DialogResult = DialogResult.Cancel;
            cancelButton.Dock = DockStyle.Bottom;
            Controls.Add(listBox);
            Controls.Add(okButton);
            Controls.Add(cancelButton);
            this.AcceptButton = okButton;
            this.CancelButton = cancelButton;
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            Selected = new List<string>();
            for (int i = 0; i < listBox.Items.Count; i++)
            {
                if (listBox.GetItemChecked(i))
                {
                    Selected.Add(deviceNames[i]);
                }
            }
        }
    }
}
