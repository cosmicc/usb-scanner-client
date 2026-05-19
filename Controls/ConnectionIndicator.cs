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

        Color fill = IsConnected ? Color.FromArgb(24, 156, 78) : Color.FromArgb(196, 43, 43);
        Color border = IsConnected ? Color.FromArgb(14, 105, 52) : Color.FromArgb(126, 25, 25);

        using var fillBrush = new SolidBrush(fill);
        using var borderPen = new Pen(border, 1.4F);
        e.Graphics.FillEllipse(fillBrush, bounds);
        e.Graphics.DrawEllipse(borderPen, bounds);
    }
}
