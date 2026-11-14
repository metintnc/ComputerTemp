import Foundation

public enum AppLanguage: String, CaseIterable, Identifiable, Sendable {
    case english = "en"
    case turkish = "tr"

    public var id: String { rawValue }

    public var shortLabel: String {
        switch self {
        case .english: return "EN"
        case .turkish: return "TR"
        }
    }

    public var displayName: String {
        switch self {
        case .english: return "English"
        case .turkish: return "Türkçe"
        }
    }
}

public struct L10n {
    public let language: AppLanguage

    public init(_ language: AppLanguage) {
        self.language = language
    }

    private func t(_ en: String, _ tr: String) -> String {
        language == .english ? en : tr
    }

    // General & Header
    public var appTitle: String { "ComputerTemp" }
    public var start: String { t("Start", "Başlat") }
    public var stop: String { t("Pause", "Durdur") }
    public var alwaysOnTop: String { t("Pin", "Üstte Tut") }
    public var openDashboard: String { t("Open Dashboard", "Paneli Aç") }
    public var quit: String { t("Quit", "Çıkış") }
    public var compactMenuBar: String { t("Compact Menu Bar", "Kompakt Üst Çubuk") }

    // Metrics
    public var cpuTemp: String { t("CPU Temp", "CPU Sıcaklık") }
    public var gpuTemp: String { t("GPU Temp", "GPU Sıcaklık") }
    public var cpuUsage: String { t("CPU Load", "CPU Kullanım") }
    public var ramUsage: String { t("Memory", "RAM Kullanım") }
    public var coresUnit: String { t("Cores", "Çekirdek") }
    public var minMaxPrefix: String { "Min / Max" }

    // Thermal levels
    public func thermalLevel(_ level: ThermalLevel) -> String {
        switch level {
        case .nominal: return t("Nominal", "Normal")
        case .fair: return t("Fair", "Ilık")
        case .serious: return t("Elevated", "Yüksek")
        case .critical: return t("Critical", "Kritik")
        }
    }

    // Alerts
    public func criticalBanner(_ threshold: Int) -> String {
        t(
            "CPU temperature exceeded the critical threshold (\(threshold) °C)!",
            "CPU sıcaklığı kritik eşiğin (\(threshold) °C) üzerine çıktı!"
        )
    }

    public var notificationTitle: String {
        t("Critical CPU Temperature", "Kritik CPU Sıcaklığı")
    }

    public func notificationBody(_ temp: Double) -> String {
        t(
            String(format: "CPU temperature reached %.1f °C.", temp),
            String(format: "CPU sıcaklığı %.1f °C seviyesine ulaştı.", temp)
        )
    }

    // Chart
    public func chartTitle(_ count: Int) -> String {
        t("Temperature History (\(count) samples)", "Sıcaklık Geçmişi (\(count) ölçüm)")
    }
    public var reset: String { t("Reset", "Sıfırla") }
    public var timeAxis: String { t("Time", "Zaman") }
    public var tempAxis: String { t("Temperature", "Sıcaklık") }
    public var sensorAxis: String { t("Sensor", "Sensör") }
    public var criticalLine: String { t("Critical", "Kritik") }

    // Sensors
    public func hardwareSensors(_ count: Int) -> String {
        t("Hardware Sensors (\(count))", "Donanım Sensörleri (\(count))")
    }
    public var noSensorsFound: String {
        t("No active SMC thermal sensors detected.", "Aktif SMC sıcaklık sensörü okunamadı.")
    }

    public func sensorLabel(_ kind: SensorKind, key: String) -> String {
        switch kind {
        case .cpu: return "CPU (\(key))"
        case .gpu: return "GPU (\(key))"
        case .palmRest: return t("Chassis / Palm Rest (\(key))", "Kasa / Palm Rest (\(key))")
        case .battery: return t("Battery (\(key))", "Batarya (\(key))")
        case .wifi: return t("Wi-Fi Module (\(key))", "Wi-Fi Modülü (\(key))")
        }
    }

    // Controls
    public var refreshInterval: String { t("Refresh:", "Yenileme:") }
    public var secondsUnit: String { t("s", "sn") }
    public var alertThreshold: String { t("Alert at:", "Uyarı Eşiği:") }
}

