namespace Temp;

public sealed class MonitoringCoordinator : IDisposable
{
    private readonly Func<HardwareMetrics> readMetrics;
    private readonly Action<HardwareMetrics> metricsReceived;
    private readonly Action<Exception> readFailed;
    private readonly SemaphoreSlim readingGate = new(1, 1);
    private CancellationTokenSource? cancellation;
    private Task? monitoringTask;
    private int intervalMilliseconds = 1000;
    private bool disposed;

    public MonitoringCoordinator(
        Func<HardwareMetrics> readMetrics,
        Action<HardwareMetrics> metricsReceived,
        Action<Exception> readFailed)
    {
        this.readMetrics = readMetrics;
        this.metricsReceived = metricsReceived;
        this.readFailed = readFailed;
    }

    public bool IsRunning => monitoringTask is { IsCompleted: false };

    public bool SetInterval(decimal seconds)
    {
        decimal milliseconds = seconds * 1000M;
        if (seconds <= 0 ||
            milliseconds < 1 ||
            milliseconds > int.MaxValue)
        {
            return false;
        }

        Volatile.Write(
            ref intervalMilliseconds,
            decimal.ToInt32(milliseconds));
        return true;
    }

    public void Start()
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        if (IsRunning)
        {
            return;
        }

        cancellation?.Dispose();
        cancellation = new CancellationTokenSource();
        monitoringTask = MonitorLoopAsync(cancellation.Token);
    }

    public void Stop()
    {
        cancellation?.Cancel();
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;
        cancellation?.Cancel();
        cancellation?.Dispose();
        readingGate.Dispose();
        GC.SuppressFinalize(this);
    }

    private async Task MonitorLoopAsync(CancellationToken token)
    {
        try
        {
            while (true)
            {
                await ReadOnceAsync(token).ConfigureAwait(false);
                await Task.Delay(
                    Volatile.Read(ref intervalMilliseconds),
                    token).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
        }
    }

    private async Task ReadOnceAsync(CancellationToken token)
    {
        if (!await readingGate.WaitAsync(0, token).ConfigureAwait(false))
        {
            return;
        }

        try
        {
            HardwareMetrics metrics = await Task.Run(
                readMetrics,
                token).ConfigureAwait(false);

            if (!token.IsCancellationRequested)
            {
                metricsReceived(metrics);
            }
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
        }
        catch (Exception ex) when (
            ex is ObjectDisposedException or InvalidOperationException)
        {
            if (!token.IsCancellationRequested)
            {
                readFailed(ex);
            }
        }
        finally
        {
            readingGate.Release();
        }
    }
}
