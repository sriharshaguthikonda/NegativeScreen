using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;

namespace NegativeScreen
{
    static class Settings
    {
        private static readonly string MonitorConfigPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "monitors.txt");
        private static readonly string WindowConfigPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "windows.txt");

        public static List<string> LoadSelectedMonitors()
        {
            if (File.Exists(MonitorConfigPath))
            {
                return new List<string>(File.ReadAllLines(MonitorConfigPath));
            }
            List<string> all = new List<string>();
            foreach (var screen in Screen.AllScreens)
            {
                all.Add(screen.DeviceName);
            }
            return all;
        }

        public static List<string> LoadSelectedWindows()
        {
            if (File.Exists(WindowConfigPath))
            {
                return new List<string>(File.ReadAllLines(WindowConfigPath));
            }
            return new List<string>();
        }

        public static void SaveSelectedMonitors(IEnumerable<string> monitors)
        {
            File.WriteAllLines(MonitorConfigPath, new List<string>(monitors).ToArray());
        }

        public static void SaveSelectedWindows(IEnumerable<string> windows)
        {
            File.WriteAllLines(WindowConfigPath, new List<string>(windows).ToArray());
        }
    }
}
