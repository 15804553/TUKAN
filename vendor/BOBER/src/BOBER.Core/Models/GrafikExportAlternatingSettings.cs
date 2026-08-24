namespace BOBER.Core.Models;

/// <summary>
/// Kolorowanie naprzemienne wierszy w eksporcie Excel grafiku służb.
/// Domyślnie wyłączone; kolory jak w kolorowaniu naprzemiennym widoku.
/// </summary>
public sealed class GrafikExportAlternatingSettings
{
    public bool Enabled { get; init; }

    public string ColorA { get; init; } = GrafikRowColorSettings.DefaultColorA;

    public string ColorB { get; init; } = GrafikRowColorSettings.DefaultColorB;
}
