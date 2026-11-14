using System.Drawing.Drawing2D;

namespace Temp;

public sealed class CircularProgressGauge : Control
{
    private float? value;
    private float maximum = 100;

    public CircularProgressGauge()
    {
        SetStyle(
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.ResizeRedraw |
            ControlStyles.UserPaint,
            true);

        BackColor = Color.FromArgb(42, 42, 61);
        ForeColor = Color.FromArgb(241, 241, 245);
        TrackColor = Color.FromArgb(67, 67, 88);
        Caption = "Gauge";
        Unit = "%";
        Size = new Size(150, 150);
    }

    public float? Value
    {
        get => value;
        set
        {
            this.value = value is float number && float.IsFinite(number)
                ? Math.Clamp(number, 0, Maximum)
                : null;
            Invalidate();
        }
    }

    public float Maximum
    {
        get => maximum;
        set
        {
            maximum = value > 0 && float.IsFinite(value) ? value : 100;
            if (this.value is float current)
            {
                this.value = Math.Clamp(current, 0, maximum);
            }

            Invalidate();
        }
    }

    public string Caption { get; set; }

    public string Unit { get; set; }

    public Color TrackColor { get; set; }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        e.Graphics.Clear(BackColor);

        int diameter = Math.Min(ClientSize.Width, ClientSize.Height) - 24;
        if (diameter <= 0)
        {
            return;
        }

        float left = (ClientSize.Width - diameter) / 2F;
        float top = (ClientSize.Height - diameter) / 2F;
        var bounds = new RectangleF(left, top, diameter, diameter);
        const float thickness = 11F;

        using var trackPen = new Pen(TrackColor, thickness)
        {
            StartCap = LineCap.Round,
            EndCap = LineCap.Round
        };
        e.Graphics.DrawArc(trackPen, bounds, -90, 359.9F);

        if (Value is float current)
        {
            float sweep = current / Maximum * 360F;
            using var progressPen = new Pen(GetProgressColor(current), thickness)
            {
                StartCap = LineCap.Round,
                EndCap = LineCap.Round
            };
            e.Graphics.DrawArc(progressPen, bounds, -90, sweep);
        }

        string valueText = Value is float number
            ? $"{number:0.0}{Unit}"
            : "--";
        using var valueBrush = new SolidBrush(ForeColor);
        using var captionBrush = new SolidBrush(
            Color.FromArgb(
                Math.Max(0, ForeColor.R - 35),
                Math.Max(0, ForeColor.G - 35),
                Math.Max(0, ForeColor.B - 35)));
        using var valueFont = new Font(
            Font.FontFamily,
            Math.Max(12, Font.Size + 3),
            FontStyle.Bold);
        using var captionFont = new Font(
            Font.FontFamily,
            Math.Max(7, Font.Size - 1),
            FontStyle.Regular);

        DrawCenteredText(
            e.Graphics,
            valueText,
            valueFont,
            valueBrush,
            new RectangleF(0, ClientSize.Height / 2F - 18, ClientSize.Width, 32));
        DrawCenteredText(
            e.Graphics,
            Caption,
            captionFont,
            captionBrush,
            new RectangleF(0, ClientSize.Height / 2F + 13, ClientSize.Width, 22));
    }

    private Color GetProgressColor(float current)
    {
        float ratio = current / Maximum * 100F;
        return ratio < 60
            ? Color.FromArgb(59, 130, 246)
            : ratio <= 80
                ? Color.FromArgb(249, 115, 22)
                : Color.FromArgb(239, 68, 68);
    }

    private static void DrawCenteredText(
        Graphics graphics,
        string text,
        Font font,
        Brush brush,
        RectangleF bounds)
    {
        using var format = new StringFormat
        {
            Alignment = StringAlignment.Center,
            LineAlignment = StringAlignment.Center
        };
        graphics.DrawString(text, font, brush, bounds, format);
    }
}
