using Tukan.App.Infrastructure;

namespace Tukan.App.Models;

public sealed class TukanUiPaletteOption(TukanAppThemeKind kind, string displayName, string description)
{
    public TukanAppThemeKind Kind { get; } = kind;

    public string Key { get; } = kind.ToString();

    public string DisplayName { get; } = displayName;

    public string Description { get; } = description;
}
