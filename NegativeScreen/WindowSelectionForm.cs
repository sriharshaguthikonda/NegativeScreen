using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;

namespace NegativeScreen
{
    internal class WindowSelectionForm : Form
    {
        private CheckedListBox listBox = new CheckedListBox();
        private Button okButton = new Button();
        private Button cancelButton = new Button();
        private List<string> values = new List<string>();

        public List<string> Selected { get; private set; }

        public WindowSelectionForm(List<string> current)
        {
            this.Text = "Select windows";
            this.Size = new Size(500, 400);
            listBox.Dock = DockStyle.Top;
            listBox.Height = 300;
            int i = 0;
            foreach (Process proc in Process.GetProcesses())
            {
                if (proc.MainWindowHandle != IntPtr.Zero && !string.IsNullOrEmpty(proc.MainWindowTitle))
                {
                    string value = proc.ProcessName + "|" + proc.MainWindowTitle;
                    string display = proc.ProcessName + " - " + proc.MainWindowTitle;
                    listBox.Items.Add(display);
                    values.Add(value);
                    if (current.Contains(value))
                    {
                        listBox.SetItemChecked(i, true);
                    }
                    i++;
                }
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
                    Selected.Add(values[i]);
                }
            }
        }
    }
}
