//Copyright 2011-2012 Melvyn Laily
//http://arcanesanctum.net

//This file is part of NegativeScreen.

//This program is free software: you can redistribute it and/or modify
//it under the terms of the GNU General Public License as published by
//the Free Software Foundation, either version 3 of the License, or
//(at your option) any later version.

//This program is distributed in the hope that it will be useful,
//but WITHOUT ANY WARRANTY; without even the implied warranty of
//MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//GNU General Public License for more details.

//You should have received a copy of the GNU General Public License
//along with this program.  If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Drawing;
using System.Diagnostics;

namespace NegativeScreen
{
	/// <summary>
	/// inherits from Form so that hot keys can be bound to this "window"...
	/// </summary>
        class OverlayManager : Form
        {
		public const int HALT_HOTKEY_ID = 42;//random id =°
		public const int TOGGLE_HOTKEY_ID = 43;
		public const int RESET_TIMER_HOTKEY_ID = 44;
		public const int INCREASE_TIMER_HOTKEY_ID = 45;
		public const int DECREASE_TIMER_HOTKEY_ID = 46;

                public const int MODE1_HOTKEY_ID = 51;
		public const int MODE2_HOTKEY_ID = 52;
		public const int MODE3_HOTKEY_ID = 53;
		public const int MODE4_HOTKEY_ID = 54;
		public const int MODE5_HOTKEY_ID = 55;
		public const int MODE6_HOTKEY_ID = 56;
		public const int MODE7_HOTKEY_ID = 57;
		public const int MODE8_HOTKEY_ID = 58;
		public const int MODE9_HOTKEY_ID = 59;
		public const int MODE10_HOTKEY_ID = 60;

		private const int DEFAULT_INCREASE_STEP = 10;
		private const int DEFAULT_SLEEP_TIME = DEFAULT_INCREASE_STEP;
		private const int PAUSE_SLEEP_TIME = 100;

		/// <summary>
		/// control whether the main loop is paused or not.
		/// </summary>
		private bool mainLoopPaused = false;

		private int refreshInterval = DEFAULT_SLEEP_TIME;

		private List<NegativeOverlay> overlays = new List<NegativeOverlay>();

		private bool resolutionHasChanged = false;

                private NotifyIcon notifyIcon;
                private ContextMenuStrip contextMenu;
                private List<string> selectedMonitors;
                private List<string> selectedWindows;
                private EventHandler displaySettingsHandler;

