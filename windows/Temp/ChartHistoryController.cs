using System.Runtime.InteropServices;
using System.Windows.Forms.DataVisualization.Charting;

namespace Temp;

public sealed class ChartHistoryController
{
    private const int MaximumPoints = 60;
    private readonly Chart chart;
    private double windowSeconds = 60;

    public ChartHistoryController(Chart chart)
    {
        this.chart = chart;
    }

    public void SetWindow(double seconds)
    {
        windowSeconds = seconds;
        if (chart.ChartAreas.Count == 0)
        {
            return;
        }

        DateTime now = DateTime.Now;
        ChartArea area = chart.ChartAreas["History"];
        area.AxisX.Minimum = now.AddSeconds(-seconds).ToOADate();
        area.AxisX.Maximum = now.ToOADate();
    }

    public void Update(HardwareMetrics metrics)
    {
        if (chart.IsDisposed ||
            chart.ChartAreas.FindByName("History") is null ||
            chart.Series.FindByName("CPU sıcaklığı") is null)
        {
            return;
        }

        DateTime now = DateTime.Now;
        double xValue = now.ToOADate();
        Series cpuTemperature = chart.Series["CPU sıcaklığı"];
        Series cpuUsage = chart.Series["CPU kullanımı"];
        Series gpuTemperature = chart.Series["GPU sıcaklığı"];
        Series gpuUsage = chart.Series["GPU kullanımı"];

        chart.SuspendLayout();
        try
        {
            AddPoint(cpuTemperature, xValue, metrics.CpuTemperature);
            AddPoint(cpuUsage, xValue, metrics.CpuUsage);
            AddPoint(gpuTemperature, xValue, metrics.GpuTemperature);
            AddPoint(gpuUsage, xValue, metrics.GpuUsage);

            ChartArea area = chart.ChartAreas["History"];
            area.AxisX.LabelStyle.Format = "HH:mm:ss";
            area.AxisX.Minimum = GetOldestXValue(
                new[] { cpuTemperature, cpuUsage, gpuTemperature, gpuUsage },
                now.ToOADate());
            area.AxisX.Maximum = now.ToOADate();
        }
        finally
        {
            chart.ResumeLayout();
        }
    }

    public void ExportPng(IWin32Window owner, Action<string> onSuccess)
    {
        using var dialog = new SaveFileDialog
        {
            AddExtension = true,
            DefaultExt = "png",
            FileName = $"hardware-history-{DateTime.Now:yyyyMMdd-HHmmss}.png",
            Filter = "PNG dosyası (*.png)|*.png",
            Title = "Grafiği dışa aktar"
        };

        if (dialog.ShowDialog(owner) != DialogResult.OK)
        {
            return;
        }

        try
        {
            chart.SaveImage(dialog.FileName, ChartImageFormat.Png);
            onSuccess(dialog.FileName);
        }
        catch (Exception ex) when (
            ex is IOException or ExternalException)
        {
            AppLogger.Error("Grafik dışa aktarılamadı.", ex);
            MessageBox.Show(
                owner,
                $"Grafik dışa aktarılamadı:\n{ex.Message}",
                "Dışa aktarma hatası",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }

    private static void AddPoint(Series series, double x, float? value)
    {
        if (value is not float number)
        {
            return;
        }

        series.Points.AddXY(x, number);
        while (series.Points.Count > MaximumPoints)
        {
            series.Points.RemoveAt(0);
        }
    }

    private double GetOldestXValue(
        IEnumerable<Series> seriesCollection,
        double fallback)
    {
        double oldest = fallback;
        foreach (Series series in seriesCollection)
        {
            if (series.Points.Count > 0)
            {
                oldest = Math.Min(oldest, series.Points[0].XValue);
            }
        }

        return Math.Max(
            oldest,
            DateTime.Now.AddSeconds(-windowSeconds).ToOADate());
    }
}
