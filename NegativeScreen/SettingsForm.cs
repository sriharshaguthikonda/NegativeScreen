using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;
using System.Diagnostics;
using System.Linq;

namespace NegativeScreen
{
    internal class SettingsForm : Form
    {
        private CheckedListBox monitorList = new CheckedListBox();
        private CheckedListBox windowList = new CheckedListBox();
        private TextBox searchBox = new TextBox();
        private Button applyButton = new Button();
        private Button cancelButton = new Button();
        private CheckBox startMinimized = new CheckBox();
        private CheckBox darkMode = new CheckBox();
        private Button renameButton = new Button();

        private Dictionary<string, string> aliases = new Dictionary<string, string>();
        private List<string> monitorIds = new List<string>();

        private List<string> monitorKeys = new List<string>();
        private List<string> windowKeys = new List<string>();
        private List<string> selectedKeys = new List<string>();

        public Config Result { get; private set; }

        public SettingsForm(Config current)
        {
            this.Text = "NegativeScreen Settings";
            this.Font = new Font("Segoe UI", 9F);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Size = new Size(500, 500);
            TabControl tabs = new TabControl { Dock = DockStyle.Fill };
            TabPage monitors = new TabPage("Monitors");
            TabPage windows = new TabPage("Windows");
            tabs.TabPages.Add(monitors);
            tabs.TabPages.Add(windows);
            monitorList.Dock = DockStyle.Fill;
            foreach (var ml in current.MonitorLabels)
                aliases[!string.IsNullOrEmpty(ml.Id) ? ml.Id : ml.Device] = ml.Label;
            int i = 0;
            foreach (var screen in Screen.AllScreens)
            {
                string id = Settings.GetMonitorId(screen);
                string alias = aliases.ContainsKey(id) ? aliases[id] : null;
                string name = BuildMonitorDisplay(screen, alias, id);
                monitorList.Items.Add(name);
                monitorKeys.Add(screen.DeviceName);
                monitorIds.Add(id);
                if (current.Monitors.Contains(screen.DeviceName))
                    monitorList.SetItemChecked(i, true);
                i++;
            }
            monitors.Controls.Add(monitorList);
            renameButton.Text = "Rename";
            renameButton.Dock = DockStyle.Bottom;
            renameButton.Click += (s, e) => RenameSelectedMonitor();
            monitors.Controls.Add(renameButton);
            monitorList.KeyDown += (s, e) => { if (e.KeyCode == Keys.F2) { RenameSelectedMonitor(); e.Handled = true; } };

            Panel searchPanel = new Panel { Dock = DockStyle.Top, Height = 24 };
            searchBox.Dock = DockStyle.Fill;
            searchBox.Text = "";
            searchBox.TextChanged += (s, e) => FilterWindows();
            searchPanel.Controls.Add(searchBox);
            windows.Controls.Add(windowList);
            windows.Controls.Add(searchPanel);
            windowList.Dock = DockStyle.Fill;

            applyButton.Text = "Apply";
            applyButton.Dock = DockStyle.Bottom;
            cancelButton.Text = "Cancel";
            cancelButton.Dock = DockStyle.Bottom;
            cancelButton.DialogResult = DialogResult.Cancel;
            startMinimized.Text = "Open minimized on startup";
            startMinimized.Dock = DockStyle.Bottom;
            startMinimized.Checked = current.StartMinimized;

            darkMode.Text = "Dark mode";
            darkMode.Dock = DockStyle.Bottom;
            darkMode.Checked = current.DarkMode;
            darkMode.CheckedChanged += (s, e) => ApplyTheme();

            this.Controls.Add(tabs);
            this.Controls.Add(applyButton);
            this.Controls.Add(cancelButton);
            this.Controls.Add(startMinimized);
            this.Controls.Add(darkMode);

            this.AcceptButton = applyButton;
            this.CancelButton = cancelButton;

            applyButton.Click += (s, e) => { CollectResult(); this.DialogResult = DialogResult.OK; };

            this.Shown += delegate { LoadWindowsAsync(current.Windows); };
            ApplyTheme();
        }

        private void LoadWindowsAsync(List<string> current)
        {
            ThreadPool.QueueUserWorkItem(delegate {
                var procs = Process.GetProcesses();
                List<KeyValuePair<string, string>> list = new List<KeyValuePair<string, string>>();
                foreach (var proc in procs)
                {
                    try
                    {
                        if (proc.MainWindowHandle != IntPtr.Zero && !string.IsNullOrEmpty(proc.MainWindowTitle))
                        {
                            string key = proc.ProcessName + "|" + proc.MainWindowTitle;
                            list.Add(new KeyValuePair<string, string>(key, proc.MainWindowTitle));
                        }
                    }
                    catch { }
                }
                this.Invoke(new MethodInvoker(delegate
                {
                    int i = 0;
                    windowList.BeginUpdate();
                    foreach (var item in list)
                    {
                        windowList.Items.Add(item.Value);
                        windowKeys.Add(item.Key);
                        if (current.Contains(item.Key))
                        {
                            windowList.SetItemChecked(i, true);
                            selectedKeys.Add(item.Key);
                        }
                        i++;
                    }
                    windowList.EndUpdate();
                }));
            });
        }

