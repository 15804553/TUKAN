using BOBER.Core.Enums;

namespace BOBER.Core.Models;

/// <summary>Pozycja słownika do kreatora zliczania (uprawnienie lub stanowisko).</summary>
public sealed class GrafikZliczanieSlownikPozycja
{
    public int Id { get; init; }
    public string Nazwa { get; init; } = string.Empty;
    public string? Podtyp { get; init; }

    public string Etykieta =>
        string.IsNullOrWhiteSpace(Podtyp) ? Nazwa : $"{Nazwa} ({Podtyp})";
}

/// <summary>Grupa OR pozycji słownika; grupy w jednym wierszu/slocie łączy AND.</summary>
public sealed class GrafikZliczanieGrupa
{
    public int Id { get; set; }
    public int? WierszId { get; set; }
    public int? SlotId { get; set; }
    public short Kolejnosc { get; set; }
    public List<int> RefIds { get; set; } = [];
}

/// <summary>Slot poziomu (np. KPP ×1, Nurkowie ×2) — osoby rozłączne, chyba że współdzielenie.</summary>
public sealed class GrafikZliczanieSlot
{
    public int Id { get; set; }
    public int PoziomId { get; set; }
    public string Nazwa { get; set; } = string.Empty;
    public GrafikZliczanieZrodlo Zrodlo { get; set; }
    public short Liczba { get; set; } = 1;
    public short Kolejnosc { get; set; }
    /// <summary>Kolejność wcześniejszego slotu, z którego wolno wziąć tę samą osobę (np. łódź = KPP).</summary>
    public short? WspoldzielSlotKolejnosc { get; set; }
    public List<GrafikZliczanieGrupa> Grupy { get; set; } = [];
}

/// <summary>Poziom (np. AB, A) sprawdzany od najniższej kolejności.</summary>
public sealed class GrafikZliczaniePoziom
{
    public int Id { get; set; }
    public int WierszId { get; set; }
    public string Kod { get; set; } = string.Empty;
    public short Kolejnosc { get; set; }
    public List<GrafikZliczanieSlot> Sloty { get; set; } = [];
}

/// <summary>Konfigurowalny wiersz podsumowania grafiku (per zmiana).</summary>
public sealed class GrafikZliczanieWiersz
{
    public int Id { get; set; }
    public int ZmianaId { get; set; }
    public string Nazwa { get; set; } = string.Empty;
    public GrafikZliczanieTyp Typ { get; set; }
    public GrafikZliczanieZrodlo Zrodlo { get; set; }
    public short Kolejnosc { get; set; }
    public List<GrafikZliczanieGrupa> Grupy { get; set; } = [];
    public List<GrafikZliczaniePoziom> Poziomy { get; set; } = [];
}
