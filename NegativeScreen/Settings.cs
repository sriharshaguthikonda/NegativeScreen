using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;

namespace NegativeScreen
{
    static class Settings
    {
        private static readonly string ConfigFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.txt");

        public static List<string> LoadSelectedMonitors()
        {
            if (File.Exists(ConfigFilePath))
            {
                return new List<string>(File.ReadAllLines(ConfigFilePath));
            }
            List<string> all = new List<string>();
            foreach (var screen in Screen.AllScreens)
            {
                all.Add(screen.DeviceName);
            }
            return all;
        }

        public static void SaveSelectedMonitors(IEnumerable<string> monitors)
        {
            File.WriteAllLines(ConfigFilePath, new List<string>(monitors).ToArray());
        }
    }
}
