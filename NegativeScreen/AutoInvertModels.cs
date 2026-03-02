using System;

namespace NegativeScreen
{
    internal sealed class AutoInvertSettings
    {
        public bool Enabled;
        public int SampleMs;
        public double BrightThreshold;
        public double DarkThreshold;
        public int DwellMs;
        public bool HideOverlays;

        public static AutoInvertSettings FromConfig(Config cfg)
        {
            return new AutoInvertSettings
            {
                Enabled = cfg.AutoInvertByBrightness,
                SampleMs = Math.Max(250, cfg.AutoInvertSampleMs),
                BrightThreshold = Clamp(cfg.AutoInvertBrightThreshold, 0.0, 1.0),
                DarkThreshold = Clamp(cfg.AutoInvertDarkThreshold, 0.0, 1.0),
                DwellMs = Math.Max(0, cfg.AutoInvertDwellMs),
                HideOverlays = cfg.AutoInvertHideOverlays
            };
        }

        private static double Clamp(double value, double min, double max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }
    }

    internal sealed class AutoInvertState
    {
        public bool IsInverted;
        public bool? PendingInvert;
        public long PendingSinceTick;
        public double LastLuminance;
    }

    internal struct BrightnessSample
    {
        public string DeviceName;
        public double Luminance;
        public int Width;
        public int Height;
        public int SampleCount;
        public long CaptureMs;
    }
}
