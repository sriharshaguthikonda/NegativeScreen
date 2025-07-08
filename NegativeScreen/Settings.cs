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
        public bool DarkMode = false;
        public List<MonitorLabel> MonitorLabels = new List<MonitorLabel>();
    }

    [Serializable]
    public class MonitorLabel
    {
        public string Device;
        public string Label;
    }

    static class Settings
    {
        private static readonly string ConfigPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "settings.xml");

        public static Config Load()
        {
            if (File.Exists(ConfigPath))
            {
                try
                {
                    XmlSerializer xs = new XmlSerializer(typeof(Config));
                    using (FileStream fs = new FileStream(ConfigPath, FileMode.Open))
                    {
                        return (Config)xs.Deserialize(fs);
                    }
                }
                catch { }
            }
            Config cfg = new Config();
            foreach (var screen in Screen.AllScreens)
            {
                cfg.Monitors.Add(screen.DeviceName);
                cfg.MonitorLabels.Add(new MonitorLabel { Device = screen.DeviceName, Label = GetMonitorFriendlyName(screen) });
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

        private static string GetMonitorFriendlyName(Screen screen)
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
    }
}
