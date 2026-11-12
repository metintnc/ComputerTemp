using System;
using System.Threading;
using Temp;
using Xunit;

namespace Temp.Tests;

public sealed class SafetyMonitorTests
{
    [Fact]
    public void TestModeDoesNotExecuteDestructiveAction()
    {
        var monitor = new SafetyMonitor(
            90,
            TimeSpan.FromMilliseconds(1),
            SafetyAction.ShutdownWindows,
            testMode: true);
        string? alert = null;

        Assert.False(monitor.Observe(95, value => alert = value));
        Thread.Sleep(10);
        Assert.True(monitor.Observe(95, value => alert = value));
        Assert.NotNull(alert);
    }

    [Fact]
    public void FallingBelowThresholdRearmsMonitor()
    {
        var monitor = new SafetyMonitor(
            90,
            TimeSpan.FromMilliseconds(1),
            SafetyAction.AlertOnly);
        int alerts = 0;

        monitor.Observe(95, _ => alerts++);
        Thread.Sleep(10);
        Assert.True(monitor.Observe(95, _ => alerts++));
        Assert.False(monitor.Observe(80, _ => alerts++));
        Thread.Sleep(10);
        Assert.False(monitor.Observe(95, _ => alerts++));
        Thread.Sleep(10);
        Assert.True(monitor.Observe(95, _ => alerts++));
        Assert.Equal(2, alerts);
    }
}
