using System;
using System.IO;

namespace NegativeScreen
{
    internal sealed class AutoInvertConsoleLog : IDisposable
    {
        private const long MaxBytes = 2 * 1024 * 1024;
        private readonly object sync = new object();
        private readonly string path;
        private readonly string backupPath;
        private bool headerWritten;

        public AutoInvertConsoleLog(string baseDirectory)
        {
            path = Path.Combine(baseDirectory, "AutoInvertConsole.log");
            backupPath = Path.Combine(baseDirectory, "AutoInvertConsole.previous.log");
        }

        public void Log(string message)
        {
            try
            {
                lock (sync)
                {
                    RotateIfNeeded();
                    if (!headerWritten)
                    {
                        File.AppendAllText(path, string.Format("{0} Branch={1}\r\n", DateTime.Now.ToString("O"), BuildInfo.BranchName));
                        headerWritten = true;
                    }
                    File.AppendAllText(path, string.Format("{0} {1}\r\n", DateTime.Now.ToString("O"), message));
                }
            }
            catch
            {
            }
        }

        private void RotateIfNeeded()
        {
            try
            {
                var info = new FileInfo(path);
                if (!info.Exists || info.Length < MaxBytes)
                    return;
                if (File.Exists(backupPath))
                    File.Delete(backupPath);
                File.Move(path, backupPath);
                headerWritten = false;
            }
            catch
            {
            }
        }

        public void Dispose()
        {
        }
    }
}
