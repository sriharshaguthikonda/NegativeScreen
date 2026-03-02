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
        public int AutoInvertSampleMs = 500;
        public double AutoInvertBrightThreshold = 0.65;
        public double AutoInvertDarkThreshold = 0.45;
        public int AutoInvertBrightDwellMs = 1500;
        public int AutoInvertDarkDwellMs = 6000;
        public int AutoInvertMinHoldMs = 2000;
        public int AutoInvertRequiredSamples = 2;
        public double AutoInvertSmoothingAlpha = 0.35;
        public double AutoInvertBrightPixelThreshold = 0.8;
        public double AutoInvertBrightCoverageThreshold = 0.35;
        public double AutoInvertDarkCoverageThreshold = 0.15;
        public bool AutoInvertUseFastPathDelta = true;
        public bool AutoInvertUseFastPathCoverage = true;
        public double AutoInvertFastPathDeltaThreshold = 0.25;
        public double AutoInvertFastPathCoverageThreshold = 0.8;
        public bool AutoInvertUseDualEma = true;
        public double AutoInvertFastEmaAlpha = 0.6;
        public double AutoInvertSlowEmaAlpha = 0.15;
        public double AutoInvertEmaDiffThreshold = 0.2;
        public bool AutoInvertUseBurstSampling = true;
        public int AutoInvertBurstSampleMs = 200;
        public int AutoInvertBurstDurationMs = 2000;
        public bool AutoInvertUseConsecutiveTrigger = true;
        public bool AutoInvertUseCoverageGate = true;
        public bool AutoInvertUseDirectionalDebounce = true;
        public bool AutoInvertUseTargetResponse = false;
        public int AutoInvertTargetResponseMs = 1200;
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
            bool hasAutoInvertStrategySettings = false;
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
                        hasAutoInvertStrategySettings = xml.IndexOf("<AutoInvertUseFastPathDelta>", StringComparison.OrdinalIgnoreCase) >= 0;
                    }
                    catch
                    {
                        hasCursorSetting = false;
                        hasSoftwareCursorSetting = false;
                        hasNormalizeCursorSchemeSetting = false;
                        hasAutoInvertSetting = false;
                        hasAutoInvertAdvancedSettings = false;
                        hasAutoInvertStrategySettings = false;
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
                cfg.AutoInvertSampleMs = 500;
                cfg.AutoInvertBrightThreshold = 0.65;
                cfg.AutoInvertDarkThreshold = 0.45;
                cfg.AutoInvertBrightDwellMs = 1500;
                cfg.AutoInvertDarkDwellMs = 6000;
                cfg.AutoInvertMinHoldMs = 2000;
                cfg.AutoInvertRequiredSamples = 2;
                cfg.AutoInvertSmoothingAlpha = 0.35;
                cfg.AutoInvertBrightPixelThreshold = 0.8;
                cfg.AutoInvertBrightCoverageThreshold = 0.35;
                cfg.AutoInvertDarkCoverageThreshold = 0.15;
                cfg.AutoInvertUseFastPathDelta = true;
                cfg.AutoInvertUseFastPathCoverage = true;
                cfg.AutoInvertFastPathDeltaThreshold = 0.25;
                cfg.AutoInvertFastPathCoverageThreshold = 0.8;
                cfg.AutoInvertUseDualEma = true;
                cfg.AutoInvertFastEmaAlpha = 0.6;
                cfg.AutoInvertSlowEmaAlpha = 0.15;
                cfg.AutoInvertEmaDiffThreshold = 0.2;
                cfg.AutoInvertUseBurstSampling = true;
                cfg.AutoInvertBurstSampleMs = 200;
                cfg.AutoInvertBurstDurationMs = 2000;
                cfg.AutoInvertUseConsecutiveTrigger = true;
                cfg.AutoInvertUseCoverageGate = true;
                cfg.AutoInvertUseDirectionalDebounce = true;
                cfg.AutoInvertUseTargetResponse = false;
                cfg.AutoInvertTargetResponseMs = 1200;
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
                cfg.AutoInvertSampleMs = 500;
                cfg.AutoInvertBrightThreshold = 0.65;
                cfg.AutoInvertDarkThreshold = 0.45;
                cfg.AutoInvertBrightDwellMs = 1500;
                cfg.AutoInvertDarkDwellMs = 6000;
                cfg.AutoInvertMinHoldMs = 2000;
                cfg.AutoInvertRequiredSamples = 2;
                cfg.AutoInvertSmoothingAlpha = 0.35;
                cfg.AutoInvertBrightPixelThreshold = 0.8;
                cfg.AutoInvertBrightCoverageThreshold = 0.35;
                cfg.AutoInvertDarkCoverageThreshold = 0.15;
                cfg.AutoInvertHideOverlays = false;
            }
            else if (!hasAutoInvertAdvancedSettings)
            {
                cfg.AutoInvertBrightDwellMs = 1500;
                cfg.AutoInvertDarkDwellMs = 6000;
                cfg.AutoInvertMinHoldMs = 2000;
                cfg.AutoInvertRequiredSamples = 2;
                cfg.AutoInvertSmoothingAlpha = 0.35;
                cfg.AutoInvertBrightPixelThreshold = 0.8;
                cfg.AutoInvertBrightCoverageThreshold = 0.35;
                cfg.AutoInvertDarkCoverageThreshold = 0.15;
            }
            if (!hasAutoInvertStrategySettings)
            {
                cfg.AutoInvertUseFastPathDelta = true;
                cfg.AutoInvertUseFastPathCoverage = true;
                cfg.AutoInvertFastPathDeltaThreshold = 0.25;
                cfg.AutoInvertFastPathCoverageThreshold = 0.8;
                cfg.AutoInvertUseDualEma = true;
                cfg.AutoInvertFastEmaAlpha = 0.6;
                cfg.AutoInvertSlowEmaAlpha = 0.15;
                cfg.AutoInvertEmaDiffThreshold = 0.2;
                cfg.AutoInvertUseBurstSampling = true;
                cfg.AutoInvertBurstSampleMs = 200;
                cfg.AutoInvertBurstDurationMs = 2000;
                cfg.AutoInvertUseConsecutiveTrigger = true;
                cfg.AutoInvertUseCoverageGate = true;
                cfg.AutoInvertUseDirectionalDebounce = true;
                cfg.AutoInvertUseTargetResponse = false;
                cfg.AutoInvertTargetResponseMs = 1200;
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
