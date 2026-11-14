import AppKit
import Charts
import SwiftUI

struct DashboardView: View {
    @ObservedObject var monitor: MonitoringService
    @State private var alwaysOnTop = false
    @State private var showSensors = true

    private var l10n: L10n { monitor.l10n }

    var body: some View {
        ScrollView {
            VStack(alignment: .leading, spacing: 16) {
                headerSection

                if monitor.isCriticalAlertActive {
                    criticalBanner
                }

                metricsGrid
                chartSection
                sensorsSection
                controlsSection
            }
            .padding(20)
        }
        .background(Color(nsColor: .windowBackgroundColor))
        .frame(minWidth: 560, minHeight: 610)
        .onChange(of: alwaysOnTop) { newValue in
            setWindowFloating(newValue)
        }
    }

    private var headerSection: some View {
        HStack(spacing: 12) {
            Image(nsImage: AppIconProvider.makeIcon(size: 128))
                .resizable()
                .interpolation(.high)
                .frame(width: 40, height: 40)

            VStack(alignment: .leading, spacing: 2) {
                HStack(spacing: 8) {
                    Text(l10n.appTitle)
                        .font(.title3.bold())
                    thermalBadge
                }
                Text(monitor.metrics.cpuName)
                    .font(.caption)
                    .foregroundStyle(.secondary)
            }

            Spacer()

            Picker("", selection: $monitor.language) {
                ForEach(AppLanguage.allCases) { lang in
                    Text(lang.shortLabel).tag(lang)
                }
            }
            .pickerStyle(.segmented)
            .frame(width: 76)

            Toggle(isOn: $alwaysOnTop) {
                Label(l10n.alwaysOnTop, systemImage: alwaysOnTop ? "pin.fill" : "pin")
                    .font(.caption.weight(.medium))
            }
            .toggleStyle(.button)

            Button(action: { monitor.toggle() }) {
                Label(
                    monitor.isRunning ? l10n.stop : l10n.start,
                    systemImage: monitor.isRunning ? "pause.fill" : "play.fill"
                )
                .font(.caption.weight(.semibold))
            }
            .buttonStyle(.borderedProminent)
            .tint(monitor.isRunning ? .orange : .green)
        }
    }

    private var thermalBadge: some View {
        let level = monitor.metrics.thermalLevel
        let color: Color = switch level {
        case .nominal: .green
        case .fair: .yellow
        case .serious: .orange
        case .critical: .red
        }

        return HStack(spacing: 4) {
            Circle()
                .fill(color)
                .frame(width: 6, height: 6)
            Text(l10n.thermalLevel(level))
                .font(.caption2.weight(.semibold))
        }
        .padding(.horizontal, 8)
        .padding(.vertical, 3)
        .background(color.opacity(0.14))
        .foregroundStyle(color)
        .clipShape(Capsule())
    }

    private var criticalBanner: some View {
        HStack(spacing: 8) {
            Image(systemName: "exclamationmark.triangle.fill")
                .foregroundStyle(.white)
            Text(l10n.criticalBanner(Int(monitor.criticalThresholdCelsius)))
                .font(.caption.weight(.semibold))
                .foregroundStyle(.white)
            Spacer()
        }
        .padding(10)
        .background(RoundedRectangle(cornerRadius: 10, style: .continuous).fill(Color.red))
    }

    private var metricsGrid: some View {
        HStack(spacing: 12) {
            CircularGaugeView(
                title: l10n.cpuTemp,
                value: monitor.metrics.cpuTemperature,
                maxValue: 110,
                unit: "°C",
                subtitle: minMaxText(min: monitor.minCpuTemp, max: monitor.maxCpuTemp),
                accentColor: temperatureColor(
                    monitor.metrics.cpuTemperature,
                    criticalThreshold: monitor.criticalThresholdCelsius
                )
            )
            CircularGaugeView(
                title: l10n.gpuTemp,
                value: monitor.metrics.gpuTemperature,
                maxValue: 110,
                unit: "°C",
                subtitle: minMaxText(min: monitor.minGpuTemp, max: monitor.maxGpuTemp),
                accentColor: temperatureColor(
                    monitor.metrics.gpuTemperature,
                    criticalThreshold: monitor.criticalThresholdCelsius
                )
            )
            CircularGaugeView(
                title: l10n.cpuUsage,
                value: monitor.metrics.cpuUsage,
                maxValue: 100,
                unit: "%",
                subtitle: "\(ProcessInfo.processInfo.activeProcessorCount) \(l10n.coresUnit)",
                accentColor: .blue
            )
            CircularGaugeView(
                title: l10n.ramUsage,
                value: monitor.metrics.memoryUsage,
                maxValue: 100,
                unit: "%",
                subtitle: memorySubtitle,
                accentColor: .purple
            )
        }
    }

