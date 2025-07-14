using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
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
        public List<MonitorLabel> MonitorLabels = new List<MonitorLabel>();
    }

    [Serializable]
    public class MonitorLabel
    {
        public string Device { get; set; }
        public string Id { get; set; }
        public string Label { get; set; }
        public bool? DarkMode { get; set; }
    }

    static class Settings
    {
        private static readonly string ConfigPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "settings.xml");

        public static Config Load()
        {
            Config cfg = null;
            if (File.Exists(ConfigPath))
            {
                try
                {
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

        /// <summary>
        /// Saves the configuration to the settings file.
        /// IMPORTANT: This should only be called when the user explicitly requests to save settings.
        /// </summary>
        /// <param name="config">The configuration to save</param>
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
            catch 
            { 
                // Log error if needed
            }
        }

        public static string GetMonitorFriendlyName(Screen screen, bool useCached = true)
        {
            try
            {
                // First check if we have a stored label for this monitor
                string monitorId = GetMonitorId(screen);
                var config = Load();
                var existingLabel = config.MonitorLabels.Find(m => (m.Id == monitorId) || (m.Device == screen.DeviceName));
                if (existingLabel != null && !string.IsNullOrEmpty(existingLabel.Label))
                {
                    return existingLabel.Label.Trim();
                }

                // Then try to get the friendly name from the monitor's EDID data
                string monitorName = GetMonitorNameFromEdid(screen);
                if (!string.IsNullOrEmpty(monitorName) && !monitorName.Contains("PnP") && !monitorName.Contains("Default"))
                    return monitorName.Trim();

                // If that fails, try to get the device string
                NativeMethods.DISPLAY_DEVICE device = new NativeMethods.DISPLAY_DEVICE();
                device.cb = Marshal.SizeOf(typeof(NativeMethods.DISPLAY_DEVICE));
                if (NativeMethods.EnumDisplayDevices(screen.DeviceName, 0, ref device, 0))
                {
                    if (!string.IsNullOrEmpty(device.DeviceString) && 
                        !device.DeviceString.Contains("PnP") && 
                        !device.DeviceString.Contains("Default") &&
                        device.DeviceString.Trim() != "\\")
                    {
                        return device.DeviceString.Trim();
                    }
                }

                // As a last resort, try to get the monitor name from the registry
                string regName = GetMonitorNameFromRegistry(screen);
                if (!string.IsNullOrEmpty(regName) && 
                    !regName.Contains("PnP") && 
                    !regName.Contains("Default"))
                {
                    return regName.Trim();
                }
            }
            catch (Exception ex)
            {
                // Log error if needed
                Debug.WriteLine($"Error getting monitor name: {ex.Message}");
            }
            
            return screen.DeviceName;
        }

        private static string GetMonitorNameFromEdid(Screen screen)
        {
            try
            {
                // Get the monitor's device path
                NativeMethods.DISPLAY_DEVICE displayDevice = new NativeMethods.DISPLAY_DEVICE();
                displayDevice.cb = Marshal.SizeOf(displayDevice);
                
                for (uint deviceIndex = 0; NativeMethods.EnumDisplayDevices(screen.DeviceName, deviceIndex, ref displayDevice, 0); deviceIndex++)
                {
                    if ((displayDevice.StateFlags & NativeMethods.DisplayDeviceStateFlags.AttachedToDesktop) == NativeMethods.DisplayDeviceStateFlags.AttachedToDesktop)
                    {
                        // Open the monitor's device key
                        string devicePath = $"SYSTEM\\CurrentControlSet\\Enum\\{displayDevice.DeviceID}\\Device Parameters";
                        using (Microsoft.Win32.RegistryKey key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(devicePath))
                        {
                            if (key != null)
                            {
                                byte[] edid = key.GetValue("EDID") as byte[];
                                if (edid != null && edid.Length >= 150)
                                {
                                    // Extract monitor name from EDID bytes (bytes 54-71 contain the monitor name)
                                    string name = "";
                                    for (int i = 54; i < 126; i += 2)
                                    {
                                        if (edid[i] == 0x0A && edid[i + 1] == 0x00)
                                            break;
                                        name += (char)edid[i];
                                    }
                                    if (!string.IsNullOrWhiteSpace(name))
                                        return name.Trim();
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error getting EDID data: {ex.Message}");
            }
            return null;
        }

        private static string GetMonitorNameFromRegistry(Screen screen)
        {
            try
            {
                // Try to get the monitor name from the Windows registry
                using (Microsoft.Win32.RegistryKey key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Enum\DISPLAY"))
                {
                    if (key != null)
                    {
                        foreach (string monitorKey in key.GetSubKeyNames())
                        {
                            using (Microsoft.Win32.RegistryKey subKey = key.OpenSubKey(monitorKey + "\\Device Parameters"))
                            {
                                if (subKey != null)
                                {
                                    byte[] edid = subKey.GetValue("EDID") as byte[];
                                    if (edid != null && edid.Length >= 150)
                                    {
                                        // Extract monitor name from EDID bytes
                                        string name = "";
                                        for (int i = 54; i < 126; i += 2)
                                        {
                                            if (edid[i] == 0x0A && edid[i + 1] == 0x00)
                                                break;
                                            name += (char)edid[i];
                                        }
                                        if (!string.IsNullOrWhiteSpace(name))
                                            return name.Trim();
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error reading monitor name from registry: {ex.Message}");
            }
            return null;
        }

        public static string GetMonitorId(Screen screen)
        {
            try
            {
                NativeMethods.DISPLAY_DEVICE device = new NativeMethods.DISPLAY_DEVICE();
                device.cb = Marshal.SizeOf(typeof(NativeMethods.DISPLAY_DEVICE));
                if (NativeMethods.EnumDisplayDevices(screen.DeviceName, 0, ref device, 0))
                {
                    if (!string.IsNullOrEmpty(device.DeviceID))
                    {
                        // Use a consistent format for the ID
                        string id = device.DeviceID.Trim();
                        // Remove any trailing backslashes
                        while (id.EndsWith("\\") && id.Length > 0)
                            id = id.Substring(0, id.Length - 1);
                        return id;
                    }
                }
                // Fallback to device name if we can't get the ID
                return screen.DeviceName;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error getting monitor ID: {ex.Message}");
                return screen.DeviceName;
            }
        }

        public static bool? GetMonitorDarkMode(Screen screen)
        {
            try
            {
                string monitorId = GetMonitorId(screen);
                var config = Load();
                var monitorLabel = config.MonitorLabels.Find(m => (m.Id == monitorId) || (m.Device == screen.DeviceName));
                return monitorLabel?.DarkMode;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error getting monitor dark mode: {ex.Message}");
                return null;
            }
        }

        public static void SetMonitorDarkMode(Screen screen, bool darkMode)
        {
            try
            {
                string monitorId = GetMonitorId(screen);
                var config = Load();
                var monitorLabel = config.MonitorLabels.Find(m => (m.Id == monitorId) || (m.Device == screen.DeviceName));
                
                if (monitorLabel == null)
                {
                    monitorLabel = new MonitorLabel 
                    { 
                        Device = screen.DeviceName, 
                        Id = monitorId,
                        Label = GetMonitorFriendlyName(screen, false),
                        DarkMode = darkMode 
                    };
                    config.MonitorLabels.Add(monitorLabel);
                }
                else
                {
                    monitorLabel.DarkMode = darkMode;
                    monitorLabel.Device = screen.DeviceName; // Update device name in case it changed
                    if (string.IsNullOrEmpty(monitorLabel.Id))
                        monitorLabel.Id = monitorId;
                }
                
                Save(config);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error setting monitor dark mode: {ex.Message}");
            }
        }
    }
}