                public OverlayManager(List<string> monitors, List<string> windows)
                {
                        this.selectedMonitors = new List<string>(monitors);
                        this.selectedWindows = new List<string>(windows);

                        contextMenu = new System.Windows.Forms.ContextMenuStrip();
                        foreach (var item in Screen.AllScreens)
                        {
                                string name = GetMonitorDetail(item);
                                ToolStripMenuItem menuItem = new ToolStripMenuItem(name, null, (s, e) =>
                                {
                                        SaveCurrentSelection();
                                        Initialization();
                                }) { CheckOnClick = true, Checked = this.selectedMonitors.Contains(item.DeviceName), Tag = item.DeviceName };
                                menuItem.CheckedChanged += (s, e) => SaveCurrentSelection();
                                contextMenu.Items.Add(menuItem);
                        }
                        contextMenu.Items.Add(new ToolStripMenuItem("Settings", null, (s, e) =>
                        {
                                Config cfg = Settings.Load();
                                cfg.Monitors = new List<string>(this.selectedMonitors);
                                cfg.Windows = new List<string>(this.selectedWindows);
                                cfg.StartMinimized = false;

                                SetOverlaysVisible(false);
                                try
                                {
                                        using (var form = new SettingsForm(cfg))
                                        {
                                                if (form.ShowDialog() == DialogResult.OK)
                                                {
                                                        this.selectedMonitors = form.Result.Monitors;
                                                        this.selectedWindows = form.Result.Windows;
                                                        Settings.Save(form.Result);
                                                        foreach (ToolStripItem item in this.contextMenu.Items)
                                                        {
                                                                ToolStripMenuItem mi = item as ToolStripMenuItem;
                                                                if (mi != null && mi.Tag != null)
                                                                        mi.Checked = this.selectedMonitors.Contains(mi.Tag.ToString());
                                                        }
                                                        Initialization();
                                                }
                                        }
                                }
                                finally
                                {
                                        SetOverlaysVisible(true);
                                }
                        }));
                        contextMenu.Items.Add(new ToolStripSeparator());
                        contextMenu.Items.Add(new ToolStripMenuItem("Exit", null, (s, e) =>
                        {
                                mainLoopPaused = false;
                                notifyIcon.Dispose();
                                this.Dispose();
                                Application.Exit();
                        }));
                        notifyIcon = new NotifyIcon();
                        notifyIcon.ContextMenuStrip = contextMenu;
			notifyIcon.Icon = new Icon(this.Icon, 32, 32);
			notifyIcon.Visible = true;
                        notifyIcon.DoubleClick += (s, e) =>
                        {
                            // Find and click the Settings menu item
                            foreach (ToolStripItem item in contextMenu.Items)
                            {
                                if (item.Text == "Settings")
                                {
                                    item.PerformClick();
                                    break;
                                }
                            }
                        };

			if (!NativeMethods.RegisterHotKey(this.Handle, HALT_HOTKEY_ID, KeyModifiers.MOD_WIN | KeyModifiers.MOD_ALT, Keys.H))
			{
				throw new Exception("RegisterHotKey(win+alt+H)", Marshal.GetExceptionForHR(Marshal.GetHRForLastWin32Error()));
			}
			if (!NativeMethods.RegisterHotKey(this.Handle, TOGGLE_HOTKEY_ID, KeyModifiers.MOD_WIN | KeyModifiers.MOD_ALT, Keys.N))
			{
				throw new Exception("RegisterHotKey(win+alt+N)", Marshal.GetExceptionForHR(Marshal.GetHRForLastWin32Error()));
			}
			if (!NativeMethods.RegisterHotKey(this.Handle, RESET_TIMER_HOTKEY_ID, KeyModifiers.MOD_WIN | KeyModifiers.MOD_ALT, Keys.Multiply))
			{
				throw new Exception("RegisterHotKey(win+alt+Multiply)", Marshal.GetExceptionForHR(Marshal.GetHRForLastWin32Error()));
			}
			if (!NativeMethods.RegisterHotKey(this.Handle, INCREASE_TIMER_HOTKEY_ID, KeyModifiers.MOD_WIN | KeyModifiers.MOD_ALT, Keys.Add))
			{
				throw new Exception("RegisterHotKey(win+alt+Add)", Marshal.GetExceptionForHR(Marshal.GetHRForLastWin32Error()));
			}
			if (!NativeMethods.RegisterHotKey(this.Handle, DECREASE_TIMER_HOTKEY_ID, KeyModifiers.MOD_WIN | KeyModifiers.MOD_ALT, Keys.Subtract))
			{
				throw new Exception("RegisterHotKey(win+alt+Substract)", Marshal.GetExceptionForHR(Marshal.GetHRForLastWin32Error()));
			}

			if (!NativeMethods.RegisterHotKey(this.Handle, MODE1_HOTKEY_ID, KeyModifiers.MOD_WIN | KeyModifiers.MOD_ALT, Keys.F1))
			{
				throw new Exception("RegisterHotKey(win+alt+F1)", Marshal.GetExceptionForHR(Marshal.GetHRForLastWin32Error()));
			}
			if (!NativeMethods.RegisterHotKey(this.Handle, MODE2_HOTKEY_ID, KeyModifiers.MOD_WIN | KeyModifiers.MOD_ALT, Keys.F2))
			{
				throw new Exception("RegisterHotKey(win+alt+F2)", Marshal.GetExceptionForHR(Marshal.GetHRForLastWin32Error()));
			}
			if (!NativeMethods.RegisterHotKey(this.Handle, MODE3_HOTKEY_ID, KeyModifiers.MOD_WIN | KeyModifiers.MOD_ALT, Keys.F3))
			{
				throw new Exception("RegisterHotKey(win+alt+F3)", Marshal.GetExceptionForHR(Marshal.GetHRForLastWin32Error()));
			}
			if (!NativeMethods.RegisterHotKey(this.Handle, MODE4_HOTKEY_ID, KeyModifiers.MOD_WIN | KeyModifiers.MOD_ALT, Keys.F4))
			{
				throw new Exception("RegisterHotKey(win+alt+F4)", Marshal.GetExceptionForHR(Marshal.GetHRForLastWin32Error()));
			}
			if (!NativeMethods.RegisterHotKey(this.Handle, MODE5_HOTKEY_ID, KeyModifiers.MOD_WIN | KeyModifiers.MOD_ALT, Keys.F5))
			{
				throw new Exception("RegisterHotKey(win+alt+F5)", Marshal.GetExceptionForHR(Marshal.GetHRForLastWin32Error()));
			}
			if (!NativeMethods.RegisterHotKey(this.Handle, MODE6_HOTKEY_ID, KeyModifiers.MOD_WIN | KeyModifiers.MOD_ALT, Keys.F6))
			{
				throw new Exception("RegisterHotKey(win+alt+F6)", Marshal.GetExceptionForHR(Marshal.GetHRForLastWin32Error()));
			}
			if (!NativeMethods.RegisterHotKey(this.Handle, MODE7_HOTKEY_ID, KeyModifiers.MOD_WIN | KeyModifiers.MOD_ALT, Keys.F7))
			{
				throw new Exception("RegisterHotKey(win+alt+F7)", Marshal.GetExceptionForHR(Marshal.GetHRForLastWin32Error()));
			}
			if (!NativeMethods.RegisterHotKey(this.Handle, MODE8_HOTKEY_ID, KeyModifiers.MOD_WIN | KeyModifiers.MOD_ALT, Keys.F8))
			{
				throw new Exception("RegisterHotKey(win+alt+F8)", Marshal.GetExceptionForHR(Marshal.GetHRForLastWin32Error()));
			}
			if (!NativeMethods.RegisterHotKey(this.Handle, MODE9_HOTKEY_ID, KeyModifiers.MOD_WIN | KeyModifiers.MOD_ALT, Keys.F9))
			{
				throw new Exception("RegisterHotKey(win+alt+F9)", Marshal.GetExceptionForHR(Marshal.GetHRForLastWin32Error()));
			}
			if (!NativeMethods.RegisterHotKey(this.Handle, MODE10_HOTKEY_ID, KeyModifiers.MOD_WIN | KeyModifiers.MOD_ALT, Keys.F10))
			{
				throw new Exception("RegisterHotKey(win+alt+F10)", Marshal.GetExceptionForHR(Marshal.GetHRForLastWin32Error()));
			}

			if (!NativeMethods.MagInitialize())
			{
				throw new Exception("MagInitialize()", Marshal.GetExceptionForHR(Marshal.GetHRForLastWin32Error()));
			}

                        displaySettingsHandler = new EventHandler(SystemEvents_DisplaySettingsChanged);
                        Microsoft.Win32.SystemEvents.DisplaySettingsChanged += displaySettingsHandler;

			Initialization();
		}

