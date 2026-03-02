using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;
using System.Windows.Forms;
using System.Runtime.InteropServices;

namespace NegativeScreen
{
    [Serializable]
    public class Config
    {
        public List<string> Monitors = new List<string>();
        public List<string> Windows = new List<string>();
        public bool StartMinimized = false;
        public bool DarkMode = true;
        public bool UseMagnifiedCursor = true;
        public bool ForceSoftwareCursor = false;
        public bool NormalizeCursorScheme = true;
        public bool AutoInvertByBrightness = false;
        public int AutoInvertSampleMs = 1000;
        public double AutoInvertBrightThreshold = 0.65;
        public double AutoInvertDarkThreshold = 0.45;
        public int AutoInvertBrightDwellMs = 3000;
        public int AutoInvertDarkDwellMs = 6000;
        public int AutoInvertMinHoldMs = 5000;
        public int AutoInvertRequiredSamples = 3;
        public double AutoInvertSmoothingAlpha = 0.2;
        public double AutoInvertBrightPixelThreshold = 0.8;
        public double AutoInvertBrightCoverageThreshold = 0.35;
        public double AutoInvertDarkCoverageThreshold = 0.15;
        public bool AutoInvertHideOverlays = false;
        public List<MonitorLabel> MonitorLabels = new List<MonitorLabel>();
    }

    [Serializable]
    public class MonitorLabel
    {
        public string Device;
        public string Id;
        public string Label;
    }

    static class Settings
    {
        private static readonly string ConfigPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "settings.xml");

