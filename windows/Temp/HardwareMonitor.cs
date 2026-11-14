namespace Temp;

/// <summary>
/// Reads CPU and GPU metrics from LibreHardwareMonitor.
/// </summary>
public sealed class HardwareMonitor : IDisposable
{
    private readonly CpuMonitor monitor = new();
    private bool disposed;

    public HardwareMetrics ReadMetrics()
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        return monitor.GetMetrics();
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        monitor.Dispose();
        disposed = true;
        GC.SuppressFinalize(this);
    }
}