		void SystemEvents_DisplaySettingsChanged(object sender, EventArgs e)
		{
			Console.WriteLine(DateTime.Now.ToString());
			//we can't start the loop here, in the event handler, because it seems to block the next events
			resolutionHasChanged = true;
		}

                private void Initialization()
                {
                    // Dispose existing overlays
                    foreach (var item in overlays)
                    {
                        item.Dispose();
                    }
                    overlays = new List<NegativeOverlay>();

                    // Get current monitor configuration
                    var currentScreens = Screen.AllScreens.ToDictionary(s => s.DeviceName, s => s);
                    
                    // Update the context menu items to reflect current monitor configuration
                    var menuItems = this.contextMenu.Items
                        .OfType<ToolStripMenuItem>()
                        .Where(item => item.Tag != null)
                        .ToDictionary(item => item.Tag.ToString(), item => item);

                    // Clear existing monitor items except the last two (Settings and Exit)
                    for (int i = contextMenu.Items.Count - 1; i >= 0; i--)
                    {
                        if (contextMenu.Items[i] is ToolStripMenuItem menuItem && 
                            menuItem.Tag != null)
                        {
                            contextMenu.Items.RemoveAt(i);
                        }
                    }

                    // Rebuild monitor menu items with current configuration
                    int insertPos = 0;
                    foreach (var screen in Screen.AllScreens)
                    {
                        string monitorId = Settings.GetMonitorId(screen);
                        string friendlyName = Settings.GetMonitorFriendlyName(screen);
                        
                        // Check if this monitor was previously selected by either device name or ID
                        bool wasSelected = this.selectedMonitors.Contains(screen.DeviceName) || 
                                         this.selectedMonitors.Any(m => m == monitorId);
                        
                        var menuItem = new ToolStripMenuItem($"{friendlyName} ({screen.DeviceName})", null, 
                            (s, e) => 
                            {
                                SaveCurrentSelection();
                                Initialization();
                            }) 
                        { 
                            CheckOnClick = true, 
                            Checked = wasSelected, 
                            Tag = screen.DeviceName 
                        };
                        
                        menuItem.Checked = wasSelected;
                        menuItem.CheckedChanged += (s, e) => SaveCurrentSelection();
                        contextMenu.Items.Insert(insertPos++, menuItem);
                    }

                    // Save the updated selection
                    SaveCurrentSelection();

                    // Create overlays for selected monitors
                    foreach (var screen in Screen.AllScreens)
                    {
                        string monitorId = Settings.GetMonitorId(screen);
                        if (this.selectedMonitors.Contains(screen.DeviceName) || 
                            this.selectedMonitors.Any(m => m == monitorId))
                        {
                            overlays.Add(new NegativeOverlay(screen));
                        }
                    }

                    // Create window overlays
                    foreach (var win in selectedWindows)
                    {
                        IntPtr handle = FindWindowByKey(win);
                        if (handle != IntPtr.Zero)
                            overlays.Add(new NegativeOverlay(handle));
                    }

                    RefreshLoop(overlays);
                }

