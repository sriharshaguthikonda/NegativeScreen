using System;
using System.Collections.Generic;
using System.Threading;

namespace NegativeScreen
{
    internal sealed class AutoInvertManager : IDisposable
    {
        private readonly OverlayManager overlayManager;
        private readonly IBrightnessSampler sampler;
        private readonly AutoInvertLogger logger;
        private readonly AutoInvertConsoleLog consoleLog;
        private readonly Dictionary<string, AutoInvertState> states = new Dictionary<string, AutoInvertState>(StringComparer.OrdinalIgnoreCase);
        private AutoInvertSettings settings;
        private Timer timer;
        private int isRunning;
        private bool disposed;

        public AutoInvertManager(OverlayManager overlayManager, AutoInvertSettings settings, IBrightnessSampler sampler)
        {
            this.overlayManager = overlayManager;
            this.settings = settings;
            this.sampler = sampler;
            this.logger = new AutoInvertLogger(AppDomain.CurrentDomain.BaseDirectory);
            this.consoleLog = new AutoInvertConsoleLog(AppDomain.CurrentDomain.BaseDirectory);
        }

        public void Start()
        {
            if (!settings.Enabled)
                return;
            timer = new Timer(OnTimer, null, settings.SampleMs, settings.SampleMs);
        }

        public void Dispose()
        {
            disposed = true;
            if (timer != null)
            {
                timer.Dispose();
                timer = null;
            }
            sampler.Dispose();
            logger.Dispose();
            consoleLog.Dispose();
            states.Clear();
        }

        private void OnTimer(object state)
        {
            if (disposed)
                return;
            if (Interlocked.Exchange(ref isRunning, 1) == 1)
                return;
            List<Tuple<string, bool>> pendingChanges = null;
            try
            {
                string[] deviceNames = overlayManager.GetActiveMonitorDeviceNames();
                if (deviceNames.Length == 0)
                    return;
                long now = Environment.TickCount;
                pendingChanges = new List<Tuple<string, bool>>();
                for (int i = 0; i < deviceNames.Length; i++)
                {
                    string deviceName = deviceNames[i];
                    if (!sampler.TrySample(deviceName, settings.BrightPixelThreshold, out BrightnessSample sample))
                        continue;
                    AutoInvertState stateEntry = GetState(deviceName);
                    double effectiveLum = sample.Luminance;
                    double effectiveBrightRatio = sample.BrightRatio;
                    if (stateEntry.IsInverted)
                    {
                        effectiveLum = 1.0 - effectiveLum;
                        effectiveBrightRatio = 1.0 - effectiveBrightRatio;
                    }
                    effectiveLum = Clamp01(effectiveLum);
                    effectiveBrightRatio = Clamp01(effectiveBrightRatio);
                    stateEntry.LastLuminance = effectiveLum;
                    stateEntry.LastBrightRatio = effectiveBrightRatio;
                    UpdateSmoothed(stateEntry, effectiveLum);
                    if (IsHoldActive(stateEntry, now))
                    {
                        ResetPending(stateEntry);
                        logger.LogSample(deviceName, sample, stateEntry.IsInverted, stateEntry.SmoothedLuminance);
                        consoleLog.Log(BuildSampleLog(deviceName, sample, stateEntry, null, true));
                        continue;
                    }
                    bool brightCoverage = effectiveBrightRatio >= settings.BrightCoverageThreshold;
                    bool darkCoverage = effectiveBrightRatio <= settings.DarkCoverageThreshold;
                    bool? desired = null;
                    if (!stateEntry.IsInverted && stateEntry.SmoothedLuminance >= settings.BrightThreshold && brightCoverage)
                        desired = true;
                    else if (stateEntry.IsInverted && stateEntry.SmoothedLuminance <= settings.DarkThreshold && darkCoverage)
                        desired = false;
                    if (desired.HasValue)
                    {
                        if (stateEntry.LastDesired.HasValue && stateEntry.LastDesired.Value == desired.Value)
                        {
                            stateEntry.DesiredStreak++;
                        }
                        else
                        {
                            stateEntry.LastDesired = desired.Value;
                            stateEntry.DesiredStreak = 1;
                        }
                        if (stateEntry.DesiredStreak >= settings.RequiredSamples)
                        {
                            if (stateEntry.PendingInvert.HasValue && stateEntry.PendingInvert.Value == desired.Value)
                            {
                                int dwellMs = desired.Value ? settings.BrightDwellMs : settings.DarkDwellMs;
                                if (dwellMs <= 0 || unchecked(now - stateEntry.PendingSinceTick) >= dwellMs)
                                {
                                    QueueChange(pendingChanges, deviceName, desired.Value);
                                    stateEntry.PendingInvert = null;
                                }
                            }
                            else
                            {
                                stateEntry.PendingInvert = desired.Value;
                                stateEntry.PendingSinceTick = now;
                            }
                        }
                        else
                        {
                            stateEntry.PendingInvert = null;
                        }
                    }
                    else
                    {
                        ResetPending(stateEntry);
                    }
                    logger.LogSample(deviceName, sample, stateEntry.IsInverted, stateEntry.SmoothedLuminance);
                    consoleLog.Log(BuildSampleLog(deviceName, sample, stateEntry, desired, false));
                }
            }
            finally
            {
                if (pendingChanges != null && pendingChanges.Count > 0)
                {
                    ApplyQueuedChanges(pendingChanges);
                }
                Interlocked.Exchange(ref isRunning, 0);
            }
        }