        public static Config Load()
        {
            Config cfg = null;
            bool hasCursorSetting = false;
            bool hasSoftwareCursorSetting = false;
            bool hasNormalizeCursorSchemeSetting = false;
            bool hasAutoInvertSetting = false;
            bool hasAutoInvertAdvancedSettings = false;
            if (File.Exists(ConfigPath))
            {
                try
                {
                    try
                    {
                        string xml = File.ReadAllText(ConfigPath);
                        hasCursorSetting = xml.IndexOf("<UseMagnifiedCursor>", StringComparison.OrdinalIgnoreCase) >= 0;
                        hasSoftwareCursorSetting = xml.IndexOf("<ForceSoftwareCursor>", StringComparison.OrdinalIgnoreCase) >= 0;
                        hasNormalizeCursorSchemeSetting = xml.IndexOf("<NormalizeCursorScheme>", StringComparison.OrdinalIgnoreCase) >= 0;
                        hasAutoInvertSetting = xml.IndexOf("<AutoInvertByBrightness>", StringComparison.OrdinalIgnoreCase) >= 0;
                        hasAutoInvertAdvancedSettings = xml.IndexOf("<AutoInvertBrightDwellMs>", StringComparison.OrdinalIgnoreCase) >= 0;
                    }
                    catch
                    {
                        hasCursorSetting = false;
                        hasSoftwareCursorSetting = false;
                        hasNormalizeCursorSchemeSetting = false;
                        hasAutoInvertSetting = false;
                        hasAutoInvertAdvancedSettings = false;
                    }
                    XmlSerializer xs = new XmlSerializer(typeof(Config));
                    using (FileStream fs = new FileStream(ConfigPath, FileMode.Open))
                    {
                        cfg = (Config)xs.Deserialize(fs);
                    }
                }
                catch { cfg = null; }
            }
            if (cfg == null)
            {
                cfg = new Config();
                cfg.DarkMode = true;
                cfg.UseMagnifiedCursor = true;
                cfg.ForceSoftwareCursor = false;
                cfg.NormalizeCursorScheme = true;
                cfg.AutoInvertByBrightness = false;
                cfg.AutoInvertSampleMs = 1000;
                cfg.AutoInvertBrightThreshold = 0.65;
                cfg.AutoInvertDarkThreshold = 0.45;
                cfg.AutoInvertBrightDwellMs = 3000;
                cfg.AutoInvertDarkDwellMs = 6000;
                cfg.AutoInvertMinHoldMs = 5000;
                cfg.AutoInvertRequiredSamples = 3;
                cfg.AutoInvertSmoothingAlpha = 0.2;
                cfg.AutoInvertBrightPixelThreshold = 0.8;
                cfg.AutoInvertBrightCoverageThreshold = 0.35;
                cfg.AutoInvertDarkCoverageThreshold = 0.15;
                cfg.AutoInvertHideOverlays = false;
            }
            else if (!hasCursorSetting)
            {
                // Default to magnified cursor for older config files.
                cfg.UseMagnifiedCursor = true;
            }
            if (!hasSoftwareCursorSetting)
            {
                cfg.ForceSoftwareCursor = false;
            }
            if (!hasNormalizeCursorSchemeSetting)
            {
                cfg.NormalizeCursorScheme = true;
            }
            if (!hasAutoInvertSetting)
            {
                cfg.AutoInvertByBrightness = false;
                cfg.AutoInvertSampleMs = 1000;
                cfg.AutoInvertBrightThreshold = 0.65;
                cfg.AutoInvertDarkThreshold = 0.45;
                cfg.AutoInvertBrightDwellMs = 3000;
                cfg.AutoInvertDarkDwellMs = 6000;
                cfg.AutoInvertMinHoldMs = 5000;
                cfg.AutoInvertRequiredSamples = 3;
                cfg.AutoInvertSmoothingAlpha = 0.2;
                cfg.AutoInvertBrightPixelThreshold = 0.8;
                cfg.AutoInvertBrightCoverageThreshold = 0.35;
                cfg.AutoInvertDarkCoverageThreshold = 0.15;
                cfg.AutoInvertHideOverlays = false;
            }
            else if (!hasAutoInvertAdvancedSettings)
            {
                cfg.AutoInvertBrightDwellMs = 3000;
                cfg.AutoInvertDarkDwellMs = 6000;
                cfg.AutoInvertMinHoldMs = 5000;
                cfg.AutoInvertRequiredSamples = 3;
                cfg.AutoInvertSmoothingAlpha = 0.2;
                cfg.AutoInvertBrightPixelThreshold = 0.8;
                cfg.AutoInvertBrightCoverageThreshold = 0.35;
                cfg.AutoInvertDarkCoverageThreshold = 0.15;
            }

            // Only add all monitors if this is a new config
            if (cfg.Monitors.Count == 0)
            {
                foreach (var screen in Screen.AllScreens)
                {
                    cfg.Monitors.Add(screen.DeviceName);
                }
            }

            // Update monitor labels for all screens
            foreach (var screen in Screen.AllScreens)
            {
                string id = GetMonitorId(screen);
                var ml = cfg.MonitorLabels.Find(m => (!string.IsNullOrEmpty(m.Id) && m.Id == id) || m.Device == screen.DeviceName);
                if (ml == null)
                {
                    cfg.MonitorLabels.Add(new MonitorLabel { Device = screen.DeviceName, Id = id, Label = GetMonitorFriendlyName(screen) });
                }
                else
                {
                    ml.Device = screen.DeviceName;
                    ml.Id = id;
                    if (string.IsNullOrEmpty(ml.Label))
                        ml.Label = GetMonitorFriendlyName(screen);
                }
            }

            return cfg;
        }

        public static void Save(Config config)
        {
            try
            {
                XmlSerializer xs = new XmlSerializer(typeof(Config));
                using (FileStream fs = new FileStream(ConfigPath, FileMode.Create))
                {
                    xs.Serialize(fs, config);
                }
            }
            catch { }
        }

        internal static string GetMonitorFriendlyName(Screen screen)
        {
            NativeMethods.DISPLAY_DEVICE device = new NativeMethods.DISPLAY_DEVICE();
            device.cb = Marshal.SizeOf(typeof(NativeMethods.DISPLAY_DEVICE));
            if (NativeMethods.EnumDisplayDevices(screen.DeviceName, 0, ref device, 0))
            {
                if (!string.IsNullOrEmpty(device.DeviceString))
                    return device.DeviceString.Trim();
            }
            return screen.DeviceName;
        }

        public static string GetMonitorId(Screen screen)
        {
            NativeMethods.DISPLAY_DEVICE device = new NativeMethods.DISPLAY_DEVICE();
            device.cb = Marshal.SizeOf(typeof(NativeMethods.DISPLAY_DEVICE));
            if (NativeMethods.EnumDisplayDevices(screen.DeviceName, 0, ref device, 0))
            {
                if (!string.IsNullOrEmpty(device.DeviceID))
                    return device.DeviceID.Trim();
            }
            return screen.DeviceName;
        }
    }
}
