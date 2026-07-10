namespace Chomik.Core.GeneralView;

public sealed class GeneralViewColumnPreferences
{
    public static IReadOnlyList<GeneralViewColumnId> OptionalColumns { get; } =
    [
        GeneralViewColumnId.Zmiana,
        GeneralViewColumnId.Stanowisko,
        GeneralViewColumnId.UprawnieniaAlert,
        GeneralViewColumnId.Wstepienie,
        GeneralViewColumnId.Badania,
        GeneralViewColumnId.Komora,
        GeneralViewColumnId.Kpp,
        GeneralViewColumnId.Uprawnienia,
        GeneralViewColumnId.Dodatek,
        GeneralViewColumnId.Awans,
        GeneralViewColumnId.Odznaczenia,
        GeneralViewColumnId.InneUwagi
    ];

    public static IReadOnlyList<GeneralViewColumnId> ShiftOnlyOptionalColumns { get; } =
        [GeneralViewColumnId.InneUwagi];

    public static IReadOnlyList<GeneralViewColumnId> AllKnownColumns { get; } =
        OptionalColumns.Concat(ShiftOnlyOptionalColumns).Distinct().ToList();

    public static GeneralViewColumnPreferences DefaultVisible { get; } =
        new(OptionalColumns.Where(c => c != GeneralViewColumnId.InneUwagi));

    public HashSet<GeneralViewColumnId> VisibleColumns { get; init; }

    public GeneralViewColumnPreferences(IEnumerable<GeneralViewColumnId> visibleColumns) =>
        VisibleColumns = visibleColumns.ToHashSet();

    public GeneralViewColumnPreferences() => VisibleColumns = [];

    public bool IsVisible(GeneralViewColumnId columnId) => VisibleColumns.Contains(columnId);

    public string Serialize() =>
        string.Join(',', VisibleColumns.OrderBy(c => c.ToString(), StringComparer.Ordinal));

    public static GeneralViewColumnPreferences Deserialize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return DefaultVisible;
        }

        var columns = value
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(part => Enum.TryParse<GeneralViewColumnId>(part, ignoreCase: true, out var id) ? id : (GeneralViewColumnId?)null)
            .Where(id => id.HasValue)
            .Select(id => id!.Value)
            .Where(AllKnownColumns.Contains)
            .ToHashSet();

        return columns.Count == 0 ? DefaultVisible : new GeneralViewColumnPreferences(columns);
    }
}
