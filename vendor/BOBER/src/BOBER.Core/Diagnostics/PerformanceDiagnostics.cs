using System.Diagnostics;

namespace BOBER.Core.Diagnostics;

public sealed record PerformanceMeasurement(
    string Operation,
    string Stage,
    double ElapsedMilliseconds,
    int ItemCount);

/// <summary>
/// Przekazuje pomiary do hosta tylko po jawnym włączeniu diagnostyki.
/// </summary>
public static class PerformanceDiagnostics
{
    public const string EnvironmentVariableName = "TUKAN_PERFORMANCE_DIAGNOSTICS";

    public static Action<PerformanceMeasurement>? Sink { get; set; }

    public static bool IsEnabled =>
        string.Equals(
            Environment.GetEnvironmentVariable(EnvironmentVariableName),
            "1",
            StringComparison.OrdinalIgnoreCase);

    public static Stopwatch Start() => Stopwatch.StartNew();

    public static void Log(
        string operation,
        string stage,
        Stopwatch stopwatch,
        int itemCount = 0)
    {
        stopwatch.Stop();
        if (!IsEnabled)
            return;

        Sink?.Invoke(new PerformanceMeasurement(
            operation,
            stage,
            stopwatch.Elapsed.TotalMilliseconds,
            itemCount));
    }
}
