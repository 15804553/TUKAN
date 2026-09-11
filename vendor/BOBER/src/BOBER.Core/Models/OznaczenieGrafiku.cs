using BOBER.Core.Constants;
using BOBER.Core.Enums;

namespace BOBER.Core.Models;

/// <summary>Konfigurowalne oznaczenie komórki grafiku służb.</summary>
public sealed class OznaczenieGrafiku
{
    public int Id { get; set; }

    /// <summary>Zmiana (1–3), do której należy konfiguracja oznaczenia.</summary>
    public int ZmianaId { get; set; } = 1;

    /// <summary>Symbol/tekst w komórce grafiku (i kod w DB).</summary>
    public string Kod { get; set; } = string.Empty;

    /// <summary>Opis — legenda i menu kontekstowe.</summary>
    public string Nazwa { get; set; } = string.Empty;

    /// <summary>Kolor tła komórki w UI — hex lub <see cref="RoleKeys.BrakWypelnienia"/>.</summary>
    public string KolorHex { get; set; } = RoleKeys.BrakWypelnienia;

    public bool WPracy { get; set; }
    public SekcjaRozkazuGrafiku? SekcjaRozkazu { get; set; }

    /// <summary>Skrót: litera/cyfra (np. D) albo znak (/ , .). Stare wpisy mogą mieć Oem2/OemQuestion.</summary>
    public string SkrotKlawiszowy { get; set; } = string.Empty;

    /// <summary>Nadpisanie tekstu (np. UWS → „U”); null = użyj <see cref="Kod"/>.</summary>
    public string? TekstWyswietlany { get; set; }

    public bool MoznaOddac { get; set; }
    public bool MoznaKropke { get; set; }
    public bool ZachowajTloWsPrzyBraku { get; set; }
    public SekcjaRozkazuGrafiku? DodatkowaSekcjaRozkazu { get; set; }
    public RolaNalozaniaOznaczenia RolaNalozania { get; set; }
    public short Kolejnosc { get; set; }

    /// <summary>Czy wpis trafia do eksportu Excel.</summary>
    public bool EksportDoExcela { get; set; } = true;

    /// <summary>Kolor tła w Excelu; pusty / None = jak <see cref="KolorHex"/>.</summary>
    public string KolorExcelHex { get; set; } = string.Empty;

    /// <summary>
    /// Dopisek do nazwiska w wybranej grupie rozkazu (np. „-odb”).
    /// Wstawiany dosłownie po stopniu i nazwisku.
    /// </summary>
    public string AdnotacjaRozkazu { get; set; } = string.Empty;

    /// <summary>Styl symbolu w komórce (pogrubienie, przekreślenie itd.).</summary>
    public StylWyswietlaniaOznaczenia StylWyswietlania { get; set; } = StylWyswietlaniaOznaczenia.Normalny;

    /// <summary>Pozycja znaczka: zwykłe / lewa / prawa / centrum (Oddaje).</summary>
    public FlagaPozycjaOznaczenia FlagaPozycja { get; set; } = FlagaPozycjaOznaczenia.Nie;

    /// <summary>Flaga LEWA/PRAWA/CENTRUM — <see cref="KolorHex"/> to kolor czcionki, nie tła.</summary>
    public bool JestFlaga => FlagaPozycja != FlagaPozycjaOznaczenia.Nie;

    /// <summary>Domyślny kolor czcionki znaczka-flagi.</summary>
    public const string DomyslnyKolorCzcionkiFlagi = "#000000";

    public bool MaWlasnyKolor => !RoleKeys.IsBrakWypelnienia(KolorHex);

    /// <summary>Kolor czcionki znaczka (flagi); pusty → czarny.</summary>
    public string EffectiveKolorCzcionkiHex =>
        string.IsNullOrWhiteSpace(KolorHex) || RoleKeys.IsBrakWypelnienia(KolorHex)
            ? DomyslnyKolorCzcionkiFlagi
            : KolorHex.Trim();

    public string EffectiveKolorExcelHex
    {
        get
        {
            if (!string.IsNullOrWhiteSpace(KolorExcelHex) && !RoleKeys.IsBrakWypelnienia(KolorExcelHex))
                return KolorExcelHex.Trim();
            if (RoleKeys.IsBrakWypelnienia(KolorExcelHex) && !string.IsNullOrWhiteSpace(KolorExcelHex))
                return RoleKeys.BrakWypelnienia;
            return KolorHex;
        }
    }

    public bool MaKolorExcel => !RoleKeys.IsBrakWypelnienia(EffectiveKolorExcelHex);
}
