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
            if (File.Exists(ConfigPath))
            {
                try
                {
                    try
                    {
                        string xml = File.ReadAllText(ConfigPath);
                        hasCursorSetting = xml.IndexOf("<UseMagnifiedCursor>", StringComparison.OrdinalIgnoreCase) >= 0;
                    }
                    catch
                    {
                        hasCursorSetting = false;
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
            }
            else if (!hasCursorSetting)
            {
                // Default to magnified cursor for older config files.
                cfg.UseMagnifiedCursor = true;
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
