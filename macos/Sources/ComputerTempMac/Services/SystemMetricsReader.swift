import Darwin
import Foundation

/// Reads CPU brand name, CPU usage percentage, and Memory usage details using Mach/sysctl APIs.
public final class SystemMetricsReader: @unchecked Sendable {
    private var previousCpuTicks: (user: UInt64, system: UInt64, idle: UInt64, nice: UInt64)?

    public init() {}

    public func readCpuName() -> String {
        var size: size_t = 0
        sysctlbyname("machdep.cpu.brand_string", nil, &size, nil, 0)
        if size > 0 {
            var buffer = [CChar](repeating: 0, count: size)
            if sysctlbyname("machdep.cpu.brand_string", &buffer, &size, nil, 0) == 0 {
                return String(cString: buffer)
            }
        }
        return "Apple Silicon"
    }

    public func readCpuUsage() -> Double? {
        var numCPUs: natural_t = 0
        var cpuInfo: processor_info_array_t?
        var numCpuInfo: mach_msg_type_number_t = 0

        let result = host_processor_info(
            mach_host_self(),
            PROCESSOR_CPU_LOAD_INFO,
            &numCPUs,
            &cpuInfo,
            &numCpuInfo
        )
        guard result == KERN_SUCCESS, let info = cpuInfo else { return nil }
        defer {
            let size = vm_size_t(numCpuInfo) * vm_size_t(MemoryLayout<integer_t>.stride)
            vm_deallocate(mach_task_self_, vm_address_t(bitPattern: info), size)
        }

        var totalUser: UInt64 = 0
        var totalSystem: UInt64 = 0
        var totalIdle: UInt64 = 0
        var totalNice: UInt64 = 0

        for i in 0..<Int(numCPUs) {
            let base = i * Int(CPU_STATE_MAX)
            totalUser += UInt64(UInt32(bitPattern: info[base + Int(CPU_STATE_USER)]))
            totalSystem += UInt64(UInt32(bitPattern: info[base + Int(CPU_STATE_SYSTEM)]))
            totalIdle += UInt64(UInt32(bitPattern: info[base + Int(CPU_STATE_IDLE)]))
            totalNice += UInt64(UInt32(bitPattern: info[base + Int(CPU_STATE_NICE)]))
        }

        let current = (user: totalUser, system: totalSystem, idle: totalIdle, nice: totalNice)
        defer { previousCpuTicks = current }

        guard let prev = previousCpuTicks else { return nil }

        let dUser = current.user &- prev.user
        let dSystem = current.system &- prev.system
        let dIdle = current.idle &- prev.idle
        let dNice = current.nice &- prev.nice
        let total = dUser + dSystem + dIdle + dNice
        guard total > 0 else { return nil }

        let used = dUser + dSystem + dNice
        return (Double(used) / Double(total)) * 100.0
    }

    public func readMemoryUsage() -> (percent: Double?, usedGB: Double?, totalGB: Double) {
        let totalBytes = ProcessInfo.processInfo.physicalMemory
        let totalGB = Double(totalBytes) / 1_073_741_824.0

        var stats = vm_statistics64()
        var count = mach_msg_type_number_t(MemoryLayout<vm_statistics64_data_t>.stride / MemoryLayout<integer_t>.stride)

        let result = withUnsafeMutablePointer(to: &stats) {
            $0.withMemoryRebound(to: integer_t.self, capacity: Int(count)) {
                host_statistics64(mach_host_self(), HOST_VM_INFO64, $0, &count)
            }
        }
        guard result == KERN_SUCCESS, totalBytes > 0 else {
            return (nil, nil, totalGB)
        }

        let pageSize = UInt64(vm_kernel_page_size)
        let active = UInt64(stats.active_count) * pageSize
        let wired = UInt64(stats.wire_count) * pageSize
        let compressed = UInt64(stats.compressor_page_count) * pageSize
        let usedBytes = active + wired + compressed

        let usedGB = Double(usedBytes) / 1_073_741_824.0
        let percent = min(100.0, (Double(usedBytes) / Double(totalBytes)) * 100.0)
        return (percent, usedGB, totalGB)
    }
}
