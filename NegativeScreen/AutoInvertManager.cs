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
        private readonly object timerSync = new object();
        private int baseSampleMs;
        private int currentSampleMs;
        private long burstUntilTick;
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
            baseSampleMs = settings.SampleMs;
            currentSampleMs = baseSampleMs;
            consoleLog.Log(string.Format("SETTINGS sampleMs={0} bright={1:F2} dark={2:F2} brightDwell={3} darkDwell={4} minHold={5} reqSamples={6} alpha={7:F2} fastDelta={8} fastCov={9} dualEma={10} burst={11} coverageGate={12} dirDebounce={13} targetResp={14}",
                settings.SampleMs,
                settings.BrightThreshold,
                settings.DarkThreshold,
                settings.BrightDwellMs,
                settings.DarkDwellMs,
                settings.MinHoldMs,
                settings.RequiredSamples,
                settings.SmoothingAlpha,
                settings.UseFastPathDelta ? "1" : "0",
                settings.UseFastPathCoverage ? "1" : "0",
                settings.UseDualEma ? "1" : "0",
                settings.UseBurstSampling ? "1" : "0",
                settings.UseCoverageGate ? "1" : "0",
                settings.UseDirectionalDebounce ? "1" : "0",
                settings.UseTargetResponse ? "1" : "0"));
            InitializeNow();
            timer = new Timer(OnTimer, null, currentSampleMs, currentSampleMs);
        }

        public void Dispose()
        {
            disposed = true;
            if (timer != null)
            {
                timer.Dispose();
                timer = null;
            }
            burstUntilTick = 0;
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
                if (settings.UseBurstSampling && burstUntilTick > 0 && unchecked(now - burstUntilTick) >= 0)
                {
                    burstUntilTick = 0;
                    UpdateTimerInterval(baseSampleMs);
                    consoleLog.Log(string.Format("BURST_END interval={0}", baseSampleMs));
                }
                pendingChanges = new List<Tuple<string, bool>>();
                for (int i = 0; i < deviceNames.Length; i++)
                {
                    string deviceName = deviceNames[i];
                    if (!sampler.TrySample(deviceName, settings.BrightPixelThreshold, out BrightnessSample sample))
                        continue;
                    AutoInvertState stateEntry = GetState(deviceName);
                    double prevLum = stateEntry.LastLuminance;
                    bool hadPrev = stateEntry.HasSmoothed;
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
                    UpdateDualEma(stateEntry, effectiveLum);
                    double delta = hadPrev ? (effectiveLum - prevLum) : 0.0;
                    double emaDiff = stateEntry.FastEma - stateEntry.SlowEma;
                    if (IsHoldActive(stateEntry, now))
                    {
                        ResetPending(stateEntry);
                        logger.LogSample(deviceName, sample, stateEntry.IsInverted, stateEntry.SmoothedLuminance);
                        consoleLog.Log(BuildSampleLog(deviceName, sample, stateEntry, null, true, delta, emaDiff, false, false));
                        continue;
                    }
                    bool brightCoverage = !settings.UseCoverageGate || effectiveBrightRatio >= settings.BrightCoverageThreshold;
                    bool darkCoverage = !settings.UseCoverageGate || effectiveBrightRatio <= settings.DarkCoverageThreshold;
                    bool? desired = null;
                    bool fastPathTriggered = false;
                    bool dualEmaTriggered = false;
                    bool burstTriggered = false;
                    bool? fastDesired = null;
                    if (!stateEntry.IsInverted)
                    {
                        if (settings.UseFastPathDelta && stateEntry.HasSmoothed && delta >= settings.FastPathDeltaThreshold && brightCoverage && effectiveLum >= settings.BrightThreshold)
                        {
                            fastPathTriggered = true;
                            fastDesired = true;
                        }
                        if (settings.UseFastPathCoverage && effectiveBrightRatio >= settings.FastPathCoverageThreshold)
                        {
                            fastPathTriggered = true;
                            fastDesired = true;
                        }
                    }
                    if (settings.UseDualEma && Math.Abs(emaDiff) >= settings.EmaDiffThreshold)
                    {
                        if (!stateEntry.IsInverted && emaDiff >= settings.EmaDiffThreshold && brightCoverage)
                        {
                            dualEmaTriggered = true;
                            fastDesired = true;
                        }
                        else if (stateEntry.IsInverted && emaDiff <= -settings.EmaDiffThreshold && darkCoverage)
                        {
                            dualEmaTriggered = true;
                            fastDesired = false;
                        }
                    }
                    if (settings.UseBurstSampling)
                    {
                        if (fastPathTriggered || dualEmaTriggered)
                            burstTriggered = true;
                        else if (settings.UseFastPathDelta && stateEntry.HasSmoothed && Math.Abs(delta) >= settings.FastPathDeltaThreshold)
                            burstTriggered = true;
                    }
                    if (burstTriggered)
                    {
                        StartBurst(now);
                    }
                    if (fastDesired.HasValue)
                    {
                        QueueChange(pendingChanges, deviceName, fastDesired.Value);
                        stateEntry.PendingInvert = null;
                        ResetPending(stateEntry);
                        logger.LogSample(deviceName, sample, stateEntry.IsInverted, stateEntry.SmoothedLuminance);
                        consoleLog.Log(BuildSampleLog(deviceName, sample, stateEntry, fastDesired, false, delta, emaDiff, fastPathTriggered, dualEmaTriggered));
                        continue;
                    }
                    if (!stateEntry.IsInverted && stateEntry.SmoothedLuminance >= settings.BrightThreshold && brightCoverage)
                        desired = true;
                    else if (stateEntry.IsInverted && stateEntry.SmoothedLuminance <= settings.DarkThreshold && darkCoverage)
                        desired = false;
                    if (desired.HasValue)
                    {
                        int requiredSamples = settings.UseConsecutiveTrigger ? settings.RequiredSamples : 1;
                        if (stateEntry.LastDesired.HasValue && stateEntry.LastDesired.Value == desired.Value)
                        {
                            stateEntry.DesiredStreak++;
                        }
                        else
                        {
                            stateEntry.LastDesired = desired.Value;
                            stateEntry.DesiredStreak = 1;
                        }
                        if (stateEntry.DesiredStreak >= requiredSamples)
                        {
                            if (settings.UseConsecutiveTrigger && desired.Value && stateEntry.DesiredStreak >= requiredSamples)
                            {
                                QueueChange(pendingChanges, deviceName, true);
                                stateEntry.PendingInvert = null;
                            }
                            else if (stateEntry.PendingInvert.HasValue && stateEntry.PendingInvert.Value == desired.Value)
                            {
                                int dwellMs = settings.UseDirectionalDebounce ? (desired.Value ? settings.BrightDwellMs : settings.DarkDwellMs) : settings.BrightDwellMs;
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
                    consoleLog.Log(BuildSampleLog(deviceName, sample, stateEntry, desired, false, delta, emaDiff, fastPathTriggered, dualEmaTriggered));
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

        public void InitializeNow()
        {
            if (!settings.Enabled)
                return;
            string[] deviceNames = overlayManager.GetActiveMonitorDeviceNames();
            if (deviceNames.Length == 0)
                return;
            for (int i = 0; i < deviceNames.Length; i++)
            {
                string deviceName = deviceNames[i];
                if (!sampler.TrySample(deviceName, settings.BrightPixelThreshold, out BrightnessSample sample))
                    continue;
                AutoInvertState stateEntry = GetState(deviceName);
                double effectiveLum = Clamp01(sample.Luminance);
                double effectiveBrightRatio = Clamp01(sample.BrightRatio);
                stateEntry.LastLuminance = effectiveLum;
                stateEntry.LastBrightRatio = effectiveBrightRatio;
                stateEntry.HasSmoothed = false;
                stateEntry.HasFastEma = false;
                stateEntry.HasSlowEma = false;
                UpdateSmoothed(stateEntry, effectiveLum);
                UpdateDualEma(stateEntry, effectiveLum);
                bool coverageOk = !settings.UseCoverageGate || effectiveBrightRatio >= settings.BrightCoverageThreshold;
                bool shouldInvert = effectiveLum >= settings.BrightThreshold && coverageOk;
                overlayManager.SetMonitorOverlayVisible(deviceName, shouldInvert);
                stateEntry.IsInverted = shouldInvert;
                stateEntry.LastChangeTick = Environment.TickCount;
                ResetPending(stateEntry);
                logger.LogSample(deviceName, sample, stateEntry.IsInverted, stateEntry.SmoothedLuminance);
                consoleLog.Log(string.Format("INIT device={0} rawLum={1:F4} rawBrightRatio={2:F4} effLum={3:F4} invert={4}",
                    deviceName,
                    sample.Luminance,
                    sample.BrightRatio,
                    stateEntry.LastLuminance,
                    shouldInvert ? "1" : "0"));
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

        private void UpdateDualEma(AutoInvertState state, double current)
        {
            if (!settings.UseDualEma)
                return;
            if (!state.HasFastEma)
            {
                state.FastEma = current;
                state.HasFastEma = true;
            }
            else
            {
                state.FastEma = settings.FastEmaAlpha * current + (1.0 - settings.FastEmaAlpha) * state.FastEma;
            }
            if (!state.HasSlowEma)
            {
                state.SlowEma = current;
                state.HasSlowEma = true;
            }
            else
            {
                state.SlowEma = settings.SlowEmaAlpha * current + (1.0 - settings.SlowEmaAlpha) * state.SlowEma;
            }
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

        private string BuildSampleLog(string deviceName, BrightnessSample sample, AutoInvertState state, bool? desired, bool hold, double delta, double emaDiff, bool fastPath, bool dualEma)
        {
            return string.Format("SAMPLE device={0} rawLum={1:F4} rawBrightRatio={2:F4} effLum={3:F4} smoothed={4:F4} delta={5:F4} emaDiff={6:F4} inverted={7} desired={8} streak={9} pending={10} hold={11} fast={12} dualEma={13}",
                deviceName,
                sample.Luminance,
                sample.BrightRatio,
                state.LastLuminance,
                state.SmoothedLuminance,
                delta,
                emaDiff,
                state.IsInverted ? "1" : "0",
                desired.HasValue ? (desired.Value ? "1" : "0") : "-",
                state.DesiredStreak,
                state.PendingInvert.HasValue ? (state.PendingInvert.Value ? "1" : "0") : "-",
                hold ? "1" : "0",
                fastPath ? "1" : "0",
                dualEma ? "1" : "0");
        }

        private void StartBurst(long now)
        {
            if (settings.BurstDurationMs <= 0)
                return;
            burstUntilTick = now + settings.BurstDurationMs;
            UpdateTimerInterval(settings.BurstSampleMs);
            consoleLog.Log(string.Format("BURST_START interval={0} duration={1}", settings.BurstSampleMs, settings.BurstDurationMs));
        }

        private void UpdateTimerInterval(int intervalMs)
        {
            int safeInterval = Math.Max(50, intervalMs);
            lock (timerSync)
            {
                if (timer == null)
                    return;
                if (currentSampleMs == safeInterval)
                    return;
                currentSampleMs = safeInterval;
                timer.Change(currentSampleMs, currentSampleMs);
            }
        }

        private double Clamp01(double value)
        {
            if (value < 0.0) return 0.0;
            if (value > 1.0) return 1.0;
            return value;
        }
    }
}
