using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;
using System.Diagnostics;

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
            int i = 0;
            foreach (var screen in Screen.AllScreens)
            {
                string name = OverlayManager.GetMonitorDetail(screen);
                monitorList.Items.Add(name);
                monitorKeys.Add(screen.DeviceName);
                if (current.Monitors.Contains(screen.DeviceName))
                    monitorList.SetItemChecked(i, true);
                i++;
            }
            monitors.Controls.Add(monitorList);

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

            this.Controls.Add(tabs);
            this.Controls.Add(applyButton);
            this.Controls.Add(cancelButton);
            this.Controls.Add(startMinimized);

            this.AcceptButton = applyButton;
            this.CancelButton = cancelButton;

            applyButton.Click += (s, e) => { CollectResult(); this.DialogResult = DialogResult.OK; };

            this.Shown += delegate { LoadWindowsAsync(current.Windows); };
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
            cfg.StartMinimized = startMinimized.Checked;
            Result = cfg;
        }
    }
}
