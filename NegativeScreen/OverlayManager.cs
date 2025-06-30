using System;
using System.Drawing;
using System.Windows.Forms;
using System.Linq;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.ComponentModel;  // Add this for Win32Exception
using NegativeScreen;
using System.Drawing.Imaging;

#pragma warning disable CA1416 // Validate platform compatibility

unsafe class OverlayManager : Form
{
    private bool exiting = false;
    private int RefreshTime = 10;
    private NotifyIcon notifyIcon;
    private float[,] currentMatrix = BuiltinMatrices.Negative;
    private Dictionary<IntPtr, bool> invertedWindows = new Dictionary<IntPtr, bool>();
    private Dictionary<IntPtr, DateTime> lastCheckTime = new Dictionary<IntPtr, DateTime>();
    private const int CHECK_INTERVAL_MS = 1000; // Check window brightness every second
    public const int HALT_HOTKEY_ID = 42;
    public const int WINDOW_TOGGLE_HOTKEY_ID = 43;
    private bool mainLoopPaused = false;
    private const double BRIGHTNESS_THRESHOLD = 0.3; // Lower threshold to detect dark windows
    private const int SAMPLE_STEP = 20;  // Increased step for better performance
    private Dictionary<IntPtr, Form> windowOverlays = new Dictionary<IntPtr, Form>();

    private bool _NegativeEnabled;
    private bool NegativeEnabled
    {
        get => _NegativeEnabled;
        set
        {
            if (_NegativeEnabled == value) return;
            _NegativeEnabled = value;
            
            // Only apply to specific windows, not globally
            foreach (var window in invertedWindows.Keys.ToList())
            {
                if (_NegativeEnabled)
                {
                    ApplyInversion(window);
                }
                else
                {
                    RemoveInversion(window);
                }
            }
        }
    }

    public OverlayManager()
    {
        // Make the form invisible but still process messages
        this.ShowInTaskbar = false;
        this.Visible = false;
        this.WindowState = FormWindowState.Minimized;
        
        Console.WriteLine("Initializing OverlayManager...");
        this.notifyIcon = new NotifyIcon();
        this.notifyIcon.Icon = new Icon("Icon.ico");
        this.notifyIcon.Visible = true;
        this.notifyIcon.Text = "NegativeScreen";

        // Add context menu with more options
        var contextMenu = new ContextMenuStrip();
        contextMenu.Items.Clear();  // Clear any existing items
        
        var invertCurrentItem = new ToolStripMenuItem("Invert Current Window");
        invertCurrentItem.Click += (s, e) => {
            Console.WriteLine("Manual inversion requested for current window");
            ForceInvertCurrentWindow();
        };
        contextMenu.Items.Add(invertCurrentItem);

        var restoreCurrentItem = new ToolStripMenuItem("Restore Current Window");
        restoreCurrentItem.Click += (s, e) => {
            Console.WriteLine("Manual restore requested for current window");
            ForceRestoreCurrentWindow();
        };
        contextMenu.Items.Add(restoreCurrentItem);

        contextMenu.Items.Add(new ToolStripSeparator());

        var exitItem = new ToolStripMenuItem("Exit");
        exitItem.Click += ExitApplication;
        contextMenu.Items.Add(exitItem);

<<<<<<< HEAD
        // Ensure menu is assigned to notifyIcon
        this.notifyIcon.ContextMenuStrip = contextMenu;
        
        // Add double-click handler
        this.notifyIcon.DoubleClick += (s, e) => ForceInvertCurrentWindow();
        
        Console.WriteLine("Tray icon and menu initialized");
=======
		private NotifyIcon notifyIcon;
		private ContextMenuStrip contextMenu;

		public OverlayManager()
		{
			contextMenu = new System.Windows.Forms.ContextMenuStrip();
			foreach (var item in Screen.AllScreens)
			{
				contextMenu.Items.Add(new ToolStripMenuItem(item.DeviceName, null, (s, e) =>
				{
					Initialization();
				}) { CheckOnClick = true, Checked = true });
			}
			notifyIcon = new NotifyIcon();
			notifyIcon.ContextMenuStrip = contextMenu;
			notifyIcon.Icon = new Icon(this.Icon, 32, 32);
			notifyIcon.Visible = true;

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
>>>>>>> custom_multi_monitor_support

        // Register hotkeys
        if (!NativeMethods.RegisterHotKey(this.Handle, HALT_HOTKEY_ID, 
            KeyModifiers.MOD_CONTROL | KeyModifiers.MOD_ALT | KeyModifiers.MOD_SHIFT, Keys.H))
        {
            throw new Exception("RegisterHotKey(Ctrl+Alt+Shift+H)", 
                Marshal.GetExceptionForHR(Marshal.GetHRForLastWin32Error()));
        }

        if (!NativeMethods.RegisterHotKey(this.Handle, WINDOW_TOGGLE_HOTKEY_ID, 
            KeyModifiers.MOD_CONTROL | KeyModifiers.MOD_ALT | KeyModifiers.MOD_SHIFT, Keys.W))
        {
            throw new Exception("RegisterHotKey(Ctrl+Alt+Shift+W)", 
                Marshal.GetExceptionForHR(Marshal.GetHRForLastWin32Error()));
        }

        // Move Initialization to OnLoad event
        this.Load += (s, e) => Initialization();
    }

    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        // Hide the form but keep it running
        this.Hide();
    }

