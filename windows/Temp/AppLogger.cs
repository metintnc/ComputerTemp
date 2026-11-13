using System.Text;

namespace Temp;

public enum AppLogLevel
{
    Information,
    Warning,
    Error,
    Critical
}

public static class AppLogger
{
    private static readonly object SyncRoot = new();

    private static string LogDirectory => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "CpuMonitor",
        "logs");

    private static string LogFilePath => Path.Combine(
        LogDirectory,
        $"app-{DateTime.UtcNow:yyyyMMdd}.log");

    public static void Write(
        AppLogLevel level,
        string message,
        Exception? exception = null)
    {
        try
        {
            Directory.CreateDirectory(LogDirectory);
            var builder = new StringBuilder()
                .Append(DateTimeOffset.Now.ToString("O"))
                .Append(" [")
                .Append(level)
                .Append("] ")
                .AppendLine(message);

            if (exception is not null)
            {
                builder.AppendLine(exception.ToString());
            }

            lock (SyncRoot)
            {
                File.AppendAllText(LogFilePath, builder.ToString());
            }
        }
        catch
        {
            // Logging must never prevent the application from starting or closing.
        }
    }

    public static void Information(string message) =>
        Write(AppLogLevel.Information, message);

    public static void Warning(string message, Exception? exception = null) =>
        Write(AppLogLevel.Warning, message, exception);

    public static void Error(string message, Exception? exception = null) =>
        Write(AppLogLevel.Error, message, exception);

    public static void Critical(string message, Exception? exception = null) =>
        Write(AppLogLevel.Critical, message, exception);
}
