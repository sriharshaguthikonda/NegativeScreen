using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using SharpDX;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using D3DDevice = SharpDX.Direct3D11.Device;

namespace NegativeScreen
{
    internal sealed class DxgiBrightnessSampler : IBrightnessSampler
    {
        private const int MinSampleStep = 4;
        private const int TargetSamples = 16000;
        private readonly object sync = new object();
        private Factory1 factory;
        private D3DDevice device;
        private readonly Dictionary<string, OutputState> outputs = new Dictionary<string, OutputState>(StringComparer.OrdinalIgnoreCase);

        public DxgiBrightnessSampler()
        {
            InitializeDevice();
            RefreshOutputs();
        }

        public IReadOnlyCollection<string> OutputNames
        {
            get
            {
                lock (sync)
                {
                    return new List<string>(outputs.Keys).AsReadOnly();
                }
            }
        }

        public void RefreshOutputs()
        {
            lock (sync)
            {
                ReleaseOutputs();
                if (factory == null || device == null)
                    return;
                for (int a = 0; a < factory.Adapters1.Length; a++)
                {
                    using (var adapter = factory.Adapters1[a])
                    {
                        for (int o = 0; o < adapter.Outputs.Length; o++)
                        {
                            using (var output = adapter.Outputs[o])
                            {
                                string name = output.Description.DeviceName;
                                if (string.IsNullOrEmpty(name))
                                    continue;
                                if (outputs.ContainsKey(name))
                                    continue;
                                try
                                {
                                    var output1 = output.QueryInterface<Output1>();
                                    var state = CreateOutputState(output1);
                                    if (state != null)
                                    {
                                        outputs[name] = state;
                                    }
                                    else
                                    {
                                        output1.Dispose();
                                    }
                                }
                                catch
                                {
                                    // ignore outputs that fail duplication
                                }
                            }
                        }
                    }
                }
            }
        }

        public bool TrySample(string deviceName, out BrightnessSample sample)
        {
            sample = new BrightnessSample { DeviceName = deviceName };
            OutputState state;
            lock (sync)
            {
                if (!outputs.TryGetValue(deviceName, out state))
                {
                    return false;
                }
            }
            Stopwatch sw = Stopwatch.StartNew();
            bool success = false;
            try
            {
                if (!AcquireFrame(state, out Texture2D frame))
                    return false;
                using (frame)
                {
                    device.ImmediateContext.CopyResource(frame, state.Staging);
                }
                device.ImmediateContext.Flush();
                DataBox box = device.ImmediateContext.MapSubresource(state.Staging, 0, MapMode.Read, SharpDX.Direct3D11.MapFlags.None);
                try
                {
                    double luminance = ComputeLuminance(box, state.Width, state.Height, out int samples);
                    sample.Luminance = luminance;
                    sample.Width = state.Width;
                    sample.Height = state.Height;
                    sample.SampleCount = samples;
                    success = samples > 0;
                }
                finally
                {
                    device.ImmediateContext.UnmapSubresource(state.Staging, 0);
                }
            }
            catch (SharpDXException ex)
            {
                if (ex.ResultCode == SharpDX.DXGI.ResultCode.AccessLost || ex.ResultCode == SharpDX.DXGI.ResultCode.InvalidCall)
                {
                    RebuildOutput(deviceName);
                }
                return false;
            }
            catch
            {
                return false;
            }
            finally
            {
                sw.Stop();
                sample.CaptureMs = sw.ElapsedMilliseconds;
            }
            return success;
        }

        private bool AcquireFrame(OutputState state, out Texture2D frame)
        {
            frame = null;
            OutputDuplicateFrameInformation frameInfo;
            SharpDX.DXGI.Resource screenResource = null;
            bool acquired = false;
            try
            {
                state.Duplication.AcquireNextFrame(50, out frameInfo, out screenResource);
                acquired = true;
                using (screenResource)
                {
                    frame = screenResource.QueryInterface<Texture2D>();
                }
            }
            catch (SharpDXException ex)
            {
                if (ex.ResultCode == SharpDX.DXGI.ResultCode.WaitTimeout)
                    return false;
                if (ex.ResultCode == SharpDX.DXGI.ResultCode.AccessLost || ex.ResultCode == SharpDX.DXGI.ResultCode.InvalidCall)
                    throw;
                return false;
            }
            finally
            {
                if (acquired)
                {
                    try
                    {
                        state.Duplication.ReleaseFrame();
                    }
                    catch
                    {
                    }
                }
            }
            return frame != null;
        }

