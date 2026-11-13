import AppKit

enum AppIconProvider {
    static func makeIcon(size: CGFloat = 512) -> NSImage {
        let image = NSImage(size: NSSize(width: size, height: size))
        image.lockFocus()
        defer { image.unlockFocus() }

        guard let ctx = NSGraphicsContext.current?.cgContext else {
            return image
        }

        let rect = CGRect(x: 0, y: 0, width: size, height: size)
        let inset = size * 0.06
        let squircleRect = rect.insetBy(dx: inset, dy: inset)
        let cornerRadius = size * 0.22
        let bgPath = CGPath(
            roundedRect: squircleRect,
            cornerWidth: cornerRadius,
            cornerHeight: cornerRadius,
            transform: nil
        )

        // Background gradient (dark slate)
        ctx.saveGState()
        ctx.addPath(bgPath)
        ctx.clip()

        let colorSpace = CGColorSpaceCreateDeviceRGB()
        let bgColors = [
            NSColor(calibratedRed: 0.14, green: 0.16, blue: 0.22, alpha: 1.0).cgColor,
            NSColor(calibratedRed: 0.08, green: 0.09, blue: 0.13, alpha: 1.0).cgColor
        ] as CFArray
        if let gradient = CGGradient(colorsSpace: colorSpace, colors: bgColors, locations: [0.0, 1.0]) {
            ctx.drawLinearGradient(
                gradient,
                start: CGPoint(x: 0, y: size),
                end: CGPoint(x: size, y: 0),
                options: []
            )
        }
        ctx.restoreGState()

        // Subtle outer border
        ctx.setStrokeColor(NSColor.white.withAlphaComponent(0.14).cgColor)
        ctx.setLineWidth(size * 0.012)
        ctx.addPath(bgPath)
        ctx.strokePath()

        // Temperature arc ring
        let center = CGPoint(x: size / 2, y: size / 2)
        let arcRadius = size * 0.30
        let arcWidth = size * 0.045

        ctx.setLineWidth(arcWidth)
        ctx.setLineCap(.round)
        ctx.setStrokeColor(NSColor.white.withAlphaComponent(0.10).cgColor)
        ctx.addArc(
            center: center,
            radius: arcRadius,
            startAngle: .pi * 1.25,
            endAngle: -.pi * 0.25,
            clockwise: true
        )
        ctx.strokePath()

        // Active warm arc (orange -> red)
        let activeArc = CGMutablePath()
        activeArc.addArc(
            center: center,
            radius: arcRadius,
            startAngle: .pi * 1.25,
            endAngle: .pi * 0.05,
            clockwise: true
        )
        let strokedArc = activeArc.copy(
            strokingWithWidth: arcWidth,
            lineCap: .round,
            lineJoin: .round,
            miterLimit: 10
        )
        ctx.saveGState()
        ctx.addPath(strokedArc)
        ctx.clip()
        let arcColors = [
            NSColor.systemOrange.cgColor,
            NSColor.systemRed.cgColor
        ] as CFArray
        if let arcGradient = CGGradient(colorsSpace: colorSpace, colors: arcColors, locations: [0.0, 1.0]) {
            ctx.drawLinearGradient(
                arcGradient,
                start: CGPoint(x: size * 0.2, y: size * 0.2),
                end: CGPoint(x: size * 0.8, y: size * 0.8),
                options: []
            )
        }
        ctx.restoreGState()

        // Simple thermometer symbol in center
        if let symbol = NSImage(
            systemSymbolName: "thermometer.medium",
            accessibilityDescription: "ComputerTemp"
        ) {
            let config = NSImage.SymbolConfiguration(pointSize: size * 0.26, weight: .semibold)
                .applying(.init(paletteColors: [.white, .systemOrange]))
            let configured = symbol.withSymbolConfiguration(config) ?? symbol
            let symSize = configured.size
            let drawRect = CGRect(
                x: (size - symSize.width) / 2,
                y: (size - symSize.height) / 2,
                width: symSize.width,
                height: symSize.height
            )
            configured.draw(in: drawRect)
        }

        return image
    }
}

