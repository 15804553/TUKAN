using System.Windows.Media;

namespace BOBER.App.Helpers;

/// <summary>Kolory komórek grafiku (D/WS oraz opcjonalne Del/S) ładowane z ustawień.</summary>
public sealed class GrafikCellColors
{
    public SolidColorBrush DyzurTlo { get; init; } = new(Color.FromRgb(0x6A, 0x5C, 0x00));
    public SolidColorBrush WsTlo { get; init; } = new(Color.FromRgb(0x6A, 0x5C, 0x00));

    /// <summary>Tło Del — null = brak własnego koloru (żółte jak WS, tylko tekst „Del”).</summary>
    public SolidColorBrush? DelTlo { get; init; }

    /// <summary>Tło S — null = brak własnego koloru (żółte jak WS, tylko tekst „S”).</summary>
    public SolidColorBrush? STlo { get; init; }
}
