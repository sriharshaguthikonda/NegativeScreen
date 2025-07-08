using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;
using System.Windows.Forms;

namespace NegativeScreen
{
    [Serializable]
    public class Config
    {
        public List<string> Monitors = new List<string>();
        public List<string> Windows = new List<string>();
        public bool StartMinimized = false;
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
                cfg.Monitors.Add(screen.DeviceName);
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
    }
}
