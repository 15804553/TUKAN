namespace BOBER.Core.Models;

public sealed class UrlopPlanValidationIssue
{
    public required string RuleId { get; init; }
    public required string Message { get; init; }
    public int? FunkcjonariuszId { get; init; }
    public DateOnly? Data { get; init; }
}
