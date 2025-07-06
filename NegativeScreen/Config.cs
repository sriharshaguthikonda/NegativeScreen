using System;
using System.IO;

namespace NegativeScreen
{
    public class Config
    {
        public int RefreshInterval = 10;
        public static string ConfigFilePath = "config.ini";

        public static Config Load()
        {
            Config cfg = new Config();
            try
            {
                if (File.Exists(ConfigFilePath))
                {
                    string[] lines = File.ReadAllLines(ConfigFilePath);
                    foreach (string line in lines)
                    {
                        string[] parts = line.Split('=');
                        if (parts.Length == 2)
                        {
                            if (parts[0].Trim() == "RefreshInterval")
                            {
                                int val;
                                if (int.TryParse(parts[1], out val))
                                {
                                    cfg.RefreshInterval = val;
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception) { }
            return cfg;
        }

        public void Save()
        {
            try
            {
                File.WriteAllLines(ConfigFilePath, new string[] {
                    "RefreshInterval=" + this.RefreshInterval
                });
            }
            catch (Exception) { }
        }
    }
}
