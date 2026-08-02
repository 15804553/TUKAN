namespace BOBER.Core.Models;

/// <summary>
/// Tryb kolorowania wierszy grafiku służb w UI (nie dotyczy eksportu Excel).
/// </summary>
public enum GrafikRowColorMode
{
    /// <summary>Kolory według ról/stanowisk (domyślne).</summary>
    Role = 0,

    /// <summary>Naprzemienne dwa kolory dla wszystkich wierszy funkcjonariuszy.</summary>
    Alternating = 1
}
