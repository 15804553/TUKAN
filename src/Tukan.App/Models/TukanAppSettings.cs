using Tukan.App.Infrastructure;

namespace Tukan.App.Models;

public sealed class TukanAppSettings
{
    public string UiColorPalette { get; set; } = TukanAppTheme.DefaultKind.ToString();

    public static TukanAppSettings CreateDefault() => new();
}
