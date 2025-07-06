using System;
using System.Linq;
using System.Windows.Forms;

namespace NegativeScreen
{
    public class SettingsForm : Form
    {
        private CheckedListBox displayList;
        private Button saveButton;
        private Button cancelButton;

        public string[] SelectedDisplays { get; private set; }

        public SettingsForm(string[] selectedDisplays)
        {
            this.Text = "NegativeScreen Settings";
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Width = 300;
            this.Height = 300;

            displayList = new CheckedListBox();
            displayList.Dock = DockStyle.Top;
            displayList.Height = 200;
            foreach (var screen in Screen.AllScreens)
            {
                int index = displayList.Items.Add(screen.DeviceName);
                if (selectedDisplays.Contains(screen.DeviceName))
                    displayList.SetItemChecked(index, true);
            }

            saveButton = new Button { Text = "Save", Dock = DockStyle.Bottom };
            cancelButton = new Button { Text = "Cancel", Dock = DockStyle.Bottom };

            saveButton.Click += (s, e) =>
            {
                SelectedDisplays = displayList.CheckedItems.Cast<string>().ToArray();
                this.DialogResult = DialogResult.OK;
                this.Close();
            };
            cancelButton.Click += (s, e) =>
            {
                this.DialogResult = DialogResult.Cancel;
                this.Close();
            };

            this.Controls.Add(displayList);
            this.Controls.Add(saveButton);
            this.Controls.Add(cancelButton);
        }
    }
}
