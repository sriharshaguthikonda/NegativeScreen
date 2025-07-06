using System;
using System.Collections.Generic;
using System.IO;

namespace NegativeScreen
{
    static class ConfigManager
    {
        private static readonly string ConfigPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.txt");

        public static List<string> LoadEnabledDisplays()
        {
            try
            {
                if (File.Exists(ConfigPath))
                {
                    return new List<string>(File.ReadAllLines(ConfigPath));
                }
            }
            catch { }
            return new List<string>();
        }

        public static void SaveEnabledDisplays(IEnumerable<string> displays)
        {
            try
            {
                File.WriteAllLines(ConfigPath, displays);
            }
            catch { }
        }
    }
}
