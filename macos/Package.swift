// swift-tools-version: 5.9
import PackageDescription

let package = Package(
    name: "ComputerTempMac",
    platforms: [
        .macOS(.v13)
    ],
    products: [
        .executable(
            name: "ComputerTempMac",
            targets: ["ComputerTempMac"]
        )
    ],
    targets: [
        .executableTarget(
            name: "ComputerTempMac",
            path: "Sources/ComputerTempMac",
            linkerSettings: [
                .linkedFramework("IOKit"),
                .linkedFramework("UserNotifications")
            ]
        ),
        .testTarget(
            name: "ComputerTempMacTests",
            dependencies: ["ComputerTempMac"],
            path: "Tests/ComputerTempMacTests"
        )
    ]
)