                private void SaveCurrentSelection()
                {
                    var selectedMonitorIds = new List<string>();
                    
                    // Get currently connected screens
                    var currentScreens = Screen.AllScreens.ToDictionary(s => s.DeviceName, s => s);
                    
                    // Save monitor selections
                    foreach (ToolStripItem item in this.contextMenu.Items)
                    {
                        if (item is ToolStripMenuItem menuItem && menuItem.Tag != null && menuItem.Checked)
                        {
                            string deviceName = menuItem.Tag.ToString();
                            if (currentScreens.TryGetValue(deviceName, out var screen))
                            {
                                // Save both device name and monitor ID for better reliability
                                selectedMonitorIds.Add(deviceName);
                                string monitorId = Settings.GetMonitorId(screen);
                                if (!selectedMonitorIds.Contains(monitorId))
                                {
                                    selectedMonitorIds.Add(monitorId);
                                }
                            }
                        }
                    }
                    
                    this.selectedMonitors = selectedMonitorIds.Distinct().ToList();
                    
                    // Save to settings
                    Config cfg = Settings.Load();
                    cfg.Monitors = new List<string>(this.selectedMonitors);
                    cfg.Windows = new List<string>(this.selectedWindows);
                    Settings.Save(cfg);
                }

                private static IntPtr FindWindowByKey(string key)
                {
                        string[] parts = key.Split('|');
                        if (parts.Length >= 2)
                        {
                                string proc = parts[0];
                                string title = parts[1];
                                foreach (var p in Process.GetProcessesByName(proc))
                                {
                                        if (p.MainWindowHandle != IntPtr.Zero && p.MainWindowTitle == title)
                                                return p.MainWindowHandle;
                                }
                        }
                        return IntPtr.Zero;
                }

		private void RefreshLoop(List<NegativeOverlay> overlays)
		{
			bool noError = true;
			while (noError)
			{

				if (resolutionHasChanged)
				{
					resolutionHasChanged = false;
					//if the screen configuration change, we try to reinitialize all the overlays.
					//we break the loop. the initialization method is called...
					break;
				}

				for (int i = 0; i < overlays.Count; i++)
				{
					noError = RefreshOverlay(overlays[i]);
					if (!noError)
					{
						//application is exiting
						break;
					}
				}

				//Process Window messages
				Application.DoEvents();

				if (this.refreshInterval > 0)
				{
					System.Threading.Thread.Sleep(this.refreshInterval);
				}

				//pause
				while (mainLoopPaused)
				{
					for (int i = 0; i < overlays.Count; i++)
					{
						overlays[i].Visible = false;
					}
					System.Threading.Thread.Sleep(PAUSE_SLEEP_TIME);
					Application.DoEvents();
					if (!mainLoopPaused)
					{
						for (int i = 0; i < overlays.Count; i++)
						{
							overlays[i].Visible = true;
						}
					}
				}
			}
			if (noError)
			{
				//the loop broke because of a screen resolution change
				Initialization();
			}
		}

