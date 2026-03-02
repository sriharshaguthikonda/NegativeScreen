using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;

namespace NegativeScreen
{
    internal sealed class AutoInvertLogger : IDisposable
    {
        private readonly string path;
        private readonly object sync = new object();
        private bool headerWritten;

        public AutoInvertLogger(string baseDirectory)
        {
            path = Path.Combine(baseDirectory, "AutoInvertMetrics.csv");
        }

        public void LogSample(string deviceName, BrightnessSample sample, bool isInverted)
        {
            try
            {
                lock (sync)
                {
                    if (!headerWritten)
                    {
                        if (!File.Exists(path))
                        {
                            File.AppendAllText(path, "Timestamp,Device,Luminance,Inverted,Width,Height,Samples,CaptureMs,WorkingSetMB,PrivateMB,TotalCpuMs\r\n");
                        }
                        headerWritten = true;
                    }
                    Process proc = Process.GetCurrentProcess();
                    double workingMb = proc.WorkingSet64 / (1024.0 * 1024.0);
                    double privateMb = proc.PrivateMemorySize64 / (1024.0 * 1024.0);
                    double cpuMs = proc.TotalProcessorTime.TotalMilliseconds;
                    string line = string.Format(CultureInfo.InvariantCulture,
                        "{0},{1},{2:F4},{3},{4},{5},{6},{7},{8:F2},{9:F2},{10:F0}\r\n",
                        DateTime.Now.ToString("O"),
                        deviceName ?? "",
                        sample.Luminance,
                        isInverted ? "1" : "0",
                        sample.Width,
                        sample.Height,
                        sample.SampleCount,
                        sample.CaptureMs,
                        workingMb,
                        privateMb,
                        cpuMs);
                    File.AppendAllText(path, line);
                }
            }
            catch
            {
                // best-effort logging
            }
        }

        public void Dispose()
        {
        }
    }
}
