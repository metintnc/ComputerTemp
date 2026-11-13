import SwiftUI

struct CircularGaugeView: View {
    let title: String
    let value: Double?
    let maxValue: Double
    let unit: String
    let subtitle: String?
    let accentColor: Color

    init(
        title: String,
        value: Double?,
        maxValue: Double,
        unit: String,
        subtitle: String? = nil,
        accentColor: Color
    ) {
        self.title = title
        self.value = value
        self.maxValue = maxValue
        self.unit = unit
        self.subtitle = subtitle
        self.accentColor = accentColor
    }

    private var progress: Double {
        guard let value, maxValue > 0 else { return 0 }
        return min(max(value / maxValue, 0), 1)
    }

    var body: some View {
        VStack(spacing: 10) {
            ZStack {
                Circle()
                    .stroke(accentColor.opacity(0.14), lineWidth: 9)

                Circle()
                    .trim(from: 0, to: progress)
                    .stroke(
                        AngularGradient(
                            colors: [accentColor.opacity(0.7), accentColor],
                            center: .center,
                            startAngle: .degrees(0),
                            endAngle: .degrees(360 * max(progress, 0.05))
                        ),
                        style: StrokeStyle(lineWidth: 9, lineCap: .round)
                    )
                    .rotationEffect(.degrees(-90))
                    .animation(.easeOut(duration: 0.2), value: progress)

                VStack(spacing: 0) {
                    Text(formattedValue)
                        .font(.system(size: 19, weight: .bold, design: .rounded))
                        .monospacedDigit()
                        .foregroundStyle(.primary)
                    Text(unit)
                        .font(.system(size: 10, weight: .semibold, design: .rounded))
                        .foregroundStyle(.secondary)
                }
            }
            .frame(width: 84, height: 84)

            VStack(spacing: 3) {
                Text(title)
                    .font(.caption.weight(.semibold))
                    .foregroundStyle(.primary)
                    .lineLimit(1)

                Text(subtitle ?? " ")
                    .font(.system(size: 10, weight: .medium, design: .rounded))
                    .monospacedDigit()
                    .foregroundStyle(.secondary)
                    .lineLimit(1)
            }
        }
        .padding(.vertical, 14)
        .padding(.horizontal, 10)
        .frame(maxWidth: .infinity)
        .background(
            RoundedRectangle(cornerRadius: 14, style: .continuous)
                .fill(Color(nsColor: .controlBackgroundColor))
        )
        .overlay(
            RoundedRectangle(cornerRadius: 14, style: .continuous)
                .stroke(Color.primary.opacity(0.07), lineWidth: 1)
        )
    }

    private var formattedValue: String {
        guard let value else { return "--" }
        return String(format: "%.1f", value)
    }
}

func temperatureColor(_ celsius: Double?, criticalThreshold: Double = 85.0) -> Color {
    guard let celsius else { return .secondary }
    if celsius >= criticalThreshold { return .red }
    if celsius >= criticalThreshold - 15 { return .orange }
    return .green
}
