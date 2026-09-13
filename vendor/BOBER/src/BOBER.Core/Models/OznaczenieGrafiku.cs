using BOBER.Core.Constants;
using BOBER.Core.Enums;

namespace BOBER.Core.Models;

/// <summary>Konfigurowalne oznaczenie komórki grafiku służb (katalog z ustawień).</summary>
public sealed class OznaczenieGrafiku
{
    public int Id { get; set; }
    public int ZmianaId { get; set; } = 1;
    public string Kod { get; set; } = string.Empty;
    public string Nazwa { get; set; } = string.Empty;
    public string KolorHex { get; set; } = RoleKeys.BrakWypelnienia;
    public bool WPracy { get; set; }
    public SekcjaRozkazuGrafiku? SekcjaRozkazu { get; set; }
    public string SkrotKlawiszowy { get; set; } = string.Empty;
    public string? TekstWyswietlany { get; set; }
    public short Kolejnosc { get; set; }
    public bool EksportDoExcela { get; set; } = true;
    public string KolorExcelHex { get; set; } = string.Empty;
    public string AdnotacjaRozkazu { get; set; } = string.Empty;
    public StylWyswietlaniaOznaczenia StylWyswietlania { get; set; } = StylWyswietlaniaOznaczenia.Normalny;
    public FlagaPozycjaOznaczenia FlagaPozycja { get; set; } = FlagaPozycjaOznaczenia.Nie;

    public bool JestFlaga => FlagaPozycja != FlagaPozycjaOznaczenia.Nie;

    /// <summary>Domyślny kolor czcionki flagi (nie tła).</summary>
    public const string DomyslnyKolorCzcionkiFlagi = "#000000";

    public bool MaWlasnyKolor => !RoleKeys.IsBrakWypelnienia(KolorHex);

    /// <summary>Kolor czcionki flagi; pusty → czarny.</summary>
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
