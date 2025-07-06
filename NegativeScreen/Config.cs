using System;
using System.IO;
using System.Linq;

namespace NegativeScreen
{
    public static class Config
    {
        private static readonly string ConfigDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "NegativeScreen");
        private static readonly string ConfigFile = Path.Combine(ConfigDir, "config.txt");

        public static string[] LoadSelectedDisplays()
        {
            try
            {
                if (File.Exists(ConfigFile))
                {
                    return File.ReadAllLines(ConfigFile);
                }
            }
            catch { }
            return System.Windows.Forms.Screen.AllScreens.Select(s => s.DeviceName).ToArray();
        }

        public static void SaveSelectedDisplays(string[] displays)
        {
            try
            {
                Directory.CreateDirectory(ConfigDir);
                File.WriteAllLines(ConfigFile, displays);
            }
            catch { }
        }
    }
}
