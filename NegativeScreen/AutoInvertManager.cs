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
            states.Clear();
        }

        private void OnTimer(object state)
        {
            if (disposed)
                return;
            if (Interlocked.Exchange(ref isRunning, 1) == 1)
                return;
            List<Tuple<NegativeOverlay, bool>> visibilitySnapshot = null;
            try
            {
                if (settings.HideOverlays)
                {
                    visibilitySnapshot = overlayManager.HideOverlaysForAutoInvert();
                }
                string[] deviceNames = overlayManager.GetActiveMonitorDeviceNames();
                if (deviceNames.Length == 0)
                    return;
                long now = Environment.TickCount;
                for (int i = 0; i < deviceNames.Length; i++)
                {
                    string deviceName = deviceNames[i];
                    if (!sampler.TrySample(deviceName, out BrightnessSample sample))
                        continue;
                    AutoInvertState stateEntry = GetState(deviceName);
                    stateEntry.LastLuminance = sample.Luminance;
                    bool? desired = null;
                    if (!stateEntry.IsInverted && sample.Luminance >= settings.BrightThreshold)
                        desired = true;
                    else if (stateEntry.IsInverted && sample.Luminance <= settings.DarkThreshold)
                        desired = false;
                    if (desired.HasValue)
                    {
                        if (stateEntry.PendingInvert.HasValue && stateEntry.PendingInvert.Value == desired.Value)
                        {
                            if (settings.DwellMs <= 0 || unchecked(now - stateEntry.PendingSinceTick) >= settings.DwellMs)
                            {
                                ApplyInvert(deviceName, desired.Value, stateEntry);
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
                    logger.LogSample(deviceName, sample, stateEntry.IsInverted);
                }
            }
            finally
            {
                if (visibilitySnapshot != null)
                {
                    overlayManager.RestoreOverlaysAfterAutoInvert(visibilitySnapshot);
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

        private void ApplyInvert(string deviceName, bool invert, AutoInvertState state)
        {
            overlayManager.SetMonitorOverlayVisible(deviceName, invert);
            state.IsInverted = invert;
            state.PendingInvert = null;
        }
    }
}