        private void FilterWindows()
        {
            string filter = searchBox.Text.ToLowerInvariant();
            windowList.BeginUpdate();
            windowList.Items.Clear();
            int i = 0;
            for (int idx = 0; idx < windowKeys.Count; idx++)
            {
                string display = windowKeys[idx].Split('|')[1];
                if (string.IsNullOrEmpty(filter) || display.ToLowerInvariant().Contains(filter))
                {
                    windowList.Items.Add(display);
                    if (selectedKeys.Contains(windowKeys[idx]))
                        windowList.SetItemChecked(i, true);
                    i++;
                }
            }
            windowList.EndUpdate();
        }

        private void CollectResult()
        {
            Config cfg = new Config();
            selectedKeys.Clear();
            for (int i = 0; i < monitorList.Items.Count; i++)
                if (monitorList.GetItemChecked(i))
                    cfg.Monitors.Add(monitorKeys[i]);
            for (int i = 0; i < windowList.Items.Count; i++)
                if (windowList.GetItemChecked(i))
                {
                    cfg.Windows.Add(windowKeys[i]);
                    selectedKeys.Add(windowKeys[i]);
                }
            for (int i = 0; i < monitorIds.Count; i++)
            {
                string id = monitorIds[i];
                string label = aliases.ContainsKey(id) ? aliases[id] : null;
                cfg.MonitorLabels.Add(new MonitorLabel { Device = monitorKeys[i], Id = id, Label = label });
            }
            cfg.StartMinimized = startMinimized.Checked;
            cfg.DarkMode = darkMode.Checked;
            Result = cfg;
        }

        private void ApplyTheme()
        {
            if (darkMode.Checked)
            {
                Color back = Color.FromArgb(45, 45, 48);
                Color fore = Color.WhiteSmoke;
                this.BackColor = back;
                foreach (Control c in this.Controls)
                {
                    c.BackColor = back;
                    c.ForeColor = fore;
                }
                monitorList.BackColor = back;
                monitorList.ForeColor = fore;
                windowList.BackColor = back;
                windowList.ForeColor = fore;
                searchBox.BackColor = Color.FromArgb(30, 30, 30);
                searchBox.ForeColor = fore;
            }
            else
            {
                this.BackColor = SystemColors.Control;
                foreach (Control c in this.Controls)
                {
                    c.BackColor = SystemColors.Control;
                    c.ForeColor = SystemColors.ControlText;
                }
            }
        }

        private string BuildMonitorDisplay(Screen screen, string alias, string id)
        {
            int index = Array.IndexOf(Screen.AllScreens, screen) + 1;
            string name = string.IsNullOrEmpty(alias) ? OverlayManager.GetMonitorName(screen) : alias;
            return $"Display {index} - {name} [{id}] ({screen.Bounds.Width}x{screen.Bounds.Height})";
        }

        private void RenameSelectedMonitor()
        {
            int idx = monitorList.SelectedIndex;
            if (idx < 0) return;
            string id = monitorIds[idx];
            string current = aliases.ContainsKey(id) ? aliases[id] : OverlayManager.GetMonitorName(Screen.AllScreens[idx]);
            string input = Prompt("Rename monitor", current);
            if (!string.IsNullOrEmpty(input))
            {
                aliases[id] = input;
                monitorList.Items[idx] = BuildMonitorDisplay(Screen.AllScreens[idx], input, id);
            }
        }

        private static string Prompt(string title, string value)
        {
            using (Form f = new Form())
            {
                f.Text = title;
                f.FormBorderStyle = FormBorderStyle.FixedDialog;
                f.StartPosition = FormStartPosition.CenterParent;
                f.Width = 300;
                f.Height = 120;
                TextBox box = new TextBox { Text = value, Dock = DockStyle.Top };
                Button ok = new Button { Text = "OK", Dock = DockStyle.Bottom, DialogResult = DialogResult.OK };
                f.Controls.Add(box);
                f.Controls.Add(ok);
                f.AcceptButton = ok;
                if (f.ShowDialog() == DialogResult.OK)
                    return box.Text;
            }
            return null;
        }
    }
}
