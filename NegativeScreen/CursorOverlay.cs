using System;
using System.Drawing;
using System.Windows.Forms;

namespace NegativeScreen
{
    internal class CursorOverlay : Form
    {
        private const int MinOverlaySize = 24;
        private readonly Timer timer;
        private Point lastLocation = new Point(int.MinValue, int.MinValue);

        public CursorOverlay()
        {
            this.FormBorderStyle = FormBorderStyle.None;
            this.ShowInTaskbar = false;
            this.StartPosition = FormStartPosition.Manual;
            this.TopMost = true;
            this.BackColor = Color.Magenta;
            this.TransparencyKey = Color.Magenta;
            this.Size = new Size(MinOverlaySize, MinOverlaySize);
            this.DoubleBuffered = true;

            timer = new Timer();
            timer.Interval = 16;
            timer.Tick += (s, e) => UpdatePosition();
        }

        protected override bool ShowWithoutActivation
        {
            get { return true; }
        }

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ExStyle |= (int)ExtendedWindowStyles.WS_EX_TRANSPARENT;
                cp.ExStyle |= (int)ExtendedWindowStyles.WS_EX_LAYERED;
                cp.ExStyle |= (int)ExtendedWindowStyles.WS_EX_TOOLWINDOW;
                cp.ExStyle |= (int)ExtendedWindowStyles.WS_EX_TOPMOST;
                cp.ExStyle |= (int)ExtendedWindowStyles.WS_EX_NOACTIVATE;
                return cp;
            }
        }

        public void Start()
        {
            this.Show();
            timer.Start();
        }

        public void Stop()
        {
            timer.Stop();
            this.Hide();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                timer.Stop();
                timer.Dispose();
            }
            base.Dispose(disposing);
        }

        private void UpdatePosition()
        {
            Cursor cursor = Cursor.Current ?? Cursors.Arrow;
            Size size = cursor.Size;
            if (size.Width < MinOverlaySize || size.Height < MinOverlaySize)
            {
                size = new Size(Math.Max(size.Width, MinOverlaySize), Math.Max(size.Height, MinOverlaySize));
            }
            if (this.Size != size)
            {
                this.Size = size;
            }
            Point hotSpot = cursor.HotSpot;
            Point pos = Cursor.Position;
            Point newLocation = new Point(pos.X - hotSpot.X, pos.Y - hotSpot.Y);
            if (newLocation != lastLocation)
            {
                this.Location = newLocation;
                lastLocation = newLocation;
            }
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            Cursor cursor = Cursor.Current ?? Cursors.Arrow;
            Rectangle bounds = new Rectangle(0, 0, this.Width, this.Height);
            cursor.Draw(e.Graphics, bounds);
        }
    }
}
