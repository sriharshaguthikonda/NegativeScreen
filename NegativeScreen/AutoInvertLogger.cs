using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;

namespace NegativeScreen
{
    internal sealed class AutoInvertLogger : IDisposable
    {
        private const long MaxBytes = 5 * 1024 * 1024;
        private readonly string path;
        private readonly string backupPath;
        private readonly object sync = new object();
        private bool headerWritten;

        public AutoInvertLogger(string baseDirectory)
        {
            path = Path.Combine(baseDirectory, "AutoInvertMetrics.csv");
            backupPath = Path.Combine(baseDirectory, "AutoInvertMetrics.previous.csv");
        }

        public void LogSample(string deviceName, BrightnessSample sample, bool isInverted, double smoothedLuminance)
        {
            try
            {
                lock (sync)
                {
                    RotateIfNeeded();
                    if (!headerWritten)
                    {
                        if (!File.Exists(path))
                        {
                            File.AppendAllText(path, "Timestamp,Device,Luminance,SmoothedLuminance,BrightRatio,Inverted,Width,Height,Samples,CaptureMs,WorkingSetMB,PrivateMB,TotalCpuMs\r\n");
                        }
                        headerWritten = true;
                    }
                    Process proc = Process.GetCurrentProcess();
                    double workingMb = proc.WorkingSet64 / (1024.0 * 1024.0);
                    double privateMb = proc.PrivateMemorySize64 / (1024.0 * 1024.0);
                    double cpuMs = proc.TotalProcessorTime.TotalMilliseconds;
                    string line = string.Format(CultureInfo.InvariantCulture,
                        "{0},{1},{2:F4},{3:F4},{4:F4},{5},{6},{7},{8},{9},{10:F2},{11:F2},{12:F0}\r\n",
                        DateTime.Now.ToString("O"),
                        deviceName ?? "",
                        sample.Luminance,
                        smoothedLuminance,
                        sample.BrightRatio,
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
