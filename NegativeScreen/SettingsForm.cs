using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace NegativeScreen
{
    class SettingsForm : Form
    {
        private CheckedListBox displayList;
        private Button saveButton;
        private Button cancelButton;
        private List<Screen> screens;

        public SettingsForm(List<Screen> screens, IEnumerable<string> enabled)
        {
            this.screens = screens;
            this.Text = "Settings";
            this.Size = new Size(300, 200);

            displayList = new CheckedListBox { Dock = DockStyle.Top };
            int i = 1;
            foreach (var screen in screens)
            {
                string name = $"Display {i++} {(screen.Primary ? "(Primary)" : "")}: {screen.DeviceName}";
                displayList.Items.Add(name, enabled.Contains(screen.DeviceName));
            }

            saveButton = new Button { Text = "Save", DialogResult = DialogResult.OK };
            cancelButton = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel };

            FlowLayoutPanel panel = new FlowLayoutPanel { Dock = DockStyle.Bottom, FlowDirection = FlowDirection.RightToLeft };
            panel.Controls.Add(saveButton);
            panel.Controls.Add(cancelButton);

            this.Controls.Add(displayList);
            this.Controls.Add(panel);
        }

        public List<string> GetSelectedDisplays()
        {
            var selected = new List<string>();
            for (int i = 0; i < displayList.Items.Count; i++)
            {
                if (displayList.GetItemChecked(i))
                {
                    selected.Add(screens[i].DeviceName);
                }
            }
            return selected;
        }
    }
}