		/// <summary>
		/// return true on success, false on failure.
		/// </summary>
		/// <returns></returns>
                private bool RefreshOverlay(NegativeOverlay overlay)
                {
                        try
                        {
                                overlay.UpdateBounds();
                                // Reclaim topmost status.
                                if (!NativeMethods.SetWindowPos(overlay.Handle, NativeMethods.HWND_TOPMOST, 0, 0, 0, 0,
                           (int)SetWindowPosFlags.SWP_NOACTIVATE | (int)SetWindowPosFlags.SWP_NOMOVE | (int)SetWindowPosFlags.SWP_NOSIZE))
                                {
					throw new Exception("SetWindowPos()", Marshal.GetExceptionForHR(Marshal.GetHRForLastWin32Error()));
				}
				// Force redraw.
				if (!NativeMethods.InvalidateRect(overlay.HwndMag, IntPtr.Zero, true))
				{
					throw new Exception("InvalidateRect()", Marshal.GetExceptionForHR(Marshal.GetHRForLastWin32Error()));
				}
				return true;
			}
			catch (ObjectDisposedException)
			{
				//application is exiting
				return false;
			}
			catch (Exception)
			{
				throw;
			}
		}

                private void UnregisterHotKeys()
                {
                        NativeMethods.UnregisterHotKey(this.Handle, HALT_HOTKEY_ID);
			NativeMethods.UnregisterHotKey(this.Handle, TOGGLE_HOTKEY_ID);
			NativeMethods.UnregisterHotKey(this.Handle, RESET_TIMER_HOTKEY_ID);
			NativeMethods.UnregisterHotKey(this.Handle, INCREASE_TIMER_HOTKEY_ID);
			NativeMethods.UnregisterHotKey(this.Handle, DECREASE_TIMER_HOTKEY_ID);

			NativeMethods.UnregisterHotKey(this.Handle, MODE1_HOTKEY_ID);
			NativeMethods.UnregisterHotKey(this.Handle, MODE2_HOTKEY_ID);
			NativeMethods.UnregisterHotKey(this.Handle, MODE3_HOTKEY_ID);
			NativeMethods.UnregisterHotKey(this.Handle, MODE4_HOTKEY_ID);
			NativeMethods.UnregisterHotKey(this.Handle, MODE5_HOTKEY_ID);
			NativeMethods.UnregisterHotKey(this.Handle, MODE6_HOTKEY_ID);
			NativeMethods.UnregisterHotKey(this.Handle, MODE7_HOTKEY_ID);
			NativeMethods.UnregisterHotKey(this.Handle, MODE8_HOTKEY_ID);
			NativeMethods.UnregisterHotKey(this.Handle, MODE9_HOTKEY_ID);
                        NativeMethods.UnregisterHotKey(this.Handle, MODE10_HOTKEY_ID);
                }

                private void SetOverlaysVisible(bool visible)
                {
                        foreach (var ov in overlays)
                        {
                                ov.Visible = visible;
                        }
                }

