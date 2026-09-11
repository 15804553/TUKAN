namespace BOBER.Core.Models;

public sealed class KolorStanowiska
{
    public string KluczRoli { get; set; } = string.Empty;
    public string KolorHex { get; set; } = "#2D2D2D";

    /// <summary>
    /// Czy kolor jest stosowany w grafiku miesięcznym (i powiązanych widokach).
    /// Brak rekordu w bazie traktujemy jako włączony.
    /// </summary>
    public bool Aktywny { get; set; } = true;
}