    private void UnregisterHotKeys()
    {
        try
        {
            NativeMethods.UnregisterHotKey(this.Handle, HALT_HOTKEY_ID);
            NativeMethods.UnregisterHotKey(this.Handle, WINDOW_TOGGLE_HOTKEY_ID);
        }
        catch (Exception) { }
    }

<<<<<<< HEAD
    protected override void WndProc(ref Message m)
    {
        switch (m.Msg)
        {
            case (int)WindowMessage.WM_HOTKEY:
                switch ((int)m.WParam)
                {
                    case HALT_HOTKEY_ID:
                        mainLoopPaused = !mainLoopPaused;
                        break;
                    case WINDOW_TOGGLE_HOTKEY_ID:
                        ToggleCurrentWindow();
                        break;
                    default:
                        break;
                }
                break;
=======
		private void Initialization()
		{
			foreach (var item in overlays)
			{
				item.Dispose();
			}
			overlays = new List<NegativeOverlay>();
			foreach (var item in Screen.AllScreens)
			{
				foreach (ToolStripMenuItem menuItem in this.contextMenu.Items)
				{
					if (menuItem.Text == item.DeviceName && menuItem.Checked)
					{
						overlays.Add(new NegativeOverlay(item));
					}
				}
			}
			RefreshLoop(overlays);
		}
>>>>>>> custom_multi_monitor_support

            // Add foreground window change detection
            case (int)WindowMessage.WM_ACTIVATEAPP:
            case (int)WindowMessage.WM_ACTIVATE:
            case (int)WindowMessage.WM_SETFOCUS:
                HandleWindowFocusChange();
                break;

            default:
                base.WndProc(ref m);
                break;
        }
    }

    private void HandleWindowFocusChange()
    {
        IntPtr focusedWindow = NativeMethods.GetForegroundWindow();
        Console.WriteLine($"\nWindow focus changed to: {focusedWindow}");
        
        if (focusedWindow != IntPtr.Zero && !IsSystemWindow(focusedWindow))
        {
            // Skip brightness check if the window is already being managed
            if (invertedWindows.ContainsKey(focusedWindow))
            {
                Console.WriteLine("Window is already managed - skipping brightness check");
                return;
            }

            try
            {
                double brightness = GetWindowBrightness(focusedWindow);
                bool isDark = brightness < BRIGHTNESS_THRESHOLD;
                Console.WriteLine($"Window {focusedWindow}: Brightness = {brightness:F3} ({(isDark ? "Dark" : "Bright")})");

                // Only apply automatic inversion for bright windows
                if (!isDark)
                {
                    Console.WriteLine("Window is bright - inverting colors");
                    ApplyInversion(focusedWindow);
                    invertedWindows[focusedWindow] = true;
                }
                
                lastCheckTime[focusedWindow] = DateTime.Now;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error handling window {focusedWindow}: {ex.Message}");
            }
        }
    }

