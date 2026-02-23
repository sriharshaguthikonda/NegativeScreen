using System;
using System.Drawing;
using System.Windows.Forms;

namespace NegativeScreen
{
    internal class CursorOverlay : Form
    {
        private const int OverlaySize = 48;
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
            this.Size = new Size(OverlaySize, OverlaySize);
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
            Point pos = Cursor.Position;
            Point newLocation = new Point(pos.X - OverlaySize / 2, pos.Y - OverlaySize / 2);
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
            Rectangle bounds = new Rectangle(0, 0, OverlaySize, OverlaySize);
            cursor.Draw(e.Graphics, bounds);
        }
    }
}
