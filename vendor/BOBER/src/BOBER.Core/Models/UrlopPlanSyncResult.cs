namespace BOBER.Core.Models;

public sealed class UrlopPlanSyncResult
{
    public int AppliedCount { get; init; }
    public int UpdatedCount { get; init; }
    public int SkippedManualCount { get; init; }
    public IReadOnlyList<string> SkippedManualDetails { get; init; } = [];
}
