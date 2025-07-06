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
        public List<string> EnabledDisplays { get; set; }
        public int RefreshInterval { get; set; }

        private static string ConfigPath
        {
            get { return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.xml"); }
        }

        public static Config Load()
        {
            try
            {
                if (File.Exists(ConfigPath))
                {
                    XmlSerializer ser = new XmlSerializer(typeof(Config));
                    using (FileStream fs = new FileStream(ConfigPath, FileMode.Open))
                    {
                        return (Config)ser.Deserialize(fs);
                    }
                }
            }
            catch { }
            return null;
        }

        public void Save()
        {
            try
            {
                XmlSerializer ser = new XmlSerializer(typeof(Config));
                using (FileStream fs = new FileStream(ConfigPath, FileMode.Create))
                {
                    ser.Serialize(fs, this);
                }
            }
            catch { }
        }

        public static Config CreateDefault()
        {
            Config cfg = new Config();
            cfg.EnabledDisplays = new List<string>();
            foreach (var screen in Screen.AllScreens)
            {
                cfg.EnabledDisplays.Add(screen.DeviceName);
            }
            cfg.RefreshInterval = 10;
            return cfg;
        }
    }
}