        private AutoInvertState GetState(string deviceName)
        {
            if (!states.TryGetValue(deviceName, out AutoInvertState state))
            {
                state = new AutoInvertState
                {
                    IsInverted = overlayManager.GetMonitorOverlayVisible(deviceName)
                };
                states[deviceName] = state;
            }
            return state;
        }

        private void UpdateSmoothed(AutoInvertState state, double current)
        {
            if (!state.HasSmoothed)
            {
                state.SmoothedLuminance = current;
                state.HasSmoothed = true;
                return;
            }
            double alpha = settings.SmoothingAlpha;
            state.SmoothedLuminance = alpha * current + (1.0 - alpha) * state.SmoothedLuminance;
        }

        private bool IsHoldActive(AutoInvertState state, long now)
        {
            if (settings.MinHoldMs <= 0 || state.LastChangeTick == 0)
                return false;
            return unchecked(now - state.LastChangeTick) < settings.MinHoldMs;
        }

        private void ResetPending(AutoInvertState state)
        {
            state.PendingInvert = null;
            state.LastDesired = null;
            state.DesiredStreak = 0;
            state.PendingSinceTick = 0;
        }

        private void QueueChange(List<Tuple<string, bool>> pendingChanges, string deviceName, bool invert)
        {
            for (int i = 0; i < pendingChanges.Count; i++)
            {
                if (string.Equals(pendingChanges[i].Item1, deviceName, StringComparison.OrdinalIgnoreCase))
                {
                    pendingChanges[i] = new Tuple<string, bool>(deviceName, invert);
                    return;
                }
            }
            pendingChanges.Add(new Tuple<string, bool>(deviceName, invert));
        }

        private void ApplyQueuedChanges(List<Tuple<string, bool>> pendingChanges)
        {
            for (int i = 0; i < pendingChanges.Count; i++)
            {
                var change = pendingChanges[i];
                overlayManager.SetMonitorOverlayVisible(change.Item1, change.Item2);
                AutoInvertState state = GetState(change.Item1);
                state.IsInverted = change.Item2;
                state.LastChangeTick = Environment.TickCount;
                ResetPending(state);
                consoleLog.Log(string.Format("APPLY device={0} invert={1}", change.Item1, change.Item2 ? "1" : "0"));
            }
        }

        private string BuildSampleLog(string deviceName, BrightnessSample sample, AutoInvertState state, bool? desired, bool hold)
        {
            return string.Format("SAMPLE device={0} rawLum={1:F4} rawBrightRatio={2:F4} effLum={3:F4} smoothed={4:F4} inverted={5} desired={6} streak={7} pending={8} hold={9}",
                deviceName,
                sample.Luminance,
                sample.BrightRatio,
                state.LastLuminance,
                state.SmoothedLuminance,
                state.IsInverted ? "1" : "0",
                desired.HasValue ? (desired.Value ? "1" : "0") : "-",
                state.DesiredStreak,
                state.PendingInvert.HasValue ? (state.PendingInvert.Value ? "1" : "0") : "-",
                hold ? "1" : "0");
        }

        private double Clamp01(double value)
        {
            if (value < 0.0) return 0.0;
            if (value > 1.0) return 1.0;
            return value;
        }
    }
}
