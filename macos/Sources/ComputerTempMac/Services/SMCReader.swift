import Foundation
import IOKit

/// Reads CPU, GPU, and system temperatures from AppleSMC via IOKit.
public final class SMCReader: @unchecked Sendable {
    private struct SMCKeyData {
        struct Vers {
            var major: CUnsignedChar = 0
            var minor: CUnsignedChar = 0
            var build: CUnsignedChar = 0
            var reserved: CUnsignedChar = 0
            var release: CUnsignedShort = 0
        }
        struct PLimitData {
            var version: UInt16 = 0
            var length: UInt16 = 0
            var cpuPLimit: UInt32 = 0
            var gpuPLimit: UInt32 = 0
            var memPLimit: UInt32 = 0
        }
        struct KeyInfo {
            var dataSize: UInt32 = 0
            var dataType: UInt32 = 0
            var dataAttributes: UInt8 = 0
        }
        var key: UInt32 = 0
        var vers = Vers()
        var pLimitData = PLimitData()
        var keyInfo = KeyInfo()
        var padding: UInt16 = 0
        var result: UInt8 = 0
        var status: UInt8 = 0
        var data8: UInt8 = 0
        var data32: UInt32 = 0
        var bytes: (
            UInt8, UInt8, UInt8, UInt8, UInt8, UInt8, UInt8, UInt8,
            UInt8, UInt8, UInt8, UInt8, UInt8, UInt8, UInt8, UInt8,
            UInt8, UInt8, UInt8, UInt8, UInt8, UInt8, UInt8, UInt8,
            UInt8, UInt8, UInt8, UInt8, UInt8, UInt8, UInt8, UInt8
        ) = (0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0)
    }

    private var connection: io_connect_t = 0
    private var keyInfoCache: [String: (dataSize: UInt32, dataType: UInt32)] = [:]

    private let cpuKeys = [
        "Tp09", "Tp0T", "Tp01", "Tp05", "Tp0D", "Tp0H", "Tp0L", "Tp0P", "Tp0X", "Tp0b",
        "TC0P", "TC0E", "TC0F", "TC0D", "TC0H"
    ]
    private let gpuKeys = [
        "Tg05", "Tg0D", "Tg0L", "Tg0T", "Tg0f", "Tg0j",
        "TG0P", "TG0D", "TG0H"
    ]
    private let systemKeys: [(key: String, kind: SensorKind)] = [
        ("Ts0P", .palmRest),
        ("TB0T", .battery),
        ("TW0P", .wifi)
    ]

    public init() {
        open()
    }

    deinit {
        if connection != 0 {
            IOServiceClose(connection)
            connection = 0
        }
    }

    public func readTemperatures() -> (cpu: Double?, gpu: Double?, sensors: [SensorReading]) {
        var sensors: [SensorReading] = []

        var cpuValues: [Double] = []
        for key in cpuKeys {
            if let temp = readTemperature(key: key) {
                cpuValues.append(temp)
                sensors.append(SensorReading(id: key, kind: .cpu, celsius: temp))
            }
        }

        var gpuValues: [Double] = []
        for key in gpuKeys {
            if let temp = readTemperature(key: key) {
                gpuValues.append(temp)
                sensors.append(SensorReading(id: key, kind: .gpu, celsius: temp))
            }
        }

        for item in systemKeys {
            if let temp = readTemperature(key: item.key) {
                sensors.append(SensorReading(id: item.key, kind: item.kind, celsius: temp))
            }
        }

        let cpuAvg = cpuValues.isEmpty ? nil : cpuValues.reduce(0, +) / Double(cpuValues.count)
        let gpuAvg = gpuValues.isEmpty ? nil : gpuValues.reduce(0, +) / Double(gpuValues.count)
        return (cpuAvg, gpuAvg, sensors)
    }

    private func open() {
        let matching = IOServiceMatching("AppleSMC")
        var iterator: io_iterator_t = 0
        guard IOServiceGetMatchingServices(kIOMainPortDefault, matching, &iterator) == KERN_SUCCESS else {
            return
        }
        defer { IOObjectRelease(iterator) }

        let device = IOIteratorNext(iterator)
        guard device != 0 else { return }
        defer { IOObjectRelease(device) }

        var conn: io_connect_t = 0
        if IOServiceOpen(device, mach_task_self_, 0, &conn) == KERN_SUCCESS {
            connection = conn
        }
    }

    private func readTemperature(key: String) -> Double? {
        guard connection != 0, let info = keyInfo(for: key) else { return nil }

        var input = SMCKeyData()
        var output = SMCKeyData()
        input.key = fourCharCode(key)
        input.keyInfo.dataSize = info.dataSize
        input.data8 = 5 // SMC_CMD_READ_BYTES

        var outSize = MemoryLayout<SMCKeyData>.stride
        let res = IOConnectCallStructMethod(
            connection,
            2,
            &input,
            MemoryLayout<SMCKeyData>.stride,
            &output,
            &outSize
        )
        guard res == KERN_SUCCESS, output.result == 0 else { return nil }

        let b = output.bytes
        let value: Double?
        if info.dataType == fourCharCode("flt ") && info.dataSize == 4 {
            var floatVal: Float = 0
            let raw = [b.0, b.1, b.2, b.3]
            memcpy(&floatVal, raw, 4)
            value = Double(floatVal)
        } else if info.dataType == fourCharCode("sp78") && info.dataSize == 2 {
            let intVal = (Int16(b.0) << 8) | Int16(b.1)
            value = Double(intVal) / 256.0
        } else {
            value = nil
        }

        guard let temp = value, temp > 1.0, temp < 130.0 else { return nil }
        return temp
    }

    private func keyInfo(for key: String) -> (dataSize: UInt32, dataType: UInt32)? {
        if let cached = keyInfoCache[key] {
            return cached
        }

        var input = SMCKeyData()
        var output = SMCKeyData()
        input.key = fourCharCode(key)
        input.data8 = 9 // SMC_CMD_READ_KEYINFO

        var outSize = MemoryLayout<SMCKeyData>.stride
        let res = IOConnectCallStructMethod(
            connection,
            2,
            &input,
            MemoryLayout<SMCKeyData>.stride,
            &output,
            &outSize
        )
        guard res == KERN_SUCCESS, output.result == 0, output.keyInfo.dataSize > 0 else {
            return nil
        }

        let info = (output.keyInfo.dataSize, output.keyInfo.dataType)
        keyInfoCache[key] = info
        return info
    }

    private func fourCharCode(_ string: String) -> UInt32 {
        string.utf8.reduce(0) { ($0 << 8) | UInt32($1) }
    }
}
