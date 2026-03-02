using System;
using System.IO;

namespace NegativeScreen
{
    internal sealed class AutoInvertConsoleLog : IDisposable
    {
        private readonly object sync = new object();
        private readonly string path;
        private bool headerWritten;

        public AutoInvertConsoleLog(string baseDirectory)
        {
            path = Path.Combine(baseDirectory, "AutoInvertConsole.log");
        }

        public void Log(string message)
        {
            try
            {
                lock (sync)
                {
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

        public void Dispose()
        {
        }
    }
}
