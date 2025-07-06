using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;
using System.Windows.Forms;

namespace NegativeScreen
{
    [Serializable]
    public class AppConfig
    {
        public int RefreshInterval = 10;
        public List<string> EnabledDisplays = new List<string>();

        public static string ConfigFilePath
        {
            get
            {
                return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.xml");
            }
        }

        public static AppConfig Load()
        {
            if (File.Exists(ConfigFilePath))
            {
                try
                {
                    XmlSerializer serializer = new XmlSerializer(typeof(AppConfig));
                    using (var fs = new FileStream(ConfigFilePath, FileMode.Open))
                    {
                        return (AppConfig)serializer.Deserialize(fs);
                    }
                }
                catch { }
            }
            // create default config
            AppConfig cfg = new AppConfig();
            foreach (var screen in Screen.AllScreens)
            {
                cfg.EnabledDisplays.Add(screen.DeviceName);
            }
            return cfg;
        }

        public void Save()
        {
            XmlSerializer serializer = new XmlSerializer(typeof(AppConfig));
            using (var fs = new FileStream(ConfigFilePath, FileMode.Create))
            {
                serializer.Serialize(fs, this);
            }
        }
    }
}
