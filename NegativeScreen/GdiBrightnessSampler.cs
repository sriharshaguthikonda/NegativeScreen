using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace NegativeScreen
{
    internal sealed class GdiBrightnessSampler : IBrightnessSampler
    {
        private const int MinSampleSize = 64;
        private const int MaxSampleSize = 256;
        private const double RoiMargin = 0.1;
        private readonly object sync = new object();
        private readonly Dictionary<string, Screen> screens = new Dictionary<string, Screen>(StringComparer.OrdinalIgnoreCase);

        public GdiBrightnessSampler()
        {
            RefreshOutputs();
        }

        public IReadOnlyCollection<string> OutputNames
        {
            get
            {
                lock (sync)
                {
                    return new List<string>(screens.Keys).AsReadOnly();
                }
            }
        }

        public void RefreshOutputs()
        {
            lock (sync)
            {
                screens.Clear();
                foreach (var screen in Screen.AllScreens)
                {
                    screens[screen.DeviceName] = screen;
                }
            }
        }

        public bool TrySample(string deviceName, double brightPixelThreshold, out BrightnessSample sample)
        {
            sample = new BrightnessSample { DeviceName = deviceName };
            Screen screen = null;
            lock (sync)
            {
                if (!screens.TryGetValue(deviceName, out screen))
                {
                    RefreshOutputs();
                    screens.TryGetValue(deviceName, out screen);
                }
            }
            if (screen == null)
                return false;
            Rectangle bounds = screen.Bounds;
            if (bounds.Width <= 0 || bounds.Height <= 0)
                return false;
            Rectangle region = GetSampleRegion(bounds);
            if (region.Width <= 0 || region.Height <= 0)
                return false;

            Stopwatch sw = Stopwatch.StartNew();
            int sampleWidth;
            int sampleHeight;
            CalculateSampleSize(region.Width, region.Height, out sampleWidth, out sampleHeight);

            using (var bmp = new Bitmap(sampleWidth, sampleHeight, PixelFormat.Format32bppArgb))
            {
                using (var g = Graphics.FromImage(bmp))
                {
                    IntPtr hdcDest = g.GetHdc();
                    IntPtr hdcSrc = GdiNativeMethods.GetDC(IntPtr.Zero);
                    try
                    {
                        GdiNativeMethods.SetStretchBltMode(hdcDest, GdiNativeMethods.HALFTONE);
                        bool ok = GdiNativeMethods.StretchBlt(
                            hdcDest, 0, 0, sampleWidth, sampleHeight,
                            hdcSrc, region.Left, region.Top, region.Width, region.Height,
                            GdiNativeMethods.SRCCOPY);
                        if (!ok)
                            return false;
                    }
                    finally
                    {
                        GdiNativeMethods.ReleaseDC(IntPtr.Zero, hdcSrc);
                        g.ReleaseHdc(hdcDest);
                    }
                }

                double luminance = ComputeLuminance(bmp, brightPixelThreshold, out double brightRatio, out int count);
                sw.Stop();
                if (count <= 0)
                    return false;
                sample.Luminance = luminance;
                sample.BrightRatio = brightRatio;
                sample.Width = sampleWidth;
                sample.Height = sampleHeight;
                sample.SampleCount = count;
                sample.CaptureMs = sw.ElapsedMilliseconds;
            }
            return true;
        }

        private void CalculateSampleSize(int sourceWidth, int sourceHeight, out int width, out int height)
        {
            int targetWidth = Math.Max(MinSampleSize, Math.Min(MaxSampleSize, sourceWidth / 8));
            int targetHeight = Math.Max(MinSampleSize, Math.Min(MaxSampleSize, sourceHeight / 8));
            double scale = Math.Min(targetWidth / (double)sourceWidth, targetHeight / (double)sourceHeight);
            width = Math.Max(MinSampleSize, (int)Math.Round(sourceWidth * scale));
            height = Math.Max(MinSampleSize, (int)Math.Round(sourceHeight * scale));
        }

        private double ComputeLuminance(Bitmap bmp, double brightPixelThreshold, out double brightRatio, out int sampleCount)
        {
            Rectangle rect = new Rectangle(0, 0, bmp.Width, bmp.Height);
            BitmapData data = bmp.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            try
            {
                int width = bmp.Width;
                int height = bmp.Height;
                int stride = data.Stride;
                long sum = 0;
                int brightCount = 0;
                sampleCount = 0;
                for (int y = 0; y < height; y++)
                {
                    IntPtr row = IntPtr.Add(data.Scan0, y * stride);
                    for (int x = 0; x < width; x++)
                    {
                        int offset = x * 4;
                        byte b = Marshal.ReadByte(row, offset);
                        byte g = Marshal.ReadByte(row, offset + 1);
                        byte r = Marshal.ReadByte(row, offset + 2);
                        double lum = (0.2126 * r + 0.7152 * g + 0.0722 * b) / 255.0;
                        sum += (long)(lum * 1000000.0);
                        if (lum >= brightPixelThreshold)
                            brightCount++;
                        sampleCount++;
                    }
                }
                if (sampleCount == 0)
                {
                    brightRatio = 0.0;
                    return 0.0;
                }
                brightRatio = Math.Min(1.0, Math.Max(0.0, (double)brightCount / sampleCount));
                return (double)sum / sampleCount / 1000000.0;
            }
            finally
            {
                bmp.UnlockBits(data);
            }
        }

        private Rectangle GetSampleRegion(Rectangle bounds)
        {
            int marginX = (int)Math.Round(bounds.Width * RoiMargin);
            int marginY = (int)Math.Round(bounds.Height * RoiMargin);
            int left = bounds.Left + marginX;
            int top = bounds.Top + marginY;
            int width = bounds.Width - marginX * 2;
            int height = bounds.Height - marginY * 2;
            if (width <= 0 || height <= 0)
                return bounds;
            return new Rectangle(left, top, width, height);
        }

        public void Dispose()
        {
        }

        private static class GdiNativeMethods
        {
            public const int SRCCOPY = 0x00CC0020;
            public const int HALFTONE = 4;

            [DllImport("user32.dll", SetLastError = true)]
            public static extern IntPtr GetDC(IntPtr hWnd);

            [DllImport("user32.dll", SetLastError = true)]
            public static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);

            [DllImport("gdi32.dll", SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool StretchBlt(
                IntPtr hdcDest, int xDest, int yDest, int wDest, int hDest,
                IntPtr hdcSrc, int xSrc, int ySrc, int wSrc, int hSrc, int rop);

            [DllImport("gdi32.dll", SetLastError = true)]
            public static extern int SetStretchBltMode(IntPtr hdc, int mode);
        }
    }
}
