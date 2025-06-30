using System;
using System.Runtime.InteropServices;
using SharpDX;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using Device = SharpDX.Direct3D11.Device;
using MapFlags = SharpDX.Direct3D11.MapFlags;
using Resource = SharpDX.Direct3D11.Resource;
using MapMode = SharpDX.Direct3D11.MapMode;

namespace NegativeScreen
{
    internal static class Direct3D11Helper
    {
        public static Device CreateDevice()
        {
            // Create Direct3D 11 device with BGRA support
            try
            {
                var device = new Device(
                    SharpDX.Direct3D.DriverType.Hardware,
                    DeviceCreationFlags.BgraSupport | 
                    (System.Diagnostics.Debugger.IsAttached ? DeviceCreationFlags.Debug : DeviceCreationFlags.None));
                
                #if DEBUG
                device.DebugName = "NegativeScreenDevice";
                #endif
                
                return device;
            }
            catch (Exception ex)
            {
                throw new Exception("Failed to create Direct3D11 device. Make sure you have the latest graphics drivers installed.", ex);
            }
        }

        public static Texture2D CreateStagingTexture(Device device, int width, int height, Format format = Format.B8G8R8A8_UNorm)
        {
            var desc = new Texture2DDescription
            {
                Width = width,
                Height = height,
                MipLevels = 1,
                ArraySize = 1,
                Format = format,
                SampleDescription = new SampleDescription(1, 0),
                Usage = ResourceUsage.Staging,
                BindFlags = BindFlags.None,
                CpuAccessFlags = CpuAccessFlags.Read,
                OptionFlags = ResourceOptionFlags.None
            };

            return new Texture2D(device, desc);
        }

        public static bool CopyResource(DeviceContext context, Resource source, Resource destination)
        {
            try
            {
                context.CopyResource(source, destination);
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to copy resource: {ex.Message}");
                return false;
            }
        }

        public static DataStream? MapResource(DeviceContext context, Resource resource, MapMode mapMode, MapFlags mapFlags, out DataStream? stream)
        {
            try
            {
                var dataBox = context.MapSubresource(resource, 0, mapMode, mapFlags, out var streamOut);
                stream = new DataStream(dataBox.DataPointer, dataBox.SlicePitch, true, true);
                return stream;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to map resource: {ex.Message}");
                stream = null;
                return null;
            }
        }

        public static void UnmapResource(DeviceContext context, Resource resource)
        {
            try
            {
                context.UnmapSubresource(resource, 0);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to unmap resource: {ex.Message}");
            }
        }

        public static void SafeDispose<T>(ref T? resource) where T : class, IDisposable
        {
            if (resource != null)
            {
                try
                {
                    resource.Dispose();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error disposing {typeof(T).Name}: {ex.Message}");
                }
                finally
                {
                    resource = null;
                }
            }
        }
    }
}
