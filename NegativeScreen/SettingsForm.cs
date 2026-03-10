using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;
using System.Diagnostics;
using System.Linq;
using Microsoft.Win32;

namespace NegativeScreen
{
    internal class SettingsForm : Form
    {
        private sealed class WindowItem
        {
            public string Key;
            public string Title;

            public WindowItem(string key, string title)
            {
                Key = key;
                Title = title;
            }

            public override string ToString()
            {
                return Title;
            }
        }

        private CheckedListBox monitorList = new CheckedListBox();
        private CheckedListBox windowList = new CheckedListBox();
        private TextBox searchBox = new TextBox();
        private Button applyButton = new Button();
        private Button cancelButton = new Button();
        private CheckBox startMinimized = new CheckBox();
        private ComboBox themeMode = new ComboBox();
        private CheckBox magnifiedCursor = new CheckBox();
        private CheckBox softwareCursor = new CheckBox();
        private CheckBox normalizeCursorScheme = new CheckBox();
        private CheckBox autoInvert = new CheckBox();
        private CheckBox autoInvertFastPathDelta = new CheckBox();
        private CheckBox autoInvertFastPathCoverage = new CheckBox();
        private CheckBox autoInvertDualEma = new CheckBox();
        private CheckBox autoInvertCoverageGate = new CheckBox();
        private CheckBox autoInvertBurstSampling = new CheckBox();
        private CheckBox autoInvertConsecutive = new CheckBox();
        private CheckBox autoInvertDirectionalDebounce = new CheckBox();
        private CheckBox autoInvertTargetResponse = new CheckBox();
        private CheckBox autoInvertMediaPause = new CheckBox();
        private Button renameButton = new Button();

        private Dictionary<string, string> aliases = new Dictionary<string, string>();
        private List<string> monitorIds = new List<string>();

        private List<string> monitorKeys = new List<string>();
        private List<WindowItem> windowItems = new List<WindowItem>();
        private HashSet<string> selectedKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly Config currentConfig;
        private TabControl tabs = new TabControl();
        private SplitContainer contentSplit = new SplitContainer();
        private Panel headerPanel = new Panel();
        private Panel footerPanel = new Panel();
        private Label titleLabel = new Label();
        private Label subtitleLabel = new Label();
        private Label hotkeysLabel = new Label();

        public Config Result { get; private set; }

