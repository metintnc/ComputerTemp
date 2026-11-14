import XCTest
@testable import ComputerTempMac

final class MovingAverageTests: XCTestCase {
    func testSlidingWindowComputesAverage() {
        let avg = MovingAverage(capacity: 3)
        XCTAssertEqual(avg.add(40.0), 40.0, accuracy: 0.001)
        XCTAssertEqual(avg.add(50.0), 45.0, accuracy: 0.001)
        XCTAssertEqual(avg.add(60.0), 50.0, accuracy: 0.001)
        XCTAssertEqual(avg.add(70.0), 60.0, accuracy: 0.001)
    }

    func testResetClearsWindow() {
        let avg = MovingAverage(capacity: 2)
        _ = avg.add(80.0)
        avg.reset()
        XCTAssertEqual(avg.add(30.0), 30.0, accuracy: 0.001)
    }
}
