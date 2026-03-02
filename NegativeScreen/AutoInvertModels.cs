using System;

namespace NegativeScreen
{
    internal sealed class AutoInvertSettings
    {
        public bool Enabled;
        public int SampleMs;
        public double BrightThreshold;
        public double DarkThreshold;
        public int BrightDwellMs;
        public int DarkDwellMs;
        public int MinHoldMs;
        public int RequiredSamples;
        public double SmoothingAlpha;
        public double BrightPixelThreshold;
        public double BrightCoverageThreshold;
        public double DarkCoverageThreshold;
        public bool HideOverlays;

        public static AutoInvertSettings FromConfig(Config cfg)
        {
            var settings = new AutoInvertSettings
            {
                Enabled = cfg.AutoInvertByBrightness,
                SampleMs = Math.Max(250, cfg.AutoInvertSampleMs),
                BrightThreshold = Clamp(cfg.AutoInvertBrightThreshold, 0.0, 1.0),
                DarkThreshold = Clamp(cfg.AutoInvertDarkThreshold, 0.0, 1.0),
                BrightDwellMs = Math.Max(0, cfg.AutoInvertBrightDwellMs),
                DarkDwellMs = Math.Max(0, cfg.AutoInvertDarkDwellMs),
                MinHoldMs = Math.Max(0, cfg.AutoInvertMinHoldMs),
                RequiredSamples = Math.Max(1, cfg.AutoInvertRequiredSamples),
                SmoothingAlpha = Clamp(cfg.AutoInvertSmoothingAlpha, 0.05, 1.0),
                BrightPixelThreshold = Clamp(cfg.AutoInvertBrightPixelThreshold, 0.0, 1.0),
                BrightCoverageThreshold = Clamp(cfg.AutoInvertBrightCoverageThreshold, 0.0, 1.0),
                DarkCoverageThreshold = Clamp(cfg.AutoInvertDarkCoverageThreshold, 0.0, 1.0),
                HideOverlays = cfg.AutoInvertHideOverlays
            };
            if (settings.BrightThreshold <= settings.DarkThreshold)
            {
                double mid = (settings.BrightThreshold + settings.DarkThreshold) / 2.0;
                settings.BrightThreshold = Clamp(mid + 0.1, 0.0, 1.0);
                settings.DarkThreshold = Clamp(mid - 0.1, 0.0, 1.0);
            }
            if (settings.DarkCoverageThreshold > settings.BrightCoverageThreshold)
            {
                double mid = (settings.DarkCoverageThreshold + settings.BrightCoverageThreshold) / 2.0;
                settings.BrightCoverageThreshold = Clamp(mid + 0.1, 0.0, 1.0);
                settings.DarkCoverageThreshold = Clamp(mid - 0.1, 0.0, 1.0);
            }
            return settings;
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
        public double SmoothedLuminance;
        public double LastBrightRatio;
        public bool HasSmoothed;
        public bool? LastDesired;
        public int DesiredStreak;
        public long LastChangeTick;
    }

    internal struct BrightnessSample
    {
        public string DeviceName;
        public double Luminance;
        public double BrightRatio;
        public int Width;
        public int Height;
        public int SampleCount;
        public long CaptureMs;
    }
}
