using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using SharpDX;
using SharpDX.Direct3D11;
// Using directive removed as we're using the alias instead
using Device = SharpDX.Direct3D11.Device;
using Resource = SharpDX.DXGI.Resource;
using MapFlags = SharpDX.Direct3D11.MapFlags;
using Result = SharpDX.Result;
using SharpDX.Mathematics.Interop;
using SharpDX.Direct3D;
using System.Collections.Generic;
using SharpDX.DXGI;

namespace NegativeScreen
{
    public static class Utility
    {
        private static Device _device;
        private static Factory1 _factory;
        private static Adapter _adapter;
        private static Output _output;
        private static Output1 _output1;
        private static OutputDuplication _deskDupl;
        private static Texture2D _stagingTexture;
        private static int _screenWidth;
        private static int _screenHeight;
        private static readonly object _syncRoot = new object();
        
        // Constants for desktop duplication
        private const int WAIT_TIMEOUT = 100; // 100ms timeout

        static Utility()
        {
            try
            {
                // Initialize Direct3D device
                _device = Direct3D11Helper.CreateDevice();
                
                // Get DXGI factory and adapter
                _factory = new Factory1();
                _adapter = _factory.GetAdapter(0);
                _output = _adapter.GetOutput(0);
                _output1 = _output.QueryInterface<Output1>();
                
                // Get screen dimensions
                var outputDesc = _output.Description;
                _screenWidth = outputDesc.DesktopBounds.Right - outputDesc.DesktopBounds.Left;
                _screenHeight = outputDesc.DesktopBounds.Bottom - outputDesc.DesktopBounds.Top;
                
                // Create staging texture
                _stagingTexture = Direct3D11Helper.CreateStagingTexture(_device, _screenWidth, _screenHeight);
                
                // Get desktop duplication
                _deskDupl = _output1.DuplicateOutput(_device);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to initialize screen capture: {ex.Message}");
                throw new InvalidOperationException("Failed to initialize screen capture. Make sure the application is running with appropriate permissions.", ex);
            }
        }

        public static bool IsDark()
        {
            try
            {
                // Try to get the screen capture
                if (!TryGetScreenCapture(out var pixelData))
                    return false;
                
                // Analyze the pixels for brightness if we got valid data
                return pixelData != null && IsDark(pixelData, 128, 0.5);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in IsDark: {ex.Message}");
                return false;
            }
        }

        private static bool TryGetScreenCapture(out byte[]? pixelData)
        {
            pixelData = null;
            
            try
            {
                if (_deskDupl == null)
                {
                    ReinitializeCapture();
                    if (_deskDupl == null)
                        return false;
                }
                // Try to get the screen capture with a timeout
                OutputDuplicateFrameInformation frameInfo;
                Resource? desktopResource = null;
                
                // In SharpDX 4.2.0, we use TryAcquireNextFrame which returns a boolean
                try
                {
                    var result = _deskDupl.TryAcquireNextFrame(WAIT_TIMEOUT, out frameInfo, out desktopResource);
                    if (!result.Success)
                    {
                        return false;
                    }
                }
                catch (SharpDXException ex) when (ex.ResultCode == (int)SharpDX.DXGI.ResultCode.WaitTimeout ||
                                              ex.ResultCode == (int)SharpDX.DXGI.ResultCode.AccessLost ||
                                              ex.ResultCode == (int)SharpDX.DXGI.ResultCode.AccessDenied)
                {
                    // These are expected in some cases, just return false
                    return false;
                }
                catch (Exception ex)
                {
                    // Log other exceptions and return false
                    Debug.WriteLine($"Error in TryAcquireNextFrame: {ex.Message}");
                    return false;
                }

                
                using (desktopResource)
                {
                    // Get the screen texture
                    using (var screenTexture = desktopResource.QueryInterface<Texture2D>())
                    {
                        // Copy to staging texture
                        _device.ImmediateContext.CopyResource(screenTexture, _stagingTexture);
                        
                        // Map the staging texture
                        var dataBox = _device.ImmediateContext.MapSubresource(
                            _stagingTexture, 
                            0, 
                            MapMode.Read, 
                            SharpDX.Direct3D11.MapFlags.None, 
                            out var stream);
                        
                        try
                        {
                            // Allocate buffer for pixel data
                            pixelData = new byte[dataBox.SlicePitch];
                            
                            // Copy the pixel data
                            Marshal.Copy(dataBox.DataPointer, pixelData, 0, pixelData.Length);
                            return true;
                        }
                        finally
                        {
                            _device.ImmediateContext.UnmapSubresource(_stagingTexture, 0);
                        }
                    }
                }
            }
            catch (SharpDXException ex) when (ex.ResultCode == (int)SharpDX.DXGI.ResultCode.WaitTimeout ||
                                           ex.ResultCode == (int)SharpDX.DXGI.ResultCode.AccessLost ||
                                           ex.ResultCode == (int)SharpDX.DXGI.ResultCode.AccessDenied)
            {
                // These exceptions are expected and can be safely ignored
                return false;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error capturing screen: {ex.Message}");
                return false;
            }
        }

