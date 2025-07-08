using System;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Drawing;

namespace NegativeScreen
{
    public class NegativeOverlay : Form
    {
        private IntPtr hwndMag;
        private bool trackWindow;
        private IntPtr targetWindow;
        public IntPtr HwndMag { get { return hwndMag; } }

        public NegativeOverlay(Screen screen) : this(screen.Bounds)
        {
        }

        public NegativeOverlay(IntPtr window) : this(GetWindowRectangle(window))
        {
            this.trackWindow = true;
            this.targetWindow = window;
        }

        private NegativeOverlay(Rectangle bounds) : base()
        {
            Initialize(bounds);
        }

        private void Initialize(Rectangle bounds)
        {
            this.StartPosition = FormStartPosition.Manual;
            this.Location = bounds.Location;
            this.Size = bounds.Size;
            this.TopMost = true;
            this.FormBorderStyle = FormBorderStyle.None;
            this.WindowState = FormWindowState.Normal;
            this.ShowInTaskbar = false;

            IntPtr hInst = NativeMethods.GetModuleHandle(null);
            if (hInst == IntPtr.Zero)
            {
                throw new Exception("GetModuleHandle()", Marshal.GetExceptionForHR(Marshal.GetHRForLastWin32Error()));
            }

            if (NativeMethods.SetWindowLong(this.Handle, NativeMethods.GWL_EXSTYLE, (int)ExtendedWindowStyles.WS_EX_LAYERED | (int)ExtendedWindowStyles.WS_EX_TRANSPARENT) == 0)
            {
                throw new Exception("SetWindowLong()", Marshal.GetExceptionForHR(Marshal.GetHRForLastWin32Error()));
            }

            if (!NativeMethods.SetLayeredWindowAttributes(this.Handle, 0, 255, LayeredWindowAttributeFlags.LWA_ALPHA))
            {
                throw new Exception("SetLayeredWindowAttributes()", Marshal.GetExceptionForHR(Marshal.GetHRForLastWin32Error()));
            }

            hwndMag = NativeMethods.CreateWindowEx(0,
                    NativeMethods.WC_MAGNIFIER,
                    "MagnifierWindow",
                    (int)WindowStyles.WS_CHILD | (int)WindowStyles.WS_VISIBLE,
                    0, 0, bounds.Width, bounds.Height,
                    this.Handle, IntPtr.Zero, hInst, IntPtr.Zero);

            if (hwndMag == IntPtr.Zero)
            {
                throw new Exception("CreateWindowEx()", Marshal.GetExceptionForHR(Marshal.GetHRForLastWin32Error()));
            }

            BuiltinMatrices.ChangeColorEffect(hwndMag, BuiltinMatrices.Negative);

            if (!NativeMethods.MagSetWindowSource(this.hwndMag, new RECT(bounds.Left, bounds.Top, bounds.Right, bounds.Bottom)))
            {
                throw new Exception("MagSetWindowSource()", Marshal.GetExceptionForHR(Marshal.GetHRForLastWin32Error()));
            }

            Transformation transformation = new Transformation(1.0f);
            if (!NativeMethods.MagSetWindowTransform(this.hwndMag, ref transformation))
            {
                throw new Exception("MagSetWindowTransform()", Marshal.GetExceptionForHR(Marshal.GetHRForLastWin32Error()));
            }

            try
            {
                bool preventFading = true;
                if (NativeMethods.DwmSetWindowAttribute(this.Handle, DWMWINDOWATTRIBUTE.DWMWA_EXCLUDED_FROM_PEEK, ref preventFading, sizeof(int)) != 0)
                {
                    throw new Exception("DwmSetWindowAttribute(DWMWA_EXCLUDED_FROM_PEEK)", Marshal.GetExceptionForHR(Marshal.GetHRForLastWin32Error()));
                }
            }
            catch (Exception) { }

            try
            {
                DWMFLIP3DWINDOWPOLICY threeDPolicy = DWMFLIP3DWINDOWPOLICY.DWMFLIP3D_EXCLUDEABOVE;
                if (NativeMethods.DwmSetWindowAttribute(this.Handle, DWMWINDOWATTRIBUTE.DWMWA_FLIP3D_POLICY, ref threeDPolicy, sizeof(int)) != 0)
                {
                    throw new Exception("DwmSetWindowAttribute(DWMWA_FLIP3D_POLICY)", Marshal.GetExceptionForHR(Marshal.GetHRForLastWin32Error()));
                }
            }
            catch (Exception) { }

            try
            {
                bool disallowPeek = true;
                if (NativeMethods.DwmSetWindowAttribute(this.Handle, DWMWINDOWATTRIBUTE.DWMWA_DISALLOW_PEEK, ref disallowPeek, sizeof(int)) != 0)
                {
                    throw new Exception("DwmSetWindowAttribute(DWMWA_DISALLOW_PEEK)", Marshal.GetExceptionForHR(Marshal.GetHRForLastWin32Error()));
                }
            }
            catch (Exception) { }

            this.Show();
        }

        public void UpdateBounds()
        {
            if (trackWindow)
            {
                RECT rect;
                if (NativeMethods.GetWindowRect(targetWindow, out rect))
                {
                    Rectangle b = new Rectangle(rect.left, rect.top, rect.right - rect.left, rect.bottom - rect.top);
                    if (this.Bounds != b)
                    {
                        this.Location = b.Location;
                        this.Size = b.Size;
                    }
                    NativeMethods.MagSetWindowSource(this.hwndMag, rect);
                }
            }
        }

        private static Rectangle GetWindowRectangle(IntPtr hwnd)
        {
            RECT r;
            if (NativeMethods.GetWindowRect(hwnd, out r))
            {
                return new Rectangle(r.left, r.top, r.right - r.left, r.bottom - r.top);
            }
            return Rectangle.Empty;
        }
    }
}