    private bool IsSystemWindow(IntPtr hwnd)
    {
        // Just check desktop window - simpler approach
        return hwnd == NativeMethods.GetDesktopWindow();
    }

    private void ExitApplication(object sender, EventArgs e)
    {
        // Clean up inversions before exiting
        foreach (var window in invertedWindows.Keys.ToList())
        {
            if (invertedWindows[window])
            {
                RemoveInversion(window);
            }
        }

        exiting = true;
        mainLoopPaused = true;
        UnregisterHotKeys();
        notifyIcon.Visible = false;
        notifyIcon.Dispose();
        Application.Exit();
    }

    private void Initialization()
    {
        // First initialize the Magnification API
        if (!NativeMethods.MagInitialize())
        {
            throw new Win32Exception(Marshal.GetLastWin32Error());
        }

<<<<<<< HEAD
        Console.WriteLine("Testing color effect matrix...");
        
        try
        {
            // Quick test of the color effect matrix
            var effect = new ColorEffect(BuiltinMatrices.Negative);
            if (!NativeMethods.MagSetFullscreenColorEffect(ref effect))
            {
                int error = Marshal.GetLastWin32Error();
                throw new Win32Exception(error, $"Full screen color effect test failed. Error code: {error}");
            }
=======
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
>>>>>>> custom_multi_monitor_support

            // Immediately reset to normal - don't wait
            effect = new ColorEffect(BuiltinMatrices.Identity);
            if (!NativeMethods.MagSetFullscreenColorEffect(ref effect))
            {
                throw new Win32Exception(Marshal.GetLastWin32Error(), "Failed to reset color effect");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Color effect test failed: {ex.Message}");
            // Try one more time to reset the effect
            try
            {
                var resetEffect = new ColorEffect(BuiltinMatrices.Identity);
                NativeMethods.MagSetFullscreenColorEffect(ref resetEffect);
            }
            catch { }
            throw; // Re-throw the original exception
        }

        Console.WriteLine("Color effect matrix test successful");

        _NegativeEnabled = true;
        Console.WriteLine("Starting application loop...");
        
        // Start refresh loop in a background thread
        System.Threading.Thread refreshThread = new System.Threading.Thread(() =>
        {
            try
            {
                RefreshLoop();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Refresh loop error: {ex.Message}");
            }
        });
        refreshThread.IsBackground = true;
        refreshThread.Start();
    }

    private void ToggleCurrentWindow()
    {
        IntPtr currentWindow = NativeMethods.GetForegroundWindow();
        if (currentWindow != IntPtr.Zero)
        {
            // Force a new brightness check
            HandleWindowFocusChange();
        }
    }

    private void ForceInvertCurrentWindow()
    {
        IntPtr currentWindow = NativeMethods.GetForegroundWindow();
        if (currentWindow != IntPtr.Zero && !IsSystemWindow(currentWindow))
        {
            Console.WriteLine($"Forcing inversion of window {currentWindow}");
            
            // Always toggle the current state, regardless of brightness
            bool currentState = invertedWindows.ContainsKey(currentWindow) && invertedWindows[currentWindow];
            if (currentState)
            {
                RemoveInversion(currentWindow);
                invertedWindows[currentWindow] = false;
                Console.WriteLine("Manual inversion removed");
            }
            else
            {
                ApplyInversion(currentWindow);
                invertedWindows[currentWindow] = true;
                Console.WriteLine("Manual inversion applied");
            }
            
            // Prevent automatic changes by setting a "permanent" timestamp
            lastCheckTime[currentWindow] = DateTime.MaxValue;
        }
        else
        {
            Console.WriteLine("No valid window to invert");
        }
    }

    private void ForceRestoreCurrentWindow()
    {
        IntPtr currentWindow = NativeMethods.GetForegroundWindow();
        if (currentWindow != IntPtr.Zero)
        {
            Console.WriteLine($"Forcing restore of window {currentWindow}");
            RemoveInversion(currentWindow);
            invertedWindows[currentWindow] = false;
            lastCheckTime[currentWindow] = DateTime.Now;
            Console.WriteLine("Restore completed successfully");
        }
        else
        {
            Console.WriteLine("No valid window to restore");
        }
    }

    public void ShowBalloonTip(int timeout, string title, string text, ToolTipIcon icon)
    {
        if (notifyIcon != null)
        {
            notifyIcon.ShowBalloonTip(timeout, title, text, icon);
        }
    }

    public void Toggle()
    {
        foreach (var window in invertedWindows.Keys.ToList())
        {
            invertedWindows[window] = !invertedWindows[window];
        }
    }

    public void Enable()
    {
        foreach (var window in invertedWindows.Keys.ToList())
        {
            invertedWindows[window] = true;
        }
    }

    public void Disable()
    {
        foreach (var window in invertedWindows.Keys.ToList())
        {
            invertedWindows[window] = false;
        }
    }

    public bool TrySetColorEffectByName(string name)
    {
        if (BuiltinMatrices.TryGetMatrix(name, out float[,] matrix))
        {
            currentMatrix = matrix;
            return true;
        }
        return false;
    }

    private void ApplyInversion(IntPtr hwnd)
    {
        try
        {
            Console.WriteLine($"Applying inversion to window {hwnd}");
            
            // Create color effect with our inversion matrix
            var effect = new ColorEffect(currentMatrix);
            
            // Apply using Magnification API directly
            if (!NativeMethods.MagInitialize())
            {
                throw new Exception("Failed to initialize magnification API");
            }

            bool success = NativeMethods.MagSetColorEffect(hwnd, ref effect);
            
            if (!success)
            {
                int error = Marshal.GetLastWin32Error();
                if (error != 0)
                {
                    throw new Win32Exception(error);
                }
            }

            Console.WriteLine("Color inversion applied successfully");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in ApplyInversion: {ex.Message}");
            throw;
        }
    }

    private void RemoveInversion(IntPtr hwnd)
    {
        try
        {
            Console.WriteLine($"Removing inversion from window {hwnd}");
            
            // Reset to identity matrix
            var effect = new ColorEffect(BuiltinMatrices.Identity);
            
            bool success = NativeMethods.MagSetColorEffect(hwnd, ref effect);
            
            if (!success)
            {
                int error = Marshal.GetLastWin32Error();
                if (error != 0)
                {
                    throw new Win32Exception(error);
                }
            }

            Console.WriteLine("Color inversion removed successfully");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in RemoveInversion: {ex.Message}");
            throw;
        }
    }

    private void RefreshLoop()
    {
        Console.WriteLine("Refresh loop started");
        while (!exiting)
        {
            try
            {
                if (!mainLoopPaused)
                {
                    // Process each window
                    foreach (var window in invertedWindows.ToList())
                    {
                        if (NativeMethods.IsWindow(window.Key))
                        {
                            ProcessWindow(window.Key);
                        }
                        else
                        {
                            Console.WriteLine($"Removing invalid window {window.Key}");
                            invertedWindows.Remove(window.Key);
                            lastCheckTime.Remove(window.Key);
                        }
                    }
                }

                // Sleep to avoid high CPU usage
                System.Threading.Thread.Sleep(RefreshTime);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in refresh loop: {ex.Message}");
                System.Threading.Thread.Sleep(1000); // Wait a bit before retrying
            }
        }
        Console.WriteLine("Refresh loop ended");
    }

    private void ProcessWindow(IntPtr hwnd)
    {
        // Skip processing if this is a manually inverted window
        if (lastCheckTime.ContainsKey(hwnd) && lastCheckTime[hwnd] == DateTime.MaxValue)
        {
            return;
        }

        if (!lastCheckTime.ContainsKey(hwnd) || 
            (DateTime.Now - lastCheckTime[hwnd]).TotalMilliseconds > CHECK_INTERVAL_MS)
        {
            bool isInverted = invertedWindows.ContainsKey(hwnd) && invertedWindows[hwnd];
            double brightness = GetWindowBrightness(hwnd);
            bool isDark = brightness < BRIGHTNESS_THRESHOLD;
            
            Console.WriteLine($"Processing window {hwnd}: Brightness = {brightness:F3} ({(isDark ? "Dark" : "Bright")})");
            
            if (!isDark && !isInverted)
            {
                ApplyInversion(hwnd);
                invertedWindows[hwnd] = true;
            }
            else if (isDark && isInverted)
            {
                RemoveInversion(hwnd);
                invertedWindows[hwnd] = false;
            }
            
            lastCheckTime[hwnd] = DateTime.Now;
        }
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        // Clean up all inversions
        foreach (var window in invertedWindows.Keys.ToList())
        {
            if (invertedWindows[window])
            {
                try
                {
                    RemoveInversion(window);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error cleaning up window {window}: {ex.Message}");
                }
            }
        }

        // Clean up Magnification API
        try
        {
            NativeMethods.MagUninitialize();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error uninitializing magnification API: {ex.Message}");
        }

        // Clean up overlays
        foreach (var overlay in windowOverlays.Values)
        {
            overlay.Close();
            overlay.Dispose();
        }
        windowOverlays.Clear();

        exiting = true;
        base.OnFormClosing(e);
    }

    private double GetWindowBrightness(IntPtr hwnd)
    {
        try
        {
            if (NativeMethods.GetWindowRect(hwnd, out RECT rect))
            {
                int width = rect.right - rect.left;
                int height = rect.bottom - rect.top;

                if (width <= 0 || height <= 0)
                {
                    Console.WriteLine($"Invalid window size for {hwnd}: {width}x{height}");
                    return 0;
                }

                using (var bitmap = new Bitmap(width, height))
                using (var g = Graphics.FromImage(bitmap))
                {
                    g.CopyFromScreen(rect.left, rect.top, 0, 0, bitmap.Size);
                    return CalculateAverageBrightness(bitmap);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting window brightness for {hwnd}: {ex.Message}");
        }
        return 0;
    }

    private bool IsWindowDark(IntPtr hwnd)
    {
        return GetWindowBrightness(hwnd) < BRIGHTNESS_THRESHOLD;
    }

    private double CalculateAverageBrightness(Bitmap bitmap)
    {
        long totalBrightness = 0;
        int sampledPixels = 0;

        // Use LockBits for faster pixel access
        var bitmapData = bitmap.LockBits(
            new Rectangle(0, 0, bitmap.Width, bitmap.Height),
            System.Drawing.Imaging.ImageLockMode.ReadOnly,
            System.Drawing.Imaging.PixelFormat.Format32bppArgb);

        try
        {
            unsafe
            {
                byte* ptr = (byte*)bitmapData.Scan0;

                for (int y = 0; y < bitmap.Height; y += SAMPLE_STEP)
                {
                    for (int x = 0; x < bitmap.Width; x += SAMPLE_STEP)
                    {
                        int offset = y * bitmapData.Stride + x * 4;
                        byte b = ptr[offset];
                        byte g = ptr[offset + 1];
                        byte r = ptr[offset + 2];
                        
                        totalBrightness += (r + g + b) / 3;
                        sampledPixels++;
                    }
                }
            }
        }
        finally
        {
            bitmap.UnlockBits(bitmapData);
        }

        return sampledPixels > 0 ? (totalBrightness / (double)(sampledPixels * 255)) : 0;
    }

    // ...existing code...
}
