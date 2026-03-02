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

        public bool TrySample(string deviceName, out BrightnessSample sample)
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

            Stopwatch sw = Stopwatch.StartNew();
            int sampleWidth;
            int sampleHeight;
            CalculateSampleSize(bounds, out sampleWidth, out sampleHeight);

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
                            hdcSrc, bounds.Left, bounds.Top, bounds.Width, bounds.Height,
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

                double luminance = ComputeLuminance(bmp, out int count);
                sw.Stop();
                sample.Luminance = luminance;
                sample.Width = sampleWidth;
                sample.Height = sampleHeight;
                sample.SampleCount = count;
                sample.CaptureMs = sw.ElapsedMilliseconds;
            }
            return true;
        }

        private void CalculateSampleSize(Rectangle bounds, out int width, out int height)
        {
            int targetWidth = Math.Max(MinSampleSize, Math.Min(MaxSampleSize, bounds.Width / 8));
            int targetHeight = Math.Max(MinSampleSize, Math.Min(MaxSampleSize, bounds.Height / 8));
            double scale = Math.Min(targetWidth / (double)bounds.Width, targetHeight / (double)bounds.Height);
            width = Math.Max(MinSampleSize, (int)Math.Round(bounds.Width * scale));
            height = Math.Max(MinSampleSize, (int)Math.Round(bounds.Height * scale));
        }

        private double ComputeLuminance(Bitmap bmp, out int sampleCount)
        {
            Rectangle rect = new Rectangle(0, 0, bmp.Width, bmp.Height);
            BitmapData data = bmp.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            try
            {
                int width = bmp.Width;
                int height = bmp.Height;
                int stride = data.Stride;
                long sum = 0;
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
                        sampleCount++;
                    }
                }
                if (sampleCount == 0)
                    return 0.0;
                return (double)sum / sampleCount / 1000000.0;
            }
            finally
            {
                bmp.UnlockBits(data);
            }
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
