using SKRYBEK.Core.Enums;

namespace SKRYBEK.Core.Models;

public sealed class Samochod
{
    public int Id { get; set; }
    public string Nazwa { get; set; } = string.Empty;
    public int LiczbaPozycji { get; set; }
    public TypSamochodu Typ { get; set; }
    public int Kolejnosc { get; set; }
    public bool CzyAktywny { get; set; } = true;

    public bool CzyPodstawowy => Typ == TypSamochodu.Podstawowy;

    /// <summary>Dodatkowe kursy/uprawnienia ustawione przez DCA JRG — obowiązują wyłącznie na pozycjach 1.D i 2.K.</summary>
    public bool CzyWymagaKursow => WymaganeUprawnieniaIds.Count > 0;

    /// <summary>IDs typów uprawnień z CHOMIK wymaganych na pozycjach 1.D lub 2.K (np. kurs drabin, kurs nurka).</summary>
    public List<int> WymaganeUprawnieniaIds { get; set; } = [];
}