    private var chartSection: some View {
        VStack(alignment: .leading, spacing: 8) {
            HStack {
                Text(l10n.chartTitle(monitor.history.count))
                    .font(.caption.weight(.semibold))
                    .foregroundStyle(.secondary)
                Spacer()
                Button(l10n.reset) {
                    monitor.clearHistory()
                }
                .buttonStyle(.borderless)
                .font(.caption)
            }

            Chart {
                ForEach(monitor.history) { sample in
                    if let cpu = sample.cpuTemperature {
                        AreaMark(
                            x: .value(l10n.timeAxis, sample.timestamp),
                            yStart: .value(l10n.tempAxis, 20),
                            yEnd: .value(l10n.tempAxis, cpu)
                        )
                        .foregroundStyle(
                            LinearGradient(
                                colors: [Color.orange.opacity(0.20), Color.orange.opacity(0.02)],
                                startPoint: .top,
                                endPoint: .bottom
                            )
                        )
                        .interpolationMethod(.linear)

                        LineMark(
                            x: .value(l10n.timeAxis, sample.timestamp),
                            y: .value(l10n.tempAxis, cpu)
                        )
                        .foregroundStyle(by: .value(l10n.sensorAxis, "CPU"))
                        .lineStyle(StrokeStyle(lineWidth: 2))
                        .interpolationMethod(.linear)
                    }
                    if let gpu = sample.gpuTemperature {
                        LineMark(
                            x: .value(l10n.timeAxis, sample.timestamp),
                            y: .value(l10n.tempAxis, gpu)
                        )
                        .foregroundStyle(by: .value(l10n.sensorAxis, "GPU"))
                        .lineStyle(StrokeStyle(lineWidth: 2))
                        .interpolationMethod(.linear)
                    }
                }

                RuleMark(y: .value(l10n.criticalLine, monitor.criticalThresholdCelsius))
                    .lineStyle(StrokeStyle(lineWidth: 1, dash: [4, 4]))
                    .foregroundStyle(.red.opacity(0.55))
            }
            .chartYScale(domain: 20...110)
            .chartForegroundStyleScale([
                "CPU": Color.orange,
                "GPU": Color.cyan
            ])
            .transaction { $0.animation = nil }
            .frame(height: 180)
            .padding(12)
            .background(
                RoundedRectangle(cornerRadius: 14, style: .continuous)
                    .fill(Color(nsColor: .controlBackgroundColor))
            )
            .overlay(
                RoundedRectangle(cornerRadius: 14, style: .continuous)
                    .stroke(Color.primary.opacity(0.07), lineWidth: 1)
            )
        }
    }

    private var sensorsSection: some View {
        DisclosureGroup(isExpanded: $showSensors) {
            if monitor.metrics.sensors.isEmpty {
                Text(l10n.noSensorsFound)
                    .font(.caption)
                    .foregroundStyle(.secondary)
                    .padding(.top, 6)
            } else {
                LazyVGrid(
                    columns: [GridItem(.flexible()), GridItem(.flexible())],
                    spacing: 8
                ) {
                    ForEach(monitor.metrics.sensors) { sensor in
                        let color = temperatureColor(
                            sensor.celsius,
                            criticalThreshold: monitor.criticalThresholdCelsius
                        )
                        HStack(spacing: 8) {
                            Circle()
                                .fill(color)
                                .frame(width: 7, height: 7)
                            Text(l10n.sensorLabel(sensor.kind, key: sensor.id))
                                .font(.caption)
                                .foregroundStyle(.secondary)
                                .lineLimit(1)
                            Spacer()
                            Text(String(format: "%.1f °C", sensor.celsius))
                                .font(.caption.weight(.semibold).monospacedDigit())
                                .foregroundStyle(color)
                        }
                        .padding(.horizontal, 10)
                        .padding(.vertical, 8)
                        .background(
                            RoundedRectangle(cornerRadius: 10, style: .continuous)
                                .fill(Color(nsColor: .controlBackgroundColor))
                        )
                        .overlay(
                            RoundedRectangle(cornerRadius: 10, style: .continuous)
                                .stroke(Color.primary.opacity(0.06), lineWidth: 1)
                        )
                    }
                }
                .padding(.top, 8)
            }
        } label: {
            Text(l10n.hardwareSensors(monitor.metrics.sensors.count))
                .font(.caption.weight(.semibold))
                .foregroundStyle(.secondary)
        }
    }

    private var controlsSection: some View {
        HStack(spacing: 16) {
            HStack(spacing: 6) {
                Text(l10n.refreshInterval)
                    .font(.caption)
                    .foregroundStyle(.secondary)
                Stepper(
                    value: $monitor.refreshInterval,
                    in: 0.5...10.0,
                    step: 0.5
                ) {
                    Text(String(format: "%.1f %@", monitor.refreshInterval, l10n.secondsUnit))
                        .font(.caption.monospacedDigit())
                }
            }

            Spacer()

            HStack(spacing: 6) {
                Text(l10n.alertThreshold)
                    .font(.caption)
                    .foregroundStyle(.secondary)
                Stepper(
                    value: $monitor.criticalThresholdCelsius,
                    in: 50...110,
                    step: 5
                ) {
                    Text("\(Int(monitor.criticalThresholdCelsius)) °C")
                        .font(.caption.monospacedDigit())
                }
            }
        }
        .padding(12)
        .background(
            RoundedRectangle(cornerRadius: 12, style: .continuous)
                .fill(Color(nsColor: .controlBackgroundColor))
        )
        .overlay(
            RoundedRectangle(cornerRadius: 12, style: .continuous)
                .stroke(Color.primary.opacity(0.06), lineWidth: 1)
        )
    }

    private var memorySubtitle: String {
        guard let used = monitor.metrics.memoryUsedGB else {
            return String(format: "%.0f GB", monitor.metrics.memoryTotalGB)
        }
        return String(format: "%.1f / %.0f GB", used, monitor.metrics.memoryTotalGB)
    }

    private func minMaxText(min: Double?, max: Double?) -> String {
        guard let min, let max else { return "\(l10n.minMaxPrefix): --" }
        return String(format: "%.0f° / %.0f°", min, max)
    }

    private func setWindowFloating(_ floating: Bool) {
        for window in NSApp.windows where window.title == "ComputerTemp" {
            window.level = floating ? .floating : .normal
        }
    }
}