        private double ComputeLuminance(DataBox box, int width, int height, out int sampleCount)
        {
            int step = ComputeSampleStep(width, height);
            long sum = 0;
            sampleCount = 0;
            int rowPitch = box.RowPitch;
            for (int y = 0; y < height; y += step)
            {
                IntPtr row = IntPtr.Add(box.DataPointer, y * rowPitch);
                for (int x = 0; x < width; x += step)
                {
                    int offset = x * 4;
                    byte b = Marshal.ReadByte(row, offset);
                    byte g = Marshal.ReadByte(row, offset + 1);
                    byte r = Marshal.ReadByte(row, offset + 2);
                    double lum = (0.2126 * r + 0.7152 * g + 0.0722 * b) / 255.0;
                    sum += (long)(lum * 1000000.0);
                    sampleCount++;
                }
            }
            if (sampleCount == 0)
                return 0.0;
            double avg = (double)sum / sampleCount / 1000000.0;
            if (avg < 0.0) return 0.0;
            if (avg > 1.0) return 1.0;
            return avg;
        }

        private int ComputeSampleStep(int width, int height)
        {
            long area = (long)width * (long)height;
            if (area <= 0)
                return MinSampleStep;
            double step = Math.Sqrt(area / (double)TargetSamples);
            int stepInt = (int)Math.Round(step);
            if (stepInt < MinSampleStep)
                stepInt = MinSampleStep;
            return stepInt;
        }

        private OutputState CreateOutputState(Output1 output1)
        {
            var bounds = output1.Description.DesktopBounds;
            int width = bounds.Right - bounds.Left;
            int height = bounds.Bottom - bounds.Top;
            if (width <= 0 || height <= 0)
                return null;
            try
            {
                var duplication = output1.DuplicateOutput(device);
                var staging = new Texture2D(device, new Texture2DDescription
                {
                    Width = width,
                    Height = height,
                    MipLevels = 1,
                    ArraySize = 1,
                    Format = Format.B8G8R8A8_UNorm,
                    SampleDescription = new SampleDescription(1, 0),
                    Usage = ResourceUsage.Staging,
                    BindFlags = BindFlags.None,
                    CpuAccessFlags = CpuAccessFlags.Read,
                    OptionFlags = ResourceOptionFlags.None
                });
                return new OutputState
                {
                    Output = output1,
                    Duplication = duplication,
                    Staging = staging,
                    Width = width,
                    Height = height
                };
            }
            catch
            {
                return null;
            }
        }

        private void RebuildOutput(string deviceName)
        {
            lock (sync)
            {
                if (outputs.TryGetValue(deviceName, out var existing))
                {
                    existing.Dispose();
                    outputs.Remove(deviceName);
                }
                if (factory == null || device == null)
                    return;
                for (int a = 0; a < factory.Adapters1.Length; a++)
                {
                    using (var adapter = factory.Adapters1[a])
                    {
                        for (int o = 0; o < adapter.Outputs.Length; o++)
                        {
                            using (var output = adapter.Outputs[o])
                            {
                                if (!string.Equals(output.Description.DeviceName, deviceName, StringComparison.OrdinalIgnoreCase))
                                    continue;
                                try
                                {
                                    var output1 = output.QueryInterface<Output1>();
                                    var state = CreateOutputState(output1);
                                    if (state != null)
                                    {
                                        outputs[deviceName] = state;
                                    }
                                    else
                                    {
                                        output1.Dispose();
                                    }
                                }
                                catch
                                {
                                }
                                return;
                            }
                        }
                    }
                }
            }
        }

        private void InitializeDevice()
        {
            try
            {
                factory = new Factory1();
                device = new D3DDevice(DriverType.Hardware, DeviceCreationFlags.BgraSupport);
            }
            catch
            {
                factory = null;
                device = null;
            }
        }

        private void ReleaseOutputs()
        {
            foreach (var kvp in outputs)
            {
                kvp.Value.Dispose();
            }
            outputs.Clear();
        }

        public void Dispose()
        {
            lock (sync)
            {
                ReleaseOutputs();
                if (device != null)
                {
                    device.Dispose();
                    device = null;
                }
                if (factory != null)
                {
                    factory.Dispose();
                    factory = null;
                }
            }
        }

        private sealed class OutputState : IDisposable
        {
            public Output1 Output;
            public OutputDuplication Duplication;
            public Texture2D Staging;
            public int Width;
            public int Height;

            public void Dispose()
            {
                if (Duplication != null)
                {
                    Duplication.Dispose();
                    Duplication = null;
                }
                if (Staging != null)
                {
                    Staging.Dispose();
                    Staging = null;
                }
                if (Output != null)
                {
                    Output.Dispose();
                    Output = null;
                }
            }
        }
    }
}