        private static bool IsDark(byte[] pixelData, byte tolerance = 128, double darkPercent = 0.5)
        {
            if (pixelData == null || pixelData.Length == 0)
                return false;

            int darkPixels = 0;
            int totalPixels = 0;
            int stride = 4; // 4 bytes per pixel (BGRA)

            // Sample every 20th pixel for performance
            for (int i = 0; i < pixelData.Length - stride; i += (20 * stride))
            {
                // Get BGR values (ignore alpha channel)
                byte b = pixelData[i];
                byte g = pixelData[i + 1];
                byte r = pixelData[i + 2];
                
                // Calculate brightness (simple average for performance)
                byte brightness = (byte)((r + g + b) / 3);
                
                if (brightness <= tolerance)
                    darkPixels++;
                    
                totalPixels++;
            }

            // Avoid division by zero
            if (totalPixels == 0)
                return false;
                
            double darkRatio = (double)darkPixels / totalPixels;
            return darkRatio > darkPercent;
        }

        private static void ReinitializeCapture()
        {
            lock (_syncRoot)
            {
                // Clean up existing resources
                Cleanup();
                
                try
                {
                    // Reinitialize Direct3D device
                    _device = Direct3D11Helper.CreateDevice();
                    
                    // Reinitialize DXGI objects
                    _factory = new Factory1();
                    _adapter = _factory.GetAdapter(0);
                    _output = _adapter.GetOutput(0);
                    _output1 = _output.QueryInterface<Output1>();
                    
                    // Get screen dimensions
                    var outputDesc = _output.Description;
                    _screenWidth = outputDesc.DesktopBounds.Right - outputDesc.DesktopBounds.Left;
                    _screenHeight = outputDesc.DesktopBounds.Bottom - outputDesc.DesktopBounds.Top;
                    
                    // Recreate staging texture
                    _stagingTexture = Direct3D11Helper.CreateStagingTexture(_device, _screenWidth, _screenHeight);
                    
                    // Recreate desktop duplication
                    _deskDupl = _output1.DuplicateOutput(_device);
                    
                    // Release the frame to avoid resource leaks
                    _deskDupl.ReleaseFrame();
                }
                catch (SharpDXException ex) when (ex.ResultCode == (int)SharpDX.DXGI.ResultCode.AccessDenied ||
                                               ex.ResultCode == (int)SharpDX.DXGI.ResultCode.AccessLost)
                {
                    // These are expected in some cases, just log and continue
                    Debug.WriteLine($"Expected error during reinitialization: {ex.Message}");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Failed to reinitialize screen capture: {ex.Message}");
                    throw new InvalidOperationException("Failed to reinitialize screen capture after access loss.", ex);
                }
            }
        }

        private static void Cleanup()
        {
            // Release Direct3D resources
            try
            {
                _deskDupl?.ReleaseFrame();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error releasing frame: {ex.Message}");
            }
            
            _stagingTexture?.Dispose();
            _deskDupl?.Dispose();
            _output1?.Dispose();
            _output?.Dispose();
            _adapter?.Dispose();
            _factory?.Dispose();
            _device?.Dispose();
            
            // Clear references - using null! to suppress null warnings since we know what we're doing
            _stagingTexture = null!;
            _deskDupl = null!;
            _output1 = null!;
            _output = null!;
            _adapter = null!;
            _factory = null!;
            _device = null!;
        }
    }
}
