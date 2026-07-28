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

    /// <summary>Dodatkowe kursy/uprawnienia ustawione w ustawieniach pojazdów — obowiązują wyłącznie na pozycjach 1.D i 2.K.</summary>
    public bool CzyWymagaKursow => WymaganeUprawnieniaIds.Count > 0;

    /// <summary>
    /// Gdy true — w rozkazie oceniany jest poziom gotowości nurkowej A/AB obsady pojazdu.
    /// </summary>
    public bool CzySprawdzajPoziomNurkowy { get; set; }

    /// <summary>IDs typów uprawnień z CHOMIK wymaganych na pozycjach 1.D lub 2.K (np. kurs drabin, kurs nurka).</summary>
    public List<int> WymaganeUprawnieniaIds { get; set; } = [];
}
