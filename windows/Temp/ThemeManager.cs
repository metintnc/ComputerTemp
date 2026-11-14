using System.Windows.Forms.DataVisualization.Charting;

namespace Temp;

public enum ThemeMode
{
    Light,
    Dark
}

public static class ThemeManager
{
    public static void Apply(Control root, ThemeMode mode)
    {
        ThemePalette palette = mode == ThemeMode.Dark
            ? ThemePalette.Dark
            : ThemePalette.Light;

        root.BackColor = palette.Background;
        root.ForeColor = palette.Foreground;
        ApplyControl(root, palette, mode);
    }

    private static void ApplyControl(
        Control control,
        ThemePalette palette,
        ThemeMode mode)
    {
        if (control is Form)
        {
            control.BackColor = palette.Background;
        }
        else if (control is Panel or FlowLayoutPanel)
        {
            control.BackColor = palette.Surface;
        }

        if (control is AnimatedCardPanel card)
        {
            card.NormalBackColor = palette.Surface;
            card.HoverBackColor = mode == ThemeMode.Dark
                ? Color.FromArgb(58, 58, 82)
                : Color.FromArgb(235, 240, 248);
            if (!card.PulseEnabled)
            {
                card.BackColor = palette.Surface;
            }
        }
        else if (control is Label label)
        {
            label.ForeColor = palette.Foreground;
        }
        else if (control is Button button)
        {
            button.BackColor = palette.Surface;
            button.ForeColor = palette.Foreground;
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderColor = palette.Border;
        }
        else if (control is CheckBox checkBox)
        {
            checkBox.ForeColor = palette.Foreground;
        }
        else if (control is NumericUpDown numericUpDown)
        {
            numericUpDown.BackColor = palette.Surface;
            numericUpDown.ForeColor = palette.Foreground;
        }
        else if (control is CircularProgressGauge gauge)
        {
            gauge.BackColor = palette.Surface;
            gauge.ForeColor = palette.Foreground;
            gauge.TrackColor = palette.Border;
            gauge.Invalidate();
        }
        else if (control is Chart chart)
        {
            chart.BackColor = palette.Background;
            foreach (ChartArea area in chart.ChartAreas)
            {
                area.BackColor = palette.Surface;
                area.BackSecondaryColor = palette.Background;
                area.BackGradientStyle = mode == ThemeMode.Dark
                    ? GradientStyle.TopBottom
                    : GradientStyle.None;
                area.AxisX.LabelStyle.ForeColor = palette.Foreground;
                area.AxisY.LabelStyle.ForeColor = palette.Foreground;
                area.AxisX.TitleForeColor = palette.Foreground;
                area.AxisY.TitleForeColor = palette.Foreground;
                area.AxisX.MajorGrid.LineColor = palette.Border;
                area.AxisY.MajorGrid.LineColor = palette.Border;
            }

            foreach (Legend legend in chart.Legends)
            {
                legend.BackColor = palette.Surface;
                legend.ForeColor = palette.Foreground;
            }
        }

        foreach (Control child in control.Controls)
        {
            ApplyControl(child, palette, mode);
        }
    }

    private sealed record ThemePalette(
        Color Background,
        Color Surface,
        Color Foreground,
        Color Border)
    {
        public static ThemePalette Light => new(
            Color.White,
            Color.FromArgb(248, 250, 252),
            Color.FromArgb(15, 23, 42),
            Color.FromArgb(203, 213, 225));

        public static ThemePalette Dark => new(
            Color.FromArgb(30, 30, 46),
            Color.FromArgb(42, 42, 61),
            Color.FromArgb(241, 241, 245),
            Color.FromArgb(78, 78, 103));
    }
}
