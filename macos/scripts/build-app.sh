#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
MACOS_DIR="$(cd "$SCRIPT_DIR/.." && pwd)"
DIST_DIR="$MACOS_DIR/dist"
APP_NAME="ComputerTemp"
APP_BUNDLE="$DIST_DIR/$APP_NAME.app"
VERSION="${1:-1.0.0}"

echo "Building $APP_NAME ($VERSION)..."
cd "$MACOS_DIR"
swift build -c release

BIN_PATH="$(swift build -c release --show-bin-path)/ComputerTempMac"
if [[ ! -f "$BIN_PATH" ]]; then
  echo "Error: Binary not found at $BIN_PATH" >&2
  exit 1
fi

rm -rf "$APP_BUNDLE"
mkdir -p "$APP_BUNDLE/Contents/MacOS" "$APP_BUNDLE/Contents/Resources"

cp "$BIN_PATH" "$APP_BUNDLE/Contents/MacOS/$APP_NAME"
chmod +x "$APP_BUNDLE/Contents/MacOS/$APP_NAME"

cat > "$APP_BUNDLE/Contents/Info.plist" <<EOF
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
    <key>CFBundleDevelopmentRegion</key>
    <string>en</string>
    <key>CFBundleExecutable</key>
    <string>$APP_NAME</string>
    <key>CFBundleIconFile</key>
    <string>AppIcon</string>
    <key>CFBundleIdentifier</key>
    <string>com.computertemp.macos</string>
    <key>CFBundleInfoDictionaryVersion</key>
    <string>6.0</string>
    <key>CFBundleName</key>
    <string>$APP_NAME</string>
    <key>CFBundlePackageType</key>
    <string>APPL</string>
    <key>CFBundleShortVersionString</key>
    <string>$VERSION</string>
    <key>CFBundleVersion</key>
    <string>$VERSION</string>
    <key>LSMinimumSystemVersion</key>
    <string>13.0</string>
    <key>NSHighResolutionCapable</key>
    <true/>
</dict>
</plist>
EOF

# Generate AppIcon.icns from AppIconProvider-style drawing
ICONSET_DIR="$DIST_DIR/AppIcon.iconset"
rm -rf "$ICONSET_DIR"
mkdir -p "$ICONSET_DIR"

swift - "$ICONSET_DIR" <<'SWIFT'
import AppKit
import Foundation

let iconsetPath = CommandLine.arguments[1]

func drawIcon(size: CGFloat) -> NSImage {
    let image = NSImage(size: NSSize(width: size, height: size))
    image.lockFocus()
    defer { image.unlockFocus() }
    guard let ctx = NSGraphicsContext.current?.cgContext else { return image }

    let rect = CGRect(x: 0, y: 0, width: size, height: size)
    let squircleRect = rect.insetBy(dx: size * 0.06, dy: size * 0.06)
    let bgPath = CGPath(roundedRect: squircleRect, cornerWidth: size * 0.22, cornerHeight: size * 0.22, transform: nil)

    ctx.saveGState()
    ctx.addPath(bgPath)
    ctx.clip()
    let cs = CGColorSpaceCreateDeviceRGB()
    let bgColors = [
        NSColor(calibratedRed: 0.14, green: 0.16, blue: 0.22, alpha: 1.0).cgColor,
        NSColor(calibratedRed: 0.08, green: 0.09, blue: 0.13, alpha: 1.0).cgColor
    ] as CFArray
    if let grad = CGGradient(colorsSpace: cs, colors: bgColors, locations: [0, 1]) {
        ctx.drawLinearGradient(grad, start: CGPoint(x: 0, y: size), end: CGPoint(x: size, y: 0), options: [])
    }
    ctx.restoreGState()

    ctx.setStrokeColor(NSColor.white.withAlphaComponent(0.14).cgColor)
    ctx.setLineWidth(size * 0.012)
    ctx.addPath(bgPath)
    ctx.strokePath()

    let center = CGPoint(x: size / 2, y: size / 2)
    let radius = size * 0.30
    let arcWidth = size * 0.045
    ctx.setLineWidth(arcWidth)
    ctx.setLineCap(.round)
    ctx.setStrokeColor(NSColor.white.withAlphaComponent(0.10).cgColor)
    ctx.addArc(center: center, radius: radius, startAngle: .pi * 1.25, endAngle: -.pi * 0.25, clockwise: true)
    ctx.strokePath()

    let activeArc = CGMutablePath()
    activeArc.addArc(center: center, radius: radius, startAngle: .pi * 1.25, endAngle: .pi * 0.05, clockwise: true)
    let stroked = activeArc.copy(strokingWithWidth: arcWidth, lineCap: .round, lineJoin: .round, miterLimit: 10)
    ctx.saveGState()
    ctx.addPath(stroked)
    ctx.clip()
    let arcColors = [NSColor.systemOrange.cgColor, NSColor.systemRed.cgColor] as CFArray
    if let arcGrad = CGGradient(colorsSpace: cs, colors: arcColors, locations: [0, 1]) {
        ctx.drawLinearGradient(arcGrad, start: CGPoint(x: size * 0.2, y: size * 0.2), end: CGPoint(x: size * 0.8, y: size * 0.8), options: [])
    }
    ctx.restoreGState()

    if let sym = NSImage(systemSymbolName: "thermometer.medium", accessibilityDescription: nil) {
        let cfg = NSImage.SymbolConfiguration(pointSize: size * 0.26, weight: .semibold)
            .applying(.init(paletteColors: [.white, .systemOrange]))
        let configured = sym.withSymbolConfiguration(cfg) ?? sym
        let s = configured.size
        configured.draw(in: CGRect(x: (size - s.width) / 2, y: (size - s.height) / 2, width: s.width, height: s.height))
    }
    return image
}

let sizes: [(Int, String)] = [
    (16, "icon_16x16.png"),
    (32, "icon_16x16@2x.png"),
    (32, "icon_32x32.png"),
    (64, "icon_32x32@2x.png"),
    (128, "icon_128x128.png"),
    (256, "icon_128x128@2x.png"),
    (256, "icon_256x256.png"),
    (512, "icon_256x256@2x.png"),
    (512, "icon_512x512.png"),
    (1024, "icon_512x512@2x.png")
]

for (px, filename) in sizes {
    let img = drawIcon(size: CGFloat(px))
    if let tiff = img.tiffRepresentation,
       let rep = NSBitmapImageRep(data: tiff),
       let png = rep.representation(using: .png, properties: [:]) {
        try png.write(to: URL(fileURLWithPath: "\(iconsetPath)/\(filename)"))
    }
}
SWIFT

iconutil -c icns "$ICONSET_DIR" -o "$APP_BUNDLE/Contents/Resources/AppIcon.icns"
rm -rf "$ICONSET_DIR"

# Ad-hoc sign so macOS allows running locally
codesign --force --deep --sign - "$APP_BUNDLE"

ZIP_PATH="$DIST_DIR/$APP_NAME-macOS.zip"
rm -f "$ZIP_PATH"
ditto -c -k --sequesterRsrc --keepParent "$APP_BUNDLE" "$ZIP_PATH"

echo "Done!"
echo "  App bundle: $APP_BUNDLE"
echo "  Release zip: $ZIP_PATH"

