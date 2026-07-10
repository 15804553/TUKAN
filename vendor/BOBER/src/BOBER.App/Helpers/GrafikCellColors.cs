using System.Windows.Media;

namespace BOBER.App.Helpers;

/// <summary>Kolory komórek grafiku (D, WS) ładowane z ustawień.</summary>
public sealed class GrafikCellColors
{
    public SolidColorBrush DyzurTlo { get; init; } = new(Color.FromRgb(0x6A, 0x5C, 0x00));
    public SolidColorBrush WsTlo { get; init; } = new(Color.FromRgb(0x6A, 0x5C, 0x00));
}
