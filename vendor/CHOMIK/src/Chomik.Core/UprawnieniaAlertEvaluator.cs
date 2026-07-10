using System.Text;
using Chomik.Core.Models;

namespace Chomik.Core;

public sealed class UprawnieniaAlertSummary
{
    public static UprawnieniaAlertSummary Empty { get; } = new();

    public bool HasAlert { get; init; }
    public DateValidityStatus? Severity { get; init; }
    public string? Tooltip { get; init; }
}

public static class UprawnieniaAlertEvaluator
{
    public static UprawnieniaAlertSummary Evaluate(IEnumerable<UprawnieniePrzypisanie> uprawnienia)
    {
        var expired = new List<UprawnieniePrzypisanie>();
        var warning = new List<UprawnieniePrzypisanie>();

        foreach (var item in uprawnienia)
        {
            var status = DateValidityEvaluator.Evaluate(item.WazneDo);
            switch (status)
            {
                case DateValidityStatus.Expired:
                    expired.Add(item);
                    break;
                case DateValidityStatus.Warning:
                    warning.Add(item);
                    break;
            }
        }

        if (expired.Count == 0 && warning.Count == 0)
        {
            return UprawnieniaAlertSummary.Empty;
        }

        var tooltip = new StringBuilder();
        if (expired.Count > 0)
        {
            tooltip.AppendLine("Po terminie:");
            foreach (var item in expired.OrderBy(u => u.WazneDo))
            {
                tooltip.Append("• ").Append(FormatLabel(item))
                    .Append(" — ").AppendLine(item.WazneDo!.Value.ToString("dd.MM.yyyy"));
            }
        }

        if (warning.Count > 0)
        {
            if (tooltip.Length > 0)
            {
                tooltip.AppendLine();
            }

            tooltip.AppendLine("Kończy się ważność (≤ 2 mies.):");
            foreach (var item in warning.OrderBy(u => u.WazneDo))
            {
                tooltip.Append("• ").Append(FormatLabel(item))
                    .Append(" — ").AppendLine(item.WazneDo!.Value.ToString("dd.MM.yyyy"));
            }
        }

        return new UprawnieniaAlertSummary
        {
            HasAlert = true,
            Severity = expired.Count > 0 ? DateValidityStatus.Expired : DateValidityStatus.Warning,
            Tooltip = tooltip.ToString().TrimEnd()
        };
    }

    private static string FormatLabel(UprawnieniePrzypisanie item) =>
        string.IsNullOrWhiteSpace(item.Podtyp) ? item.Nazwa : $"{item.Nazwa} ({item.Podtyp})";
}