        public SettingsForm(Config current)
        {
            currentConfig = current;
            Text = "NegativeScreen Settings";
            Font = new Font("Segoe UI", 9F);
            StartPosition = FormStartPosition.CenterScreen;
            Size = new Size(980, 700);
            MinimumSize = new Size(900, 620);
            MaximizeBox = false;

            SuspendLayout();

            titleLabel.Text = "NegativeScreen";
            titleLabel.Font = new Font("Segoe UI Semibold", 16F, FontStyle.Bold);
            titleLabel.Dock = DockStyle.Top;
            titleLabel.Height = 34;

            subtitleLabel.Text = "Monitor targeting, cursor behavior, and auto-invert controls";
            subtitleLabel.Dock = DockStyle.Top;
            subtitleLabel.Height = 20;

            headerPanel.Dock = DockStyle.Top;
            headerPanel.Height = 72;
            headerPanel.Padding = new Padding(16, 10, 16, 8);
            headerPanel.Controls.Add(subtitleLabel);
            headerPanel.Controls.Add(titleLabel);

            tabs.Dock = DockStyle.Fill;
            TabPage monitors = new TabPage("Monitors");
            TabPage windows = new TabPage("Windows");
            tabs.TabPages.Add(monitors);
            tabs.TabPages.Add(windows);

            monitorList.Dock = DockStyle.Fill;
            monitorList.CheckOnClick = true;
            monitorList.IntegralHeight = false;
            monitorList.BorderStyle = BorderStyle.FixedSingle;

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

            TableLayoutPanel monitorsLayout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2 };
            monitorsLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            monitorsLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40F));
            monitorsLayout.Controls.Add(monitorList, 0, 0);
            renameButton.Text = "Rename Selected (F2)";
            renameButton.Dock = DockStyle.Fill;
            renameButton.Click += (s, e) => RenameSelectedMonitor();
            monitorsLayout.Controls.Add(renameButton, 0, 1);
            monitors.Controls.Add(monitorsLayout);
            monitorList.KeyDown += (s, e) => { if (e.KeyCode == Keys.F2) { RenameSelectedMonitor(); e.Handled = true; } };

            TableLayoutPanel windowsLayout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2 };
            windowsLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 36F));
            windowsLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            Panel searchPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(6, 7, 6, 4) };
            Label searchLabel = new Label { Text = "Search", AutoSize = true, Location = new Point(0, 6) };
            searchBox.Location = new Point(54, 3);
            searchBox.Width = 420;
            searchBox.BorderStyle = BorderStyle.FixedSingle;
            searchBox.TextChanged += (s, e) => FilterWindows();
            searchPanel.Controls.Add(searchLabel);
            searchPanel.Controls.Add(searchBox);
            windowList.Dock = DockStyle.Fill;
            windowList.CheckOnClick = true;
            windowList.IntegralHeight = false;
            windowList.BorderStyle = BorderStyle.FixedSingle;
            windowList.ItemCheck += WindowList_ItemCheck;
            windowsLayout.Controls.Add(searchPanel, 0, 0);
            windowsLayout.Controls.Add(windowList, 0, 1);
            windows.Controls.Add(windowsLayout);

            startMinimized.Text = "Open minimized on startup";
            startMinimized.Checked = current.StartMinimized;

            themeMode.DropDownStyle = ComboBoxStyle.DropDownList;
            themeMode.Items.AddRange(new object[] { "System", "Dark", "Light" });
            themeMode.Width = 140;
            themeMode.SelectedIndexChanged += (s, e) => ApplyTheme();
            switch (current.ThemeMode)
            {
                case ThemeMode.Dark:
                    themeMode.SelectedIndex = 1;
                    break;
                case ThemeMode.Light:
                    themeMode.SelectedIndex = 2;
                    break;
                default:
                    themeMode.SelectedIndex = 0;
                    break;
            }

            magnifiedCursor.Text = "Use magnified cursor (hide system cursor)";
            magnifiedCursor.Checked = current.UseMagnifiedCursor;

            softwareCursor.Text = "Force software cursor (minimal trails)";
            softwareCursor.Checked = current.ForceSoftwareCursor;
            softwareCursor.CheckedChanged += (s, e) =>
            {
                if (softwareCursor.Checked)
                {
                    magnifiedCursor.Checked = false;
                    magnifiedCursor.Enabled = false;
                    normalizeCursorScheme.Enabled = true;
                }
                else
                {
                    magnifiedCursor.Enabled = true;
                    normalizeCursorScheme.Enabled = false;
                }
            };

            normalizeCursorScheme.Text = "Normalize cursor colors while forcing software cursor";
            normalizeCursorScheme.Checked = current.NormalizeCursorScheme;
            normalizeCursorScheme.Enabled = softwareCursor.Checked;

            autoInvert.Text = "Auto invert by brightness (experimental)";
            autoInvert.Checked = current.AutoInvertByBrightness;
            autoInvert.CheckedChanged += (s, e) => UpdateAutoInvertControls();

            autoInvertFastPathDelta.Text = "Fast-path on brightness jump";
            autoInvertFastPathDelta.Checked = current.AutoInvertUseFastPathDelta;

            autoInvertFastPathCoverage.Text = "Fast-path on bright coverage spike";
            autoInvertFastPathCoverage.Checked = current.AutoInvertUseFastPathCoverage;

            autoInvertDualEma.Text = "Dual-EMA trigger";
            autoInvertDualEma.Checked = current.AutoInvertUseDualEma;

            autoInvertCoverageGate.Text = "Require coverage for slow-path";
            autoInvertCoverageGate.Checked = current.AutoInvertUseCoverageGate;

            autoInvertBurstSampling.Text = "Burst sampling on trigger";
            autoInvertBurstSampling.Checked = current.AutoInvertUseBurstSampling;

            autoInvertConsecutive.Text = "Consecutive-frame trigger";
            autoInvertConsecutive.Checked = current.AutoInvertUseConsecutiveTrigger;

            autoInvertDirectionalDebounce.Text = "Direction-dependent debounce";
            autoInvertDirectionalDebounce.Checked = current.AutoInvertUseDirectionalDebounce;

            autoInvertTargetResponse.Text = "Target-response smoothing";
            autoInvertTargetResponse.Checked = current.AutoInvertUseTargetResponse;

            autoInvertMediaPause.Text = "Pause on media motion";
            autoInvertMediaPause.Checked = current.AutoInvertUseMediaPause;

            FlowLayoutPanel optionsFlow = new FlowLayoutPanel();
            optionsFlow.Dock = DockStyle.Fill;
            optionsFlow.FlowDirection = FlowDirection.TopDown;
            optionsFlow.WrapContents = false;
            optionsFlow.AutoScroll = true;
            optionsFlow.Padding = new Padding(8, 4, 8, 4);

            optionsFlow.Controls.Add(CreateSection("Appearance",
                new Label { Text = "Theme", AutoSize = true, Margin = new Padding(0, 0, 0, 2) },
                themeMode,
                startMinimized));

            optionsFlow.Controls.Add(CreateSection("Cursor",
                magnifiedCursor,
                softwareCursor,
                normalizeCursorScheme));

            optionsFlow.Controls.Add(CreateSection("Auto Invert",
                autoInvert,
                autoInvertFastPathDelta,
                autoInvertFastPathCoverage,
                autoInvertDualEma,
                autoInvertCoverageGate,
                autoInvertBurstSampling,
                autoInvertConsecutive,
                autoInvertDirectionalDebounce,
                autoInvertTargetResponse,
                autoInvertMediaPause));

            contentSplit.Dock = DockStyle.Fill;
            contentSplit.IsSplitterFixed = false;
            contentSplit.FixedPanel = FixedPanel.Panel2;
            contentSplit.Padding = new Padding(12, 8, 12, 6);
            contentSplit.Panel1.Controls.Add(tabs);
            contentSplit.Panel2.Controls.Add(optionsFlow);

            hotkeysLabel.Text = "Hotkeys: Win+Alt+H exit, Win+Alt+N pause, Win+Alt+F1..F10 effects";
            hotkeysLabel.AutoSize = true;
            hotkeysLabel.Location = new Point(0, 10);

            applyButton.Text = "Apply";
            applyButton.Width = 112;
            applyButton.Height = 34;
            cancelButton.Text = "Cancel";
            cancelButton.Width = 112;
            cancelButton.Height = 34;
            cancelButton.DialogResult = DialogResult.Cancel;

            FlowLayoutPanel buttonFlow = new FlowLayoutPanel();
            buttonFlow.FlowDirection = FlowDirection.RightToLeft;
            buttonFlow.WrapContents = false;
            buttonFlow.Dock = DockStyle.Right;
            buttonFlow.AutoSize = true;
            buttonFlow.Controls.Add(applyButton);
            buttonFlow.Controls.Add(cancelButton);

            footerPanel.Dock = DockStyle.Bottom;
            footerPanel.Height = 56;
            footerPanel.Padding = new Padding(12, 8, 12, 8);
            footerPanel.Controls.Add(buttonFlow);
            footerPanel.Controls.Add(hotkeysLabel);

            Controls.Add(contentSplit);
            Controls.Add(footerPanel);
            Controls.Add(headerPanel);

            AcceptButton = applyButton;
            CancelButton = cancelButton;

            applyButton.Click += (s, e) => { CollectResult(); DialogResult = DialogResult.OK; };

            Shown += delegate
            {
                EnsureSplitLayout();
                LoadWindowsAsync(current.Windows);
            };
            Resize += (s, e) => EnsureSplitLayout();
            SystemEvents.UserPreferenceChanged += SystemEvents_UserPreferenceChanged;
            FormClosed += (s, e) => SystemEvents.UserPreferenceChanged -= SystemEvents_UserPreferenceChanged;

            UpdateAutoInvertControls();
            ApplyTheme();
            ResumeLayout(true);
        }

        private void EnsureSplitLayout()
        {
            if (contentSplit.IsDisposed || !contentSplit.IsHandleCreated)
                return;

            int totalWidth = contentSplit.ClientSize.Width;
            if (totalWidth <= 0)
                return;

            int desiredRightWidth = 300;
            int minLeftWidth = 320;
            int minRightWidth = 240;
            int maxLeftWidth = Math.Max(minLeftWidth, totalWidth - minRightWidth);
            int proposedLeftWidth = totalWidth - desiredRightWidth;
            if (proposedLeftWidth < minLeftWidth)
                proposedLeftWidth = minLeftWidth;
            if (proposedLeftWidth > maxLeftWidth)
                proposedLeftWidth = maxLeftWidth;

            if (maxLeftWidth >= minLeftWidth && proposedLeftWidth > 0 && proposedLeftWidth < totalWidth)
            {
                contentSplit.Panel1MinSize = minLeftWidth;
                contentSplit.Panel2MinSize = minRightWidth;
                contentSplit.SplitterDistance = proposedLeftWidth;
            }
        }

        private void LoadWindowsAsync(List<string> current)
        {
            ThreadPool.QueueUserWorkItem(delegate {
                var procs = Process.GetProcesses();
                List<WindowItem> list = new List<WindowItem>();
                HashSet<string> seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var proc in procs)
                {
                    try
                    {
                        if (proc.MainWindowHandle != IntPtr.Zero && !string.IsNullOrEmpty(proc.MainWindowTitle))
                        {
                            string key = proc.ProcessName + "|" + proc.MainWindowTitle;
                            if (seen.Add(key))
                                list.Add(new WindowItem(key, proc.MainWindowTitle));
                        }
                    }
                    catch { }
                }
                list.Sort((a, b) => string.Compare(a.Title, b.Title, StringComparison.CurrentCultureIgnoreCase));
                this.Invoke(new MethodInvoker(delegate
                {
                    windowItems.Clear();
                    windowItems.AddRange(list);
                    selectedKeys.Clear();
                    foreach (var item in list)
                    {
                        if (current.Contains(item.Key))
                            selectedKeys.Add(item.Key);
                    }
                    windowList.BeginUpdate();
                    RebuildWindowList(searchBox.Text);
                    windowList.EndUpdate();
                }));
            });
        }

        private void FilterWindows()
        {
            RebuildWindowList(searchBox.Text);
        }

        private void RebuildWindowList(string filterText)
        {
            string filter = string.IsNullOrEmpty(filterText) ? string.Empty : filterText.Trim().ToLowerInvariant();
            windowList.Items.Clear();
            for (int i = 0; i < windowItems.Count; i++)
            {
                WindowItem item = windowItems[i];
                if (filter.Length == 0 || item.Title.ToLowerInvariant().Contains(filter))
                {
                    int index = windowList.Items.Add(item);
                    if (selectedKeys.Contains(item.Key))
                        windowList.SetItemChecked(index, true);
                }
            }
        }

        private void WindowList_ItemCheck(object sender, ItemCheckEventArgs e)
        {
            if (e.Index < 0 || e.Index >= windowList.Items.Count)
                return;
            WindowItem item = windowList.Items[e.Index] as WindowItem;
            if (item == null)
                return;
            if (e.NewValue == CheckState.Checked)
                selectedKeys.Add(item.Key);
            else
                selectedKeys.Remove(item.Key);
        }

        private void CollectResult()
        {
            Config cfg = new Config();
            for (int i = 0; i < monitorList.Items.Count; i++)
                if (monitorList.GetItemChecked(i))
                    cfg.Monitors.Add(monitorKeys[i]);
            foreach (string key in selectedKeys.OrderBy(k => k, StringComparer.OrdinalIgnoreCase))
                cfg.Windows.Add(key);
            for (int i = 0; i < monitorIds.Count; i++)
            {
                string id = monitorIds[i];
                string label = aliases.ContainsKey(id) ? aliases[id] : null;
                cfg.MonitorLabels.Add(new MonitorLabel { Device = monitorKeys[i], Id = id, Label = label });
            }
            cfg.StartMinimized = startMinimized.Checked;
            cfg.ThemeMode = GetSelectedThemeMode();
            cfg.DarkMode = cfg.ThemeMode == ThemeMode.Dark || (cfg.ThemeMode == ThemeMode.System && IsSystemDarkTheme());
            cfg.UseMagnifiedCursor = magnifiedCursor.Checked;
            cfg.ForceSoftwareCursor = softwareCursor.Checked;
            cfg.NormalizeCursorScheme = normalizeCursorScheme.Checked;
            cfg.AutoInvertByBrightness = autoInvert.Checked;
            cfg.AutoInvertSampleMs = currentConfig.AutoInvertSampleMs;
            cfg.AutoInvertBrightThreshold = currentConfig.AutoInvertBrightThreshold;
            cfg.AutoInvertDarkThreshold = currentConfig.AutoInvertDarkThreshold;
            cfg.AutoInvertBrightDwellMs = currentConfig.AutoInvertBrightDwellMs;
            cfg.AutoInvertDarkDwellMs = currentConfig.AutoInvertDarkDwellMs;
            cfg.AutoInvertMinHoldMs = currentConfig.AutoInvertMinHoldMs;
            cfg.AutoInvertRequiredSamples = currentConfig.AutoInvertRequiredSamples;
            cfg.AutoInvertSmoothingAlpha = currentConfig.AutoInvertSmoothingAlpha;
            cfg.AutoInvertBrightPixelThreshold = currentConfig.AutoInvertBrightPixelThreshold;
            cfg.AutoInvertBrightCoverageThreshold = currentConfig.AutoInvertBrightCoverageThreshold;
            cfg.AutoInvertDarkCoverageThreshold = currentConfig.AutoInvertDarkCoverageThreshold;
            cfg.AutoInvertUseFastPathDelta = autoInvertFastPathDelta.Checked;
            cfg.AutoInvertUseFastPathCoverage = autoInvertFastPathCoverage.Checked;
            cfg.AutoInvertFastPathDeltaThreshold = currentConfig.AutoInvertFastPathDeltaThreshold;
            cfg.AutoInvertFastPathCoverageThreshold = currentConfig.AutoInvertFastPathCoverageThreshold;
            cfg.AutoInvertUseDualEma = autoInvertDualEma.Checked;
            cfg.AutoInvertFastEmaAlpha = currentConfig.AutoInvertFastEmaAlpha;
            cfg.AutoInvertSlowEmaAlpha = currentConfig.AutoInvertSlowEmaAlpha;
            cfg.AutoInvertEmaDiffThreshold = currentConfig.AutoInvertEmaDiffThreshold;
            cfg.AutoInvertUseBurstSampling = autoInvertBurstSampling.Checked;
            cfg.AutoInvertBurstSampleMs = currentConfig.AutoInvertBurstSampleMs;
            cfg.AutoInvertBurstDurationMs = currentConfig.AutoInvertBurstDurationMs;
            cfg.AutoInvertUseConsecutiveTrigger = autoInvertConsecutive.Checked;
            cfg.AutoInvertUseCoverageGate = autoInvertCoverageGate.Checked;
            cfg.AutoInvertUseDirectionalDebounce = autoInvertDirectionalDebounce.Checked;
            cfg.AutoInvertUseTargetResponse = autoInvertTargetResponse.Checked;
            cfg.AutoInvertTargetResponseMs = currentConfig.AutoInvertTargetResponseMs;
            cfg.AutoInvertUseMediaPause = autoInvertMediaPause.Checked;
            cfg.AutoInvertMediaDeltaThreshold = currentConfig.AutoInvertMediaDeltaThreshold;
            cfg.AutoInvertMediaScoreThreshold = currentConfig.AutoInvertMediaScoreThreshold;
            cfg.AutoInvertMediaScoreAlpha = currentConfig.AutoInvertMediaScoreAlpha;
            cfg.AutoInvertMediaHoldMs = currentConfig.AutoInvertMediaHoldMs;
            cfg.AutoInvertHideOverlays = currentConfig.AutoInvertHideOverlays;
            Result = cfg;
        }

        private Panel CreateSection(string title, params Control[] controls)
        {
            Panel section = new Panel();
            section.BorderStyle = BorderStyle.FixedSingle;
            section.Padding = new Padding(10);
            section.Margin = new Padding(0, 0, 0, 10);
            section.Width = 280;
            section.AutoSize = true;
            section.AutoSizeMode = AutoSizeMode.GrowAndShrink;

            Label heading = new Label();
            heading.Text = title;
            heading.Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold);
            heading.AutoSize = true;
            heading.Dock = DockStyle.Top;
            heading.Margin = new Padding(0, 0, 0, 6);
            section.Controls.Add(heading);

            FlowLayoutPanel flow = new FlowLayoutPanel();
            flow.Dock = DockStyle.Top;
            flow.FlowDirection = FlowDirection.TopDown;
            flow.WrapContents = false;
            flow.AutoSize = true;
            flow.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            flow.Margin = new Padding(0);

            foreach (Control control in controls)
            {
                control.Margin = new Padding(0, 2, 0, 4);
                flow.Controls.Add(control);
            }

            section.Controls.Add(flow);
            flow.BringToFront();
            return section;
        }

        private void UpdateAutoInvertControls()
        {
            bool enabled = autoInvert.Checked;
            autoInvertFastPathDelta.Enabled = enabled;
            autoInvertFastPathCoverage.Enabled = enabled;
            autoInvertDualEma.Enabled = enabled;
            autoInvertCoverageGate.Enabled = enabled;
            autoInvertBurstSampling.Enabled = enabled;
            autoInvertConsecutive.Enabled = enabled;
            autoInvertDirectionalDebounce.Enabled = enabled;
            autoInvertTargetResponse.Enabled = enabled;
            autoInvertMediaPause.Enabled = enabled;
        }

        private ThemeMode GetSelectedThemeMode()
        {
            if (themeMode.SelectedIndex == 1)
                return ThemeMode.Dark;
            if (themeMode.SelectedIndex == 2)
                return ThemeMode.Light;
            return ThemeMode.System;
        }

        private static bool IsSystemDarkTheme()
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Software\\Microsoft\\Windows\\CurrentVersion\\Themes\\Personalize"))
                {
                    object value = key != null ? key.GetValue("AppsUseLightTheme") : null;
                    if (value != null)
                    {
                        int number;
                        if (int.TryParse(value.ToString(), out number))
                            return number == 0;
                    }
                }
            }
            catch
            {
            }
            return false;
        }

        private void SystemEvents_UserPreferenceChanged(object sender, UserPreferenceChangedEventArgs e)
        {
            if (GetSelectedThemeMode() != ThemeMode.System)
                return;
            if (e.Category == UserPreferenceCategory.Color ||
                e.Category == UserPreferenceCategory.General ||
                e.Category == UserPreferenceCategory.VisualStyle)
            {
                BeginInvoke(new MethodInvoker(ApplyTheme));
            }
        }

        private void ApplyTheme()
        {
            ThemeMode selected = GetSelectedThemeMode();
            bool dark = selected == ThemeMode.Dark || (selected == ThemeMode.System && IsSystemDarkTheme());

            Color formBack = dark ? Color.FromArgb(30, 30, 30) : Color.FromArgb(247, 247, 249);
            Color surface = dark ? Color.FromArgb(37, 37, 38) : Color.White;
            Color surfaceAlt = dark ? Color.FromArgb(45, 45, 48) : Color.FromArgb(239, 239, 242);
            Color border = dark ? Color.FromArgb(63, 63, 70) : Color.FromArgb(211, 211, 217);
            Color fore = dark ? Color.FromArgb(241, 241, 241) : Color.FromArgb(26, 26, 30);
            Color muted = dark ? Color.FromArgb(185, 185, 185) : Color.FromArgb(82, 82, 88);
            Color input = dark ? Color.FromArgb(30, 30, 30) : Color.White;
            Color accent = dark ? Color.FromArgb(0, 122, 204) : Color.FromArgb(0, 99, 177);
            Color accentHover = dark ? Color.FromArgb(28, 151, 234) : Color.FromArgb(0, 122, 204);

            BackColor = formBack;
            ForeColor = fore;
            headerPanel.BackColor = surfaceAlt;
            footerPanel.BackColor = surfaceAlt;
            titleLabel.ForeColor = fore;
            subtitleLabel.ForeColor = muted;
            hotkeysLabel.ForeColor = muted;
            searchBox.BackColor = input;
            searchBox.ForeColor = fore;
            monitorList.BackColor = input;
            monitorList.ForeColor = fore;
            windowList.BackColor = input;
            windowList.ForeColor = fore;
            tabs.BackColor = surface;
            tabs.ForeColor = fore;

            applyButton.FlatStyle = FlatStyle.Flat;
            applyButton.FlatAppearance.BorderSize = 1;
            applyButton.FlatAppearance.BorderColor = accent;
            applyButton.FlatAppearance.MouseOverBackColor = accentHover;
            applyButton.BackColor = accent;
            applyButton.ForeColor = Color.White;

            cancelButton.FlatStyle = FlatStyle.Flat;
            cancelButton.FlatAppearance.BorderSize = 1;
            cancelButton.FlatAppearance.BorderColor = border;
            cancelButton.FlatAppearance.MouseOverBackColor = surfaceAlt;
            cancelButton.BackColor = surface;
            cancelButton.ForeColor = fore;

            renameButton.FlatStyle = FlatStyle.Flat;
            renameButton.FlatAppearance.BorderSize = 1;
            renameButton.FlatAppearance.BorderColor = border;
            renameButton.FlatAppearance.MouseOverBackColor = surfaceAlt;
            renameButton.BackColor = surface;
            renameButton.ForeColor = fore;

            themeMode.BackColor = input;
            themeMode.ForeColor = fore;

            ApplyThemeRecursive(this, surface, surfaceAlt, fore);
            subtitleLabel.ForeColor = muted;
            hotkeysLabel.ForeColor = muted;
        }

        private void ApplyThemeRecursive(Control parent, Color surface, Color surfaceAlt, Color fore)
        {
            foreach (Control c in parent.Controls)
            {
                if (c is SplitContainer || c is TableLayoutPanel || c is FlowLayoutPanel || c is Panel)
                {
                    if (c != headerPanel && c != footerPanel)
                        c.BackColor = surface;
                }
                if (c is TabPage)
                {
                    c.BackColor = surface;
                    c.ForeColor = fore;
                }
                else if (c is CheckBox || c is Label)
                {
                    c.ForeColor = fore;
                    if (!(c == subtitleLabel || c == hotkeysLabel))
                        c.BackColor = c.Parent != null ? c.Parent.BackColor : surface;
                }
                else if (c is TextBox || c is CheckedListBox || c is ComboBox)
                {
                    c.ForeColor = fore;
                }
                else if (c is Panel && ((Panel)c).BorderStyle == BorderStyle.FixedSingle)
                {
                    c.ForeColor = fore;
                    c.BackColor = surfaceAlt;
                }

                ApplyThemeRecursive(c, surface, surfaceAlt, fore);
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
