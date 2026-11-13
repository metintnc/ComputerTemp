using System.Text.Json;

namespace Temp;

public sealed class AppSettings
{
    public decimal RefreshIntervalSeconds { get; set; } = 1.0M;
    public decimal CriticalTemperatureCelsius { get; set; } = 85.0M;
    public bool UseTemperatureColors { get; set; } = true;
    public bool StartWithWindows { get; set; }
    public ThemeMode Theme { get; set; } = ThemeMode.Dark;
    public bool SafetyEnabled { get; set; }
    public bool SafetyTestMode { get; set; } = true;
    public decimal SafetyTemperatureCelsius { get; set; } = 95.0M;
    public int SafetyDurationSeconds { get; set; } = 10;
    public SafetyAction SafetyAction { get; set; } = SafetyAction.AlertOnly;
    public string? SafetyExecutablePath { get; set; }

    public static string FilePath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "CpuMonitor",
        "settings.json");

    public static AppSettings Load(out string? error)
    {
        error = null;
        try
        {
            if (!File.Exists(FilePath))
            {
                return new AppSettings();
            }

            AppSettings settings = JsonSerializer.Deserialize<AppSettings>(
                File.ReadAllText(FilePath)) ?? new AppSettings();
            settings.Normalize();
            return settings;
        }
        catch (IOException ex)
        {
            error = $"Ayarlar okunamadı: {ex.Message}";
            AppLogger.Warning(error, ex);
        }
        catch (JsonException ex)
        {
            error = $"Ayarlar dosyası geçersiz: {ex.Message}";
            AppLogger.Warning(error, ex);
        }

        return new AppSettings();
    }

    public void Save()
    {
        Normalize();
        Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
        string json = JsonSerializer.Serialize(
            this,
            new JsonSerializerOptions { WriteIndented = true });
        string temporaryPath = $"{FilePath}.{Guid.NewGuid():N}.tmp";
        File.WriteAllText(temporaryPath, json);
        File.Move(temporaryPath, FilePath, true);
    }

    private void Normalize()
    {
        RefreshIntervalSeconds = Math.Clamp(
            RefreshIntervalSeconds,
            0.1M,
            60M);
        CriticalTemperatureCelsius = Math.Clamp(
            CriticalTemperatureCelsius,
            1M,
            150M);
        SafetyTemperatureCelsius = Math.Clamp(
            SafetyTemperatureCelsius,
            1M,
            150M);
        SafetyDurationSeconds = Math.Clamp(
            SafetyDurationSeconds,
            1,
            3600);
    }
}
