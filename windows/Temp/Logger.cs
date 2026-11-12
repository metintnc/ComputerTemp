using System.Globalization;
using System.Text;
using System.Text.Json;

namespace Temp;

public sealed class Logger : IDisposable
{
    private readonly object syncRoot = new();
    private readonly string csvPath;
    private bool disposed;

    public Logger(string? csvPath = null)
    {
        this.csvPath = csvPath ??
            Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "CpuMonitor",
                "metrics.csv");

        Directory.CreateDirectory(Path.GetDirectoryName(this.csvPath)!);

        if (!File.Exists(this.csvPath))
        {
            File.WriteAllText(
                this.csvPath,
                "Timestamp,CPU Temperature (C),GPU Temperature (C),CPU Load (%),GPU Load (%)\r\n",
                new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        }
    }

    public string CsvPath => csvPath;

    public void LogMetrics(HardwareMetrics metrics)
    {
        ObjectDisposedException.ThrowIf(disposed, this);

        var entry = CreateEntry(metrics);
        string line = string.Join(
            ",",
            entry.Timestamp.ToString("O", CultureInfo.InvariantCulture),
            FormatValue(entry.CpuTemperature),
            FormatValue(entry.GpuTemperature),
            FormatValue(entry.CpuLoad),
            FormatValue(entry.GpuLoad));

        lock (syncRoot)
        {
            File.AppendAllText(
                csvPath,
                line + Environment.NewLine,
                new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        }
    }

    public void ExportJson(string destinationPath)
    {
        ObjectDisposedException.ThrowIf(disposed, this);

        List<LogEntry> entries = ReadCsvEntries();
        string json = JsonSerializer.Serialize(
            entries,
            new JsonSerializerOptions { WriteIndented = true });

        File.WriteAllText(
            destinationPath,
            json,
            new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
    }

    public void Dispose()
    {
        disposed = true;
        GC.SuppressFinalize(this);
    }

    private List<LogEntry> ReadCsvEntries()
    {
        lock (syncRoot)
        {
            return File.ReadLines(csvPath)
                .Skip(1)
                .Select(ParseCsvLine)
                .Where(entry => entry is not null)
                .Select(entry => entry!)
                .ToList();
        }
    }

    private static LogEntry? ParseCsvLine(string line)
    {
        string[] columns = line.Split(',');
        if ((columns.Length != 4 && columns.Length != 5) ||
            !DateTimeOffset.TryParse(
                columns[0],
                CultureInfo.InvariantCulture,
                DateTimeStyles.RoundtripKind,
                out DateTimeOffset timestamp))
        {
            return null;
        }

        return new LogEntry(
            timestamp,
            ParseValue(columns[1]),
            ParseValue(columns[2]),
            ParseValue(columns[3]),
            columns.Length == 5 ? ParseValue(columns[4]) : null);
    }

    private static LogEntry CreateEntry(HardwareMetrics metrics)
    {
        return new LogEntry(
            DateTimeOffset.Now,
            metrics.CpuTemperature,
            metrics.GpuTemperature,
            metrics.CpuUsage,
            metrics.GpuUsage);
    }

    private static string FormatValue(float? value)
    {
        return value?.ToString("F2", CultureInfo.InvariantCulture) ?? string.Empty;
    }

    private static float? ParseValue(string value)
    {
        return float.TryParse(
            value,
            NumberStyles.Float,
            CultureInfo.InvariantCulture,
            out float result)
            ? result
            : null;
    }
}

public sealed record LogEntry(
    DateTimeOffset Timestamp,
    float? CpuTemperature,
    float? GpuTemperature,
    float? CpuLoad,
    float? GpuLoad);
