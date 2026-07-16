namespace BOBER.Core.Models;

/// <summary>Status zatwierdzenia grafiku nurkowego dla miesiąca.</summary>
public sealed class GrafikNurkowyZatwierdzenie
{
    public int Rok { get; set; }
    public int Miesiac { get; set; }
    public bool Zatwierdzony { get; set; }
    public string? ZatwierdzonyPrzez { get; set; }
    public DateTime? DataZatwierdzenia { get; set; }
}