		protected override void WndProc(ref Message m)
		{
			// Listen for operating system messages.
			switch (m.Msg)
			{
				case (int)WindowMessage.WM_DWMCOMPOSITIONCHANGED:
					//aero has been enabled/disabled. It causes the magnified control to stop working
					if (Environment.OSVersion.Version.Major == 6 && Environment.OSVersion.Version.Minor == 0)
					{
						//running Vista.
						//The creation of the magnification Window on this OS seems to change desktop composition,
						//leading to infinite loop
					}
					else
					{
						Initialization();
					}
					break;
				case (int)WindowMessage.WM_HOTKEY:
					switch ((int)m.WParam)
					{
						case HALT_HOTKEY_ID:
							//otherwise, if paused, the application never stops
							mainLoopPaused = false;
							notifyIcon.Dispose();
							this.Dispose();
							Application.Exit();
							break;
						case TOGGLE_HOTKEY_ID:
							this.mainLoopPaused = !mainLoopPaused;
							break;
						case RESET_TIMER_HOTKEY_ID:
							this.refreshInterval = DEFAULT_SLEEP_TIME;
							break;
						case INCREASE_TIMER_HOTKEY_ID:
							this.refreshInterval += DEFAULT_INCREASE_STEP;
							break;
						case DECREASE_TIMER_HOTKEY_ID:
							this.refreshInterval -= DEFAULT_INCREASE_STEP;
							if (this.refreshInterval < 0)
							{
								this.refreshInterval = 0;
							}
							break;
						case MODE1_HOTKEY_ID:
							BuiltinMatrices.ChangeColorEffect(overlays, BuiltinMatrices.Negative);
							break;
						case MODE2_HOTKEY_ID:
							BuiltinMatrices.ChangeColorEffect(overlays, BuiltinMatrices.NegativeHueShift180);
							break;
						case MODE3_HOTKEY_ID:
							BuiltinMatrices.ChangeColorEffect(overlays, BuiltinMatrices.NegativeHueShift180Variation1);
							break;
						case MODE4_HOTKEY_ID:
							BuiltinMatrices.ChangeColorEffect(overlays, BuiltinMatrices.NegativeHueShift180Variation2);
							break;
						case MODE5_HOTKEY_ID:
							BuiltinMatrices.ChangeColorEffect(overlays, BuiltinMatrices.NegativeHueShift180Variation3);
							break;
						case MODE6_HOTKEY_ID:
							BuiltinMatrices.ChangeColorEffect(overlays, BuiltinMatrices.NegativeHueShift180Variation4);
							break;
						case MODE7_HOTKEY_ID:
							BuiltinMatrices.ChangeColorEffect(overlays, BuiltinMatrices.NegativeSepia);
							break;
						case MODE8_HOTKEY_ID:
							BuiltinMatrices.ChangeColorEffect(overlays, BuiltinMatrices.NegativeGrayScale);
							break;
						case MODE9_HOTKEY_ID:
							BuiltinMatrices.ChangeColorEffect(overlays, BuiltinMatrices.NegativeRed);
							break;
						case MODE10_HOTKEY_ID:
							BuiltinMatrices.ChangeColorEffect(overlays, BuiltinMatrices.Red);
							break;
						default:
							break;
					}
					break;
			}
			base.WndProc(ref m);
		}

                protected override void Dispose(bool disposing)
                {
                        UnregisterHotKeys();
                        if (displaySettingsHandler != null)
                                Microsoft.Win32.SystemEvents.DisplaySettingsChanged -= displaySettingsHandler;
                        foreach (var ov in overlays)
                                ov.Dispose();
                        overlays.Clear();
                        NativeMethods.MagUninitialize();
                        base.Dispose(disposing);
                }

                internal static string GetMonitorDetail(Screen screen)
                {
                        int index = Array.IndexOf(Screen.AllScreens, screen) + 1;
                        Config cfg = Settings.Load();
                        string id = Settings.GetMonitorId(screen);
                        string alias = null;
                        if (cfg.MonitorLabels != null)
                        {
                                foreach (var ml in cfg.MonitorLabels)
                                {
                                        if ((!string.IsNullOrEmpty(ml.Id) && ml.Id == id) || ml.Device == screen.DeviceName)
                                        {
                                                alias = ml.Label;
                                                break;
                                        }
                                }
                        }
                        string name = alias;
                        if (string.IsNullOrEmpty(name))
                        {
                                NativeMethods.DISPLAY_DEVICE device = new NativeMethods.DISPLAY_DEVICE();
                                device.cb = Marshal.SizeOf(typeof(NativeMethods.DISPLAY_DEVICE));
                                if (NativeMethods.EnumDisplayDevices(screen.DeviceName, 0, ref device, 0))
                                {
                                        if (!string.IsNullOrEmpty(device.DeviceString))
                                                name = device.DeviceString.Trim();
                                }
                        }
                        if (string.IsNullOrEmpty(name))
                                name = screen.DeviceName;
                        return $"Display {index} - {name} [{id}] ({screen.Bounds.Width}x{screen.Bounds.Height})";
                }

                internal static string GetMonitorName(Screen screen)
                {
                        NativeMethods.DISPLAY_DEVICE device = new NativeMethods.DISPLAY_DEVICE();
                        device.cb = Marshal.SizeOf(typeof(NativeMethods.DISPLAY_DEVICE));
                        if (NativeMethods.EnumDisplayDevices(screen.DeviceName, 0, ref device, 0))
                        {
                                if (!string.IsNullOrEmpty(device.DeviceString))
                                {
                                        return device.DeviceString.Trim();
                                }
                        }
                        return screen.DeviceName;
                }

	}
}
