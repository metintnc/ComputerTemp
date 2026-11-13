using LibreHardwareMonitor.Hardware;

namespace Temp;

/// <summary>
/// Provides access to CPU, GPU, memory, and storage sensor data.
/// </summary>
public sealed class CpuMonitor : IDisposable
{
    private readonly Computer computer;
    private bool disposed;

    public CpuMonitor()
    {
        computer = new Computer
        {
            IsCpuEnabled = true,
            IsGpuEnabled = true,
            IsMemoryEnabled = true,
            IsStorageEnabled = true
        };

        computer.Open();
    }

    /// <summary>
    /// Gets the underlying hardware-monitoring computer instance.
    /// </summary>
    public Computer Computer => computer;

    public HardwareMetrics GetMetrics()
    {
        ObjectDisposedException.ThrowIf(disposed, this);

        float? cpuTemperature = null;
        float? cpuUsage = null;
        float? gpuTemperature = null;
        float? gpuUsage = null;
        string? cpuName = null;
        string? gpuName = null;
        float? memoryUsage = null;
        float? diskRead = null;
        float? diskWrite = null;
        var coreMetrics = new Dictionary<string, CoreMetric>(StringComparer.OrdinalIgnoreCase);

        foreach (IHardware hardware in computer.Hardware)
        {
            hardware.Update();
            ReadHardware(
                hardware,
                ref cpuTemperature,
                ref cpuUsage,
                ref gpuTemperature,
                ref gpuUsage,
                ref cpuName,
                ref gpuName,
                ref memoryUsage,
                ref diskRead,
                ref diskWrite,
                coreMetrics);
        }

        return new HardwareMetrics(
            cpuName,
            cpuTemperature,
            cpuUsage,
            gpuName,
            gpuTemperature,
            gpuUsage,
            memoryUsage,
            diskRead,
            diskWrite,
            coreMetrics.Values
                .OrderBy(metric => metric.Name, StringComparer.OrdinalIgnoreCase)
                .ToArray());
    }

    /// <summary>
    /// Gets the preferred CPU package temperature, falling back to Core Average.
    /// </summary>
    /// <returns>The temperature in Celsius, or <c>null</c> when no matching sensor is available.</returns>
    public float? GetCpuTemperature()
    {
        ObjectDisposedException.ThrowIf(disposed, this);

        float? packageTemperature = null;
        float? coreAverageTemperature = null;

        foreach (IHardware hardware in computer.Hardware)
        {
            if (hardware.HardwareType != HardwareType.Cpu)
            {
                continue;
            }

            hardware.Update();
            FindPreferredTemperatureSensor(
                hardware,
                ref packageTemperature,
                ref coreAverageTemperature);
        }

        return packageTemperature ?? coreAverageTemperature;
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        computer.Close();
        disposed = true;
        GC.SuppressFinalize(this);
    }

