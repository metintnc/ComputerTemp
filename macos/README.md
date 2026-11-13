# ComputerTemp for macOS

A lightweight, native **Swift + SwiftUI** macOS companion to [metintnc/ComputerTemp](https://github.com/metintnc/ComputerTemp), built to monitor real-time CPU/GPU temperatures, system load, and memory usage on Apple Silicon and Intel Macs.

## Features

- **Menu Bar Monitor:** Displays live CPU temperature (`°C`) in the macOS menu bar with a quick-glance popover.
- **Detailed Dashboard:**
  - Circular gauges for **CPU Temp**, **GPU Temp**, **CPU Load (%)**, and **Memory Usage (% & GB)**.
  - Session **Min / Max** temperature tracking and macOS thermal state badge (`Nominal`, `Fair`, `Elevated`, `Critical`).
  - Expandable **Hardware Sensors** list reading individual AppleSMC thermal keys (CPU, GPU, Chassis/Palm Rest, Battery, Wi-Fi).
  - Live **Temperature History** chart powered by Swift Charts with a configurable critical threshold line.
  - **Pin (Always on Top)** toggle for floating widget-style monitoring.
- **Bilingual UI (`EN` / `TR`):** Defaults to **English** and can be switched between **English** and **Türkçe** anytime from the dashboard or menu bar.
- **Critical Temperature Alerts:** Configurable refresh interval (`0.5s – 10s`) and critical threshold (`50°C – 110°C`) with sound/notification alerts.

## Requirements

- macOS 13.0 (Ventura) or later
- Swift 5.9+ / Xcode 15+

## Running the App

### From Terminal (Swift Package Manager)
```bash
cd macos
swift run
```

### From Xcode
```bash
open macos/Package.swift
```
Select the `ComputerTempMac` scheme and press **Run (`⌘R`)**.

### Building a `.app` Bundle & Release `.zip`
```bash
./macos/scripts/build-app.sh 1.0.0
```
Outputs `macos/dist/ComputerTemp.app` and `macos/dist/ComputerTemp-macOS.zip`.

### Running Tests
```bash
cd macos
swift test
```

## Project Structure

```text
macos/
├── Package.swift
├── Sources/ComputerTempMac/
│   ├── ComputerTempMacApp.swift      # App entry point, Dashboard Window & MenuBarExtra
│   ├── Models/
│   │   ├── AppLanguage.swift         # EN / TR localization strings
│   │   └── HardwareMetrics.swift     # Metric models, thermal state, MovingAverage
│   ├── Services/
│   │   ├── SMCReader.swift           # IOKit AppleSMC temperature sensor reader
│   │   ├── SystemMetricsReader.swift # Mach/sysctl CPU load & RAM reader
│   │   └── MonitoringService.swift   # Polling coordinator, history & alerts
│   └── Views/
│       ├── DashboardView.swift       # Main dashboard, chart, sensors & controls
│       └── GaugeView.swift           # Circular progress gauge component
└── Tests/ComputerTempMacTests/
    └── MovingAverageTests.swift
```

---

<details>
<summary>Türkçe Özet / Turkish Summary</summary>

Bu klasör, orijinal Windows (`Temp.sln`) projesinin **Swift + SwiftUI** ile yazılmış sade macOS sürümünü içerir:
- **Menü Çubuğu & Detaylı Panel:** Üst çubukta anlık CPU sıcaklığı, tıklandığında açılan özet menü ve detaylı gösterge paneli.
- **Gerçek Sensör Verileri:** `IOKit` (`AppleSMC`) üzerinden CPU/GPU/kasa/batarya sıcaklıkları, `Mach/sysctl` üzerinden CPU ve RAM kullanımı.
- **Dil Desteği:** Varsayılan olarak **İngilizce** açılır, arayüz üzerinden tek tıkla **Türkçe / English** arasında geçiş yapılabilir.

</details>
