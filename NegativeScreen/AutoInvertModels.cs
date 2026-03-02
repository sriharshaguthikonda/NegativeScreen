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
        public bool UseFastPathDelta;
        public bool UseFastPathCoverage;
        public double FastPathDeltaThreshold;
        public double FastPathCoverageThreshold;
        public bool UseDualEma;
        public double FastEmaAlpha;
        public double SlowEmaAlpha;
        public double EmaDiffThreshold;
        public bool UseBurstSampling;
        public int BurstSampleMs;
        public int BurstDurationMs;
        public bool UseConsecutiveTrigger;
        public bool UseCoverageGate;
        public bool UseDirectionalDebounce;
        public bool UseTargetResponse;
        public int TargetResponseMs;
        public bool UseMediaPause;
        public double MediaDeltaThreshold;
        public double MediaScoreThreshold;
        public double MediaScoreAlpha;
        public int MediaHoldMs;
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
                UseFastPathDelta = cfg.AutoInvertUseFastPathDelta,
                UseFastPathCoverage = cfg.AutoInvertUseFastPathCoverage,
                FastPathDeltaThreshold = Clamp(cfg.AutoInvertFastPathDeltaThreshold, 0.0, 1.0),
                FastPathCoverageThreshold = Clamp(cfg.AutoInvertFastPathCoverageThreshold, 0.0, 1.0),
                UseDualEma = cfg.AutoInvertUseDualEma,
                FastEmaAlpha = Clamp(cfg.AutoInvertFastEmaAlpha, 0.05, 1.0),
                SlowEmaAlpha = Clamp(cfg.AutoInvertSlowEmaAlpha, 0.05, 1.0),
                EmaDiffThreshold = Clamp(cfg.AutoInvertEmaDiffThreshold, 0.0, 1.0),
                UseBurstSampling = cfg.AutoInvertUseBurstSampling,
                BurstSampleMs = Math.Max(50, cfg.AutoInvertBurstSampleMs),
                BurstDurationMs = Math.Max(0, cfg.AutoInvertBurstDurationMs),
                UseConsecutiveTrigger = cfg.AutoInvertUseConsecutiveTrigger,
                UseCoverageGate = cfg.AutoInvertUseCoverageGate,
                UseDirectionalDebounce = cfg.AutoInvertUseDirectionalDebounce,
                UseTargetResponse = cfg.AutoInvertUseTargetResponse,
                TargetResponseMs = Math.Max(100, cfg.AutoInvertTargetResponseMs),
                UseMediaPause = cfg.AutoInvertUseMediaPause,
                MediaDeltaThreshold = Clamp(cfg.AutoInvertMediaDeltaThreshold, 0.0, 1.0),
                MediaScoreThreshold = Clamp(cfg.AutoInvertMediaScoreThreshold, 0.0, 1.0),
                MediaScoreAlpha = Clamp(cfg.AutoInvertMediaScoreAlpha, 0.05, 1.0),
                MediaHoldMs = Math.Max(0, cfg.AutoInvertMediaHoldMs),
                HideOverlays = cfg.AutoInvertHideOverlays
            };
            if (settings.UseTargetResponse)
            {
                settings.SmoothingAlpha = ComputeAlpha(settings.SampleMs, settings.TargetResponseMs);
            }
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

        private static double ComputeAlpha(int sampleMs, int targetMs)
        {
            if (targetMs <= 0)
                return 1.0;
            double ratio = Math.Max(1.0, sampleMs) / targetMs;
            double alpha = 1.0 - Math.Exp(-ratio);
            return Clamp(alpha, 0.05, 1.0);
        }
    }

    internal sealed class AutoInvertState
    {
        public bool IsInverted;
        public bool? PendingInvert;
        public long PendingSinceTick;
        public double LastLuminance;
        public double SmoothedLuminance;
        public double FastEma;
        public double SlowEma;
        public bool HasFastEma;
        public bool HasSlowEma;
        public double LastBrightRatio;
        public bool HasSmoothed;
        public bool? LastDesired;
        public int DesiredStreak;
        public long LastChangeTick;
        public double MediaScore;
        public long MediaActiveUntilTick;
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