    private static void ReadHardware(
        IHardware hardware,
        ref float? cpuTemperature,
        ref float? cpuUsage,
        ref float? gpuTemperature,
        ref float? gpuUsage,
        ref string? cpuName,
        ref string? gpuName,
        ref float? memoryUsage,
        ref float? diskRead,
        ref float? diskWrite,
        Dictionary<string, CoreMetric> coreMetrics)
    {
        if (hardware.HardwareType == HardwareType.Cpu)
        {
            cpuName ??= hardware.Name;
        }
        else if (IsGpu(hardware.HardwareType))
        {
            gpuName ??= hardware.Name;
        }

        foreach (ISensor sensor in hardware.Sensors)
        {
            if (!sensor.Value.HasValue)
            {
                continue;
            }

            float value = sensor.Value.Value;

            if (hardware.HardwareType == HardwareType.Cpu &&
                sensor.SensorType == SensorType.Temperature)
            {
                cpuTemperature = Max(cpuTemperature, value);
                AddCoreMetric(coreMetrics, sensor.Name, temperature: value);
            }
            else if (hardware.HardwareType == HardwareType.Cpu &&
                     sensor.SensorType == SensorType.Load)
            {
                if (string.Equals(
                        sensor.Name.Trim(),
                        "CPU Total",
                        StringComparison.OrdinalIgnoreCase))
                {
                    cpuUsage = sensor.Value.Value;
                }
                else if (cpuUsage is null)
                {
                    cpuUsage = sensor.Value.Value;
                }

                AddCoreMetric(coreMetrics, sensor.Name, load: value);
            }
            else if (IsGpu(hardware.HardwareType) &&
                     sensor.SensorType == SensorType.Temperature)
            {
                gpuTemperature = Max(gpuTemperature, value);
            }
            else if (IsGpu(hardware.HardwareType) &&
                     sensor.SensorType == SensorType.Load)
            {
                gpuUsage = Max(gpuUsage, value);
            }
            else if (hardware.HardwareType == HardwareType.Memory &&
                     sensor.SensorType == SensorType.Load)
            {
                memoryUsage = Max(memoryUsage, value);
            }
            else if (hardware.HardwareType == HardwareType.Storage &&
                     sensor.SensorType == SensorType.Throughput)
            {
                if (sensor.Name.Contains("read", StringComparison.OrdinalIgnoreCase))
                {
                    diskRead = Max(diskRead, value);
                }
                else if (sensor.Name.Contains("write", StringComparison.OrdinalIgnoreCase))
                {
                    diskWrite = Max(diskWrite, value);
                }
            }

        }

        foreach (IHardware subHardware in hardware.SubHardware)
        {
            subHardware.Update();
            ReadHardware(
                subHardware,
                ref cpuTemperature,
                ref cpuUsage,
                ref gpuTemperature,
                ref gpuUsage,
                ref cpuName,
                ref gpuName,
                ref memoryUsage,
                ref diskRead,
                ref diskWrite,
                coreMetrics);
        }
    }

    private static void FindPreferredTemperatureSensor(
        IHardware hardware,
        ref float? packageTemperature,
        ref float? coreAverageTemperature)
    {
        foreach (ISensor sensor in hardware.Sensors)
        {
            if (sensor.SensorType != SensorType.Temperature ||
                !sensor.Value.HasValue ||
                !float.IsFinite(sensor.Value.Value))
            {
                continue;
            }

            if (string.Equals(
                    sensor.Name.Trim(),
                    "CPU Package",
                    StringComparison.OrdinalIgnoreCase))
            {
                packageTemperature ??= sensor.Value.Value;
            }
            else if (string.Equals(
                         sensor.Name.Trim(),
                         "Core Average",
                         StringComparison.OrdinalIgnoreCase))
            {
                coreAverageTemperature ??= sensor.Value.Value;
            }
        }

        foreach (IHardware subHardware in hardware.SubHardware)
        {
            subHardware.Update();
            FindPreferredTemperatureSensor(
                subHardware,
                ref packageTemperature,
                ref coreAverageTemperature);
        }
    }

    private static bool IsGpu(HardwareType hardwareType)
    {
        return hardwareType is HardwareType.GpuNvidia
            or HardwareType.GpuAmd
            or HardwareType.GpuIntel;
    }

    private static float Max(float? current, float value)
    {
        return current is null ? value : Math.Max(current.Value, value);
    }

    private static void AddCoreMetric(
        Dictionary<string, CoreMetric> coreMetrics,
        string sensorName,
        float? temperature = null,
        float? load = null)
    {
        int digitStart = -1;
        for (int i = sensorName.Length - 1; i >= 0; i--)
        {
            if (!char.IsDigit(sensorName[i]))
            {
                continue;
            }

            digitStart = i;
            while (digitStart > 0 && char.IsDigit(sensorName[digitStart - 1]))
            {
                digitStart--;
            }

            break;
        }

        if (digitStart < 0 ||
            !int.TryParse(sensorName[digitStart..], out int coreNumber))
        {
            return;
        }

        string name = $"Core {coreNumber}";
        coreMetrics.TryGetValue(name, out CoreMetric? current);
        coreMetrics[name] = new CoreMetric(
            name,
            temperature ?? current?.Temperature,
            load ?? current?.Load);
    }
}

public sealed record HardwareMetrics(
    string? CpuName,
    float? CpuTemperature,
    float? CpuUsage,
    string? GpuName,
    float? GpuTemperature,
    float? GpuUsage,
    float? MemoryUsage,
    float? DiskRead,
    float? DiskWrite,
    IReadOnlyList<CoreMetric> Cores);

public sealed record CoreMetric(
    string Name,
    float? Temperature,
    float? Load);
