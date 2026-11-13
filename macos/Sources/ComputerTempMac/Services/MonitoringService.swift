import AppKit
import Foundation
import UserNotifications

@MainActor
public final class MonitoringService: ObservableObject {
    @Published public private(set) var metrics: HardwareMetrics
    @Published public private(set) var history: [TemperatureSample] = []
    @Published public private(set) var isRunning = false
    @Published public private(set) var isCriticalAlertActive = false

    @Published public private(set) var minCpuTemp: Double?
    @Published public private(set) var maxCpuTemp: Double?
    @Published public private(set) var minGpuTemp: Double?
    @Published public private(set) var maxGpuTemp: Double?

    @Published public var language: AppLanguage {
        didSet {
            UserDefaults.standard.set(language.rawValue, forKey: "appLanguage")
        }
    }

    public var l10n: L10n { L10n(language) }

    @Published public var refreshInterval: Double = 1.0 {
        didSet {
            let clamped = min(max(refreshInterval, 0.5), 10.0)
            if clamped != refreshInterval { refreshInterval = clamped }
            if isRunning { restartTimer() }
        }
    }

    @Published public var criticalThresholdCelsius: Double = 85.0 {
        didSet {
            let clamped = min(max(criticalThresholdCelsius, 40.0), 115.0)
            if clamped != criticalThresholdCelsius { criticalThresholdCelsius = clamped }
        }
    }

    private let smcReader = SMCReader()
    private let systemReader = SystemMetricsReader()
    private let samplingQueue = DispatchQueue(label: "com.computertemp.sampling", qos: .utility)
    private var isSampling = false
    private let cpuTempAverage = MovingAverage(capacity: 5)
    private let maxHistoryPoints = 60
    private let alertCooldown: TimeInterval = 300
    private var lastAlertDate: Date?
    private var timer: Timer?
    private var activityToken: NSObjectProtocol?
    private let cpuName: String

    public init() {
        let savedLang = UserDefaults.standard.string(forKey: "appLanguage")
        self.language = savedLang.flatMap(AppLanguage.init(rawValue:)) ?? .english
        self.cpuName = systemReader.readCpuName()
        let totalGB = Double(ProcessInfo.processInfo.physicalMemory) / 1_073_741_824.0
        self.metrics = HardwareMetrics(
            cpuName: cpuName,
            cpuTemperature: nil,
            gpuTemperature: nil,
            cpuUsage: nil,
            memoryUsage: nil,
            memoryUsedGB: nil,
            memoryTotalGB: totalGB
        )
        requestNotificationPermissionIfBundled()
        start()
    }

    public func start() {
        guard !isRunning else { return }
        isRunning = true
        if activityToken == nil {
            activityToken = ProcessInfo.processInfo.beginActivity(
                options: [.userInitiatedAllowingIdleSystemSleep, .latencyCritical],
                reason: "Live hardware temperature monitoring"
            )
        }
        sampleOnce()
        restartTimer()
    }

    public func stop() {
        isRunning = false
        timer?.invalidate()
        timer = nil
        if let token = activityToken {
            ProcessInfo.processInfo.endActivity(token)
            activityToken = nil
        }
    }

    public func toggle() {
        isRunning ? stop() : start()
    }

    public func clearHistory() {
        history.removeAll()
        cpuTempAverage.reset()
        minCpuTemp = nil
        maxCpuTemp = nil
        minGpuTemp = nil
        maxGpuTemp = nil
    }

    private func restartTimer() {
        timer?.invalidate()
        let newTimer = Timer(timeInterval: refreshInterval, repeats: true) { [weak self] _ in
            Task { @MainActor in
                self?.sampleOnce()
            }
        }
        RunLoop.main.add(newTimer, forMode: .common)
        timer = newTimer
    }

    private func sampleOnce() {
        guard !isSampling else { return }
        isSampling = true

        let smc = smcReader
        let sys = systemReader
        let name = cpuName

        samplingQueue.async { [weak self] in
            let temps = smc.readTemperatures()
            let cpuUsage = sys.readCpuUsage()
            let mem = sys.readMemoryUsage()
            let thermalLevel = ThermalLevel(processState: ProcessInfo.processInfo.thermalState)
            let now = Date()

            Task { @MainActor [weak self] in
                guard let self else { return }
                defer { self.isSampling = false }

                let smoothedCpuTemp = temps.cpu.map { self.cpuTempAverage.add($0) }
                self.updateMinMax(cpu: smoothedCpuTemp, gpu: temps.gpu)

                let current = HardwareMetrics(
                    timestamp: now,
                    cpuName: name,
                    cpuTemperature: smoothedCpuTemp,
                    gpuTemperature: temps.gpu,
                    cpuUsage: cpuUsage,
                    memoryUsage: mem.percent,
                    memoryUsedGB: mem.usedGB,
                    memoryTotalGB: mem.totalGB,
                    thermalLevel: thermalLevel,
                    sensors: temps.sensors
                )
                self.metrics = current

                self.history.append(TemperatureSample(
                    timestamp: now,
                    cpuTemperature: smoothedCpuTemp,
                    gpuTemperature: temps.gpu
                ))
                if self.history.count > self.maxHistoryPoints {
                    self.history.removeFirst(self.history.count - self.maxHistoryPoints)
                }

                self.evaluateCriticalTemperature(smoothedCpuTemp, at: now)
            }
        }
    }

    private func updateMinMax(cpu: Double?, gpu: Double?) {
        if let cpu {
            minCpuTemp = minCpuTemp.map { min($0, cpu) } ?? cpu
            maxCpuTemp = maxCpuTemp.map { max($0, cpu) } ?? cpu
        }
        if let gpu {
            minGpuTemp = minGpuTemp.map { min($0, gpu) } ?? gpu
            maxGpuTemp = maxGpuTemp.map { max($0, gpu) } ?? gpu
        }
    }

    private func evaluateCriticalTemperature(_ temperature: Double?, at now: Date) {
        guard let temperature, temperature >= criticalThresholdCelsius else {
            isCriticalAlertActive = false
            return
        }

        isCriticalAlertActive = true
        if let lastAlertDate, now.timeIntervalSince(lastAlertDate) < alertCooldown {
            return
        }

        lastAlertDate = now
        NSSound.beep()
        sendNotification(
            title: l10n.notificationTitle,
            body: l10n.notificationBody(temperature)
        )
    }

    private func requestNotificationPermissionIfBundled() {
        guard Bundle.main.bundleIdentifier != nil else { return }
        UNUserNotificationCenter.current().requestAuthorization(options: [.alert, .sound]) { _, _ in }
    }

    private func sendNotification(title: String, body: String) {
        guard Bundle.main.bundleIdentifier != nil else { return }
        let content = UNMutableNotificationContent()
        content.title = title
        content.body = body
        content.sound = .default

        let request = UNNotificationRequest(
            identifier: UUID().uuidString,
            content: content,
            trigger: nil
        )
        UNUserNotificationCenter.current().add(request)
    }
}
