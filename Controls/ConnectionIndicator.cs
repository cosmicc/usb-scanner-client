using System.ComponentModel;
using System.Drawing.Drawing2D;

namespace UsbScannerClient.Controls;

internal sealed class ConnectionIndicator : Control
{
    private bool isConnected;

    public ConnectionIndicator()
    {
        Size = new Size(18, 18);
        MinimumSize = new Size(18, 18);
        MaximumSize = new Size(18, 18);
        SetStyle(
            ControlStyles.AllPaintingInWmPaint
                | ControlStyles.OptimizedDoubleBuffer
                | ControlStyles.ResizeRedraw
                | ControlStyles.UserPaint,
            true);
    }

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool IsConnected
    {
        get => isConnected;
        set
        {
            if (isConnected == value)
            {
                return;
            }

            isConnected = value;
            Invalidate();
        }
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);

        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        Rectangle bounds = ClientRectangle;
        bounds.Inflate(-2, -2);

        Color fill = IsConnected ? Color.FromArgb(0, 225, 85) : Color.FromArgb(255, 45, 45);
        Color border = IsConnected ? Color.FromArgb(0, 145, 54) : Color.FromArgb(178, 0, 0);

        using var fillBrush = new SolidBrush(fill);
        using var borderPen = new Pen(border, 1.4F);
        e.Graphics.FillEllipse(fillBrush, bounds);
        e.Graphics.DrawEllipse(borderPen, bounds);
    }
}
