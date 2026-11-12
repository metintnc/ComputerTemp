using System.Diagnostics;

namespace Temp;

public enum SafetyAction
{
    AlertOnly,
    ShutdownWindows,
    RunExecutable
}

public sealed class SafetyMonitor
{
    private readonly float thresholdCelsius;
    private readonly TimeSpan sustainedDuration;
    private readonly SafetyAction action;
    private readonly string? executablePath;
    private readonly bool testMode;
    private DateTimeOffset? dangerousSince;
    private bool actionTriggered;

    public SafetyMonitor(
        float thresholdCelsius,
        TimeSpan sustainedDuration,
        SafetyAction action,
        string? executablePath = null,
        bool testMode = true)
    {
        if (thresholdCelsius <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(thresholdCelsius));
        }

        if (sustainedDuration <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(sustainedDuration));
        }

        this.thresholdCelsius = thresholdCelsius;
        this.sustainedDuration = sustainedDuration;
        this.action = action;
        this.executablePath = executablePath;
        this.testMode = testMode;
    }

    public bool Observe(float temperatureCelsius, Action<string> alert)
    {
        ArgumentNullException.ThrowIfNull(alert);

        if (temperatureCelsius < thresholdCelsius)
        {
            dangerousSince = null;
            actionTriggered = false;
            return false;
        }

        dangerousSince ??= DateTimeOffset.UtcNow;
        if (actionTriggered ||
            DateTimeOffset.UtcNow - dangerousSince.Value < sustainedDuration)
        {
            return false;
        }

        actionTriggered = true;
        string message =
            $"CPU sıcaklığı {temperatureCelsius:F1} °C değerinde " +
            $"{sustainedDuration.TotalSeconds:F0} saniye kaldı.";
        alert(message);
        ExecuteAction();
        return true;
    }

    private void ExecuteAction()
    {
        if (testMode && action != SafetyAction.AlertOnly)
        {
            AppLogger.Warning(
                $"Güvenlik eylemi test modu nedeniyle atlandı: {action}.");
            return;
        }

        switch (action)
        {
            case SafetyAction.AlertOnly:
                return;
            case SafetyAction.ShutdownWindows:
                Process.Start(new ProcessStartInfo
                {
                    FileName = "shutdown.exe",
                    Arguments = "/s /t 30 /c \"CPU sıcaklığı kritik seviyede\"",
                    UseShellExecute = false,
                    CreateNoWindow = true
                });
                return;
            case SafetyAction.RunExecutable:
                if (string.IsNullOrWhiteSpace(executablePath))
                {
                    throw new InvalidOperationException(
                        "Güvenlik eylemi için çalıştırılacak dosya yolu belirtilmedi.");
                }

                Process.Start(new ProcessStartInfo
                {
                    FileName = executablePath,
                    UseShellExecute = true,
                    WorkingDirectory = Path.GetDirectoryName(executablePath)
                        ?? AppContext.BaseDirectory
                });
                return;
            default:
                throw new InvalidOperationException(
                    $"Desteklenmeyen güvenlik eylemi: {action}.");
        }
    }
}
