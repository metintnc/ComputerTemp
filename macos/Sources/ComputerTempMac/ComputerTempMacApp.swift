import AppKit
import SwiftUI

final class AppDelegate: NSObject, NSApplicationDelegate {
    func applicationDidFinishLaunching(_ notification: Notification) {
        NSApp.setActivationPolicy(.regular)
        NSApp.applicationIconImage = AppIconProvider.makeIcon()
        NSApp.activate(ignoringOtherApps: true)
    }
}

@main
struct ComputerTempMacApp: App {
    @NSApplicationDelegateAdaptor(AppDelegate.self) private var appDelegate
    @StateObject private var monitor = MonitoringService()
    @AppStorage("compactMenuBar") private var compactMenuBar = false

    var body: some Scene {
        Window("ComputerTemp", id: "dashboard") {
            DashboardView(monitor: monitor)
        }
        .windowResizability(.contentSize)

        MenuBarExtra {
            MenuBarContentView(monitor: monitor, compactMenuBar: $compactMenuBar)
        } label: {
            MenuBarLabelView(monitor: monitor, compactMenuBar: compactMenuBar)
        }
        .menuBarExtraStyle(.window)
    }
}

private struct MenuBarLabelView: View {
    @ObservedObject var monitor: MonitoringService
    let compactMenuBar: Bool

    var body: some View {
        HStack(spacing: 4) {
            Image(systemName: "thermometer.medium")
            Text(menuBarTitle)
                .monospacedDigit()
        }
    }

    private var menuBarTitle: String {
        let cpu = monitor.metrics.cpuTemperature.map { String(format: "%.0f°C", $0) } ?? "--°C"
        if compactMenuBar {
            return cpu
        }
        let gpu = monitor.metrics.gpuTemperature.map { String(format: "%.0f°C", $0) } ?? "--°C"
        let ram = monitor.metrics.memoryUsage.map { String(format: "%.0f%%", $0) } ?? "--%"
        return "CPU \(cpu) · GPU \(gpu) · RAM \(ram)"
    }
}

private struct MenuBarContentView: View {
    @ObservedObject var monitor: MonitoringService
    @Binding var compactMenuBar: Bool
    @Environment(\.openWindow) private var openWindow

    private var l10n: L10n { monitor.l10n }

    var body: some View {
        VStack(alignment: .leading, spacing: 10) {
            HStack {
                Text(l10n.appTitle)
                    .font(.headline)
                Spacer()
                Picker("", selection: $monitor.language) {
                    ForEach(AppLanguage.allCases) { lang in
                        Text(lang.shortLabel).tag(lang)
                    }
                }
                .pickerStyle(.segmented)
                .frame(width: 68)
                Circle()
                    .fill(monitor.isRunning ? Color.green : Color.gray)
                    .frame(width: 8, height: 8)
            }

            Divider()

            metricRow(l10n.cpuTemp, value: formatTemp(monitor.metrics.cpuTemperature))
            metricRow(l10n.gpuTemp, value: formatTemp(monitor.metrics.gpuTemperature))
            metricRow(l10n.cpuUsage, value: formatPercent(monitor.metrics.cpuUsage))
            metricRow(l10n.ramUsage, value: formatPercent(monitor.metrics.memoryUsage))

            Divider()

            Toggle(l10n.compactMenuBar, isOn: $compactMenuBar)
                .toggleStyle(.switch)
                .controlSize(.mini)
                .font(.caption)

            Divider()

            HStack {
                Button(l10n.openDashboard) {
                    NSApp.setActivationPolicy(.regular)
                    NSApp.activate(ignoringOtherApps: true)
                    openWindow(id: "dashboard")
                }
                .keyboardShortcut("o", modifiers: [.command])

                Spacer()

                Button(l10n.quit) {
                    NSApplication.shared.terminate(nil)
                }
            }
        }
        .padding(12)
        .frame(width: 260)
    }

    private func metricRow(_ label: String, value: String) -> some View {
        HStack {
            Text(label)
                .font(.caption)
                .foregroundStyle(.secondary)
            Spacer()
            Text(value)
                .font(.caption.weight(.semibold).monospacedDigit())
        }
    }

    private func formatTemp(_ value: Double?) -> String {
        guard let value else { return "--" }
        return String(format: "%.1f °C", value)
    }

    private func formatPercent(_ value: Double?) -> String {
        guard let value else { return "--" }
        return String(format: "%.1f %%", value)
    }
}
