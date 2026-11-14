import Foundation

public enum SensorKind: Sendable {
    case cpu
    case gpu
    case palmRest
    case battery
    case wifi
}

public struct SensorReading: Identifiable, Sendable {
    public let id: String
    public let kind: SensorKind
    public let celsius: Double

    public init(id: String, kind: SensorKind, celsius: Double) {
        self.id = id
        self.kind = kind
        self.celsius = celsius
    }
}

public enum ThermalLevel: String, Sendable {
    case nominal
    case fair
    case serious
    case critical

    public init(processState: ProcessInfo.ThermalState) {
        switch processState {
        case .nominal: self = .nominal
        case .fair: self = .fair
        case .serious: self = .serious
        case .critical: self = .critical
        @unknown default: self = .nominal
        }
    }
}

public struct HardwareMetrics: Sendable {
    public let timestamp: Date
    public let cpuName: String
    public let cpuTemperature: Double?
    public let gpuTemperature: Double?
    public let cpuUsage: Double?
    public let memoryUsage: Double?
    public let memoryUsedGB: Double?
    public let memoryTotalGB: Double
    public let thermalLevel: ThermalLevel
    public let sensors: [SensorReading]

    public init(
        timestamp: Date = Date(),
        cpuName: String,
        cpuTemperature: Double?,
        gpuTemperature: Double?,
        cpuUsage: Double?,
        memoryUsage: Double?,
        memoryUsedGB: Double?,
        memoryTotalGB: Double,
        thermalLevel: ThermalLevel = .nominal,
        sensors: [SensorReading] = []
    ) {
        self.timestamp = timestamp
        self.cpuName = cpuName
        self.cpuTemperature = cpuTemperature
        self.gpuTemperature = gpuTemperature
        self.cpuUsage = cpuUsage
        self.memoryUsage = memoryUsage
        self.memoryUsedGB = memoryUsedGB
        self.memoryTotalGB = memoryTotalGB
        self.thermalLevel = thermalLevel
        self.sensors = sensors
    }
}

public struct TemperatureSample: Identifiable, Sendable {
    public let id = UUID()
    public let timestamp: Date
    public let cpuTemperature: Double?
    public let gpuTemperature: Double?
}

public final class MovingAverage {
    private let capacity: Int
    private var values: [Double] = []

    public init(capacity: Int = 5) {
        self.capacity = max(1, capacity)
    }

    public func add(_ value: Double) -> Double {
        values.append(value)
        if values.count > capacity {
            values.removeFirst(values.count - capacity)
        }
        return values.reduce(0, +) / Double(values.count)
    }

    public func reset() {
        values.removeAll()
    }
}
