using System.Drawing.Drawing2D;

namespace Temp;

public sealed class AnimatedCardPanel : Panel
{
    private Color borderColor = Color.Transparent;

    public AnimatedCardPanel()
    {
        SetStyle(
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.ResizeRedraw |
            ControlStyles.UserPaint,
            true);
        BorderColor = Color.Transparent;
        BorderWidth = 1;
    }

    public Color BorderColor
    {
        get => borderColor;
        set
        {
            borderColor = value;
            Invalidate();
        }
    }

    public int BorderWidth { get; set; }

    public Color HoverBackColor { get; set; } = Color.FromArgb(58, 58, 82);

    public Color NormalBackColor { get; set; } = Color.FromArgb(42, 42, 61);

    public Color PulseBorderColor { get; set; } = Color.FromArgb(239, 68, 68);

    public bool PulseEnabled { get; set; }

    public bool PulseVisible { get; set; }

    protected override void OnMouseEnter(EventArgs e)
    {
        base.OnMouseEnter(e);
        if (!PulseEnabled)
        {
            BackColor = HoverBackColor;
            Invalidate();
        }
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        base.OnMouseLeave(e);
        if (!PulseEnabled)
        {
            BackColor = NormalBackColor;
            Invalidate();
        }
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);

        if (BorderColor == Color.Transparent || BorderWidth <= 0)
        {
            return;
        }

        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        using var pen = new Pen(BorderColor, BorderWidth);
        Rectangle bounds = new(
            BorderWidth / 2,
            BorderWidth / 2,
            Width - BorderWidth,
            Height - BorderWidth);
        using var path = CreateRoundedPath(bounds, 12);
        e.Graphics.DrawPath(pen, path);
    }

    public void SetPulse(bool enabled, bool visible)
    {
        PulseEnabled = enabled;
        PulseVisible = visible;
        BorderColor = enabled && visible
            ? PulseBorderColor
            : Color.Transparent;

        if (enabled)
        {
            BackColor = visible
                ? Color.FromArgb(72, 38, 52)
                : NormalBackColor;
        }
        else
        {
            BackColor = NormalBackColor;
        }

        Invalidate();
    }

    private static GraphicsPath CreateRoundedPath(Rectangle bounds, int radius)
    {
        var path = new GraphicsPath();
        int diameter = radius * 2;
        path.AddArc(bounds.X, bounds.Y, diameter, diameter, 180, 90);
        path.AddArc(
            bounds.Right - diameter,
            bounds.Y,
            diameter,
            diameter,
            270,
            90);
        path.AddArc(
            bounds.Right - diameter,
            bounds.Bottom - diameter,
            diameter,
            diameter,
            0,
            90);
        path.AddArc(
            bounds.X,
            bounds.Bottom - diameter,
            diameter,
            diameter,
            90,
            90);
        path.CloseFigure();
        return path;
    }
}
