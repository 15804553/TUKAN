using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Media;
using BOBER.Core.Constants;
using BOBER.Core.Enums;
using BOBER.Core.Models;

namespace BOBER.App.ViewModels;

public sealed class OznaczenieGrafikuViewModel : INotifyPropertyChanged
{
    private string _kod = string.Empty;
    private string _nazwa = string.Empty;
    private string _kolorHex = RoleKeys.BrakWypelnienia;
    private string _kolorExcelHex = RoleKeys.BrakWypelnienia;
    private bool _wPracy;
    private SekcjaRozkazuGrafiku? _sekcjaRozkazu;
    private string _skrotKlawiszowy = string.Empty;
    private string? _tekstWyswietlany;
    private short _kolejnosc;
    private bool _eksportDoExcela = true;
    private string _adnotacjaRozkazu = string.Empty;
    private StylWyswietlaniaOznaczenia _stylWyswietlania = StylWyswietlaniaOznaczenia.Normalny;
    private FlagaPozycjaOznaczenia _flagaPozycja = FlagaPozycjaOznaczenia.Nie;
    private bool _syncExcelFromUi = true;

    public event PropertyChangedEventHandler? PropertyChanged;

    public string Kod
    {
        get => _kod;
        set { _kod = value ?? string.Empty; OnPropertyChanged(); }
    }

    public string Nazwa
    {
        get => _nazwa;
        set { _nazwa = value ?? string.Empty; OnPropertyChanged(); }
    }

    public string KolorHex
    {
        get => _kolorHex;
        set
        {
            var next = string.IsNullOrWhiteSpace(value) ? RoleKeys.BrakWypelnienia : value.Trim();
            var prev = _kolorHex;
            _kolorHex = next;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasFill));
            OnPropertyChanged(nameof(PreviewBrush));
            if (_syncExcelFromUi
                && (string.Equals(_kolorExcelHex, prev, StringComparison.OrdinalIgnoreCase)
                    || string.IsNullOrWhiteSpace(_kolorExcelHex)
                    || RoleKeys.IsBrakWypelnienia(_kolorExcelHex) && RoleKeys.IsBrakWypelnienia(prev)))
            {
                KolorExcelHex = next;
            }
        }
    }

    public string KolorExcelHex
    {
        get => _kolorExcelHex;
        set
        {
            _kolorExcelHex = string.IsNullOrWhiteSpace(value) ? RoleKeys.BrakWypelnienia : value.Trim();
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasExcelFill));
            OnPropertyChanged(nameof(ExcelPreviewBrush));
        }
    }

    public bool HasFill => !RoleKeys.IsBrakWypelnienia(KolorHex);
    public bool HasExcelFill => !RoleKeys.IsBrakWypelnienia(KolorExcelHex);
    public Brush PreviewBrush => ToBrush(KolorHex);
    public Brush ExcelPreviewBrush => ToBrush(KolorExcelHex);

    public bool WPracy
    {
        get => _wPracy;
        set
        {
            _wPracy = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(WPracyTakNie));
            if (value)
                SekcjaRozkazu = null;
            OnPropertyChanged(nameof(SekcjaEnabled));
        }
    }

    public string WPracyTakNie
    {
        get => WPracy ? "Tak" : "Nie";
        set => WPracy = string.Equals(value, "Tak", StringComparison.OrdinalIgnoreCase);
    }

    public bool SekcjaEnabled => !WPracy;

    public SekcjaRozkazuGrafiku? SekcjaRozkazu
    {
        get => _sekcjaRozkazu;
        set
        {
            _sekcjaRozkazu = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(GrupaRozkazu));
        }
    }

    public string GrupaRozkazu
    {
        get => SekcjaRozkazu switch
        {
            SekcjaRozkazuGrafiku.Urlop => "Urlopy",
            SekcjaRozkazuGrafiku.CzasWolny => "Wolna służba",
            SekcjaRozkazuGrafiku.Chory => "Chorzy",
            SekcjaRozkazuGrafiku.Delegowany => "Delegacje",
            SekcjaRozkazuGrafiku.DyzurDomowy => "Dyżur",
            _ => "—"
        };
        set
        {
            SekcjaRozkazu = value switch
            {
                "Urlopy" => SekcjaRozkazuGrafiku.Urlop,
                "Wolna służba" => SekcjaRozkazuGrafiku.CzasWolny,
                "Chorzy" => SekcjaRozkazuGrafiku.Chory,
                "Delegacje" => SekcjaRozkazuGrafiku.Delegowany,
                "Dyżur" => SekcjaRozkazuGrafiku.DyzurDomowy,
                _ => null
            };
        }
    }

    public string SkrotKlawiszowy
    {
        get => _skrotKlawiszowy;
        set { _skrotKlawiszowy = value ?? string.Empty; OnPropertyChanged(); }
    }

    public string? TekstWyswietlany
    {
        get => _tekstWyswietlany;
        set { _tekstWyswietlany = value; OnPropertyChanged(); }
    }

    public short Kolejnosc
    {
        get => _kolejnosc;
        set { _kolejnosc = value; OnPropertyChanged(); }
    }

    public bool EksportDoExcela
    {
        get => _eksportDoExcela;
        set
        {
            _eksportDoExcela = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(EksportTakNie));
        }
    }

    public string EksportTakNie
    {
        get => EksportDoExcela ? "Tak" : "Nie";
        set => EksportDoExcela = string.Equals(value, "Tak", StringComparison.OrdinalIgnoreCase);
    }

    public string AdnotacjaRozkazu
    {
        get => _adnotacjaRozkazu;
        set { _adnotacjaRozkazu = value ?? string.Empty; OnPropertyChanged(); }
    }

    public StylWyswietlaniaOznaczenia StylWyswietlania
    {
        get => _stylWyswietlania;
        set
        {
            _stylWyswietlania = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(StylLabel));
        }
    }

    public string StylLabel
    {
        get => StylWyswietlania switch
        {
            StylWyswietlaniaOznaczenia.Pogrubienie => "Pogrubienie",
            StylWyswietlaniaOznaczenia.Przekreslenie => "Przekreślenie",
            StylWyswietlaniaOznaczenia.Kursywa => "Kursywa",
            StylWyswietlaniaOznaczenia.Podkreslenie => "Podkreślenie",
            _ => "Normalny"
        };
        set => StylWyswietlania = value switch
        {
            "Pogrubienie" => StylWyswietlaniaOznaczenia.Pogrubienie,
            "Przekreślenie" => StylWyswietlaniaOznaczenia.Przekreslenie,
            "Kursywa" => StylWyswietlaniaOznaczenia.Kursywa,
            "Podkreślenie" => StylWyswietlaniaOznaczenia.Podkreslenie,
            _ => StylWyswietlaniaOznaczenia.Normalny
        };
    }

    public FlagaPozycjaOznaczenia FlagaPozycja
    {
        get => _flagaPozycja;
        set
        {
            var poprzednia = _flagaPozycja;
            _flagaPozycja = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(FlagaLabel));
            OnPropertyChanged(nameof(JestFlaga));
            OnPropertyChanged(nameof(KolorToolTip));

            if (value != FlagaPozycjaOznaczenia.Nie && poprzednia == FlagaPozycjaOznaczenia.Nie)
            {
                KolorHex = OznaczenieGrafiku.DomyslnyKolorCzcionkiFlagi;
                KolorExcelHex = RoleKeys.BrakWypelnienia;
            }
            else if (value == FlagaPozycjaOznaczenia.Nie && poprzednia != FlagaPozycjaOznaczenia.Nie)
            {
                KolorHex = RoleKeys.BrakWypelnienia;
            }
        }
    }

    public bool JestFlaga => FlagaPozycja != FlagaPozycjaOznaczenia.Nie;

    public string KolorToolTip => JestFlaga
        ? "Kolor czcionki flagi (nie tła komórki)"
        : "Kolor tła w grafiku (Brak = nie zmienia aktualnego tła komórki)";

    public string FlagaLabel
    {
        get => FlagaPozycja switch
        {
            FlagaPozycjaOznaczenia.Lewa => "LEWA",
            FlagaPozycjaOznaczenia.Prawa => "PRAWA",
            FlagaPozycjaOznaczenia.Centrum => "CENTRUM",
            _ => "NIE"
        };
        set => FlagaPozycja = value switch
        {
            "LEWA" => FlagaPozycjaOznaczenia.Lewa,
            "PRAWA" => FlagaPozycjaOznaczenia.Prawa,
            "CENTRUM" => FlagaPozycjaOznaczenia.Centrum,
            _ => FlagaPozycjaOznaczenia.Nie
        };
    }

    public void ClearFill() =>
        KolorHex = JestFlaga
            ? OznaczenieGrafiku.DomyslnyKolorCzcionkiFlagi
            : RoleKeys.BrakWypelnienia;

    public void ClearExcelFill()
    {
        _syncExcelFromUi = false;
        KolorExcelHex = RoleKeys.BrakWypelnienia;
        _syncExcelFromUi = true;
    }

    public void SetExcelFill(string hex)
    {
        _syncExcelFromUi = false;
        KolorExcelHex = hex;
        _syncExcelFromUi = true;
    }

    public static OznaczenieGrafikuViewModel FromModel(OznaczenieGrafiku m)
    {
        var vm = new OznaczenieGrafikuViewModel { _syncExcelFromUi = false };
        vm._flagaPozycja = m.FlagaPozycja;
        vm.Kod = m.Kod;
        vm.Nazwa = m.Nazwa;
        vm.KolorHex = m.JestFlaga && RoleKeys.IsBrakWypelnienia(m.KolorHex)
            ? OznaczenieGrafiku.DomyslnyKolorCzcionkiFlagi
            : m.KolorHex;
        vm.KolorExcelHex = string.IsNullOrWhiteSpace(m.KolorExcelHex) ? m.KolorHex : m.KolorExcelHex;
        vm.WPracy = m.WPracy;
        vm.SekcjaRozkazu = m.SekcjaRozkazu;
        vm.SkrotKlawiszowy = m.SkrotKlawiszowy;
        vm.TekstWyswietlany = m.TekstWyswietlany;
        vm.Kolejnosc = m.Kolejnosc;
        vm.EksportDoExcela = m.EksportDoExcela;
        vm.AdnotacjaRozkazu = m.AdnotacjaRozkazu;
        vm.StylWyswietlania = m.StylWyswietlania;
        vm._syncExcelFromUi = true;
        return vm;
    }

    public OznaczenieGrafiku ToModel() =>
        new()
        {
            Kod = Kod.Trim(),
            Nazwa = Nazwa.Trim(),
            KolorHex = RoleKeys.IsBrakWypelnienia(KolorHex) ? RoleKeys.BrakWypelnienia : KolorHex.Trim(),
            KolorExcelHex = RoleKeys.IsBrakWypelnienia(KolorExcelHex)
                ? RoleKeys.BrakWypelnienia
                : KolorExcelHex.Trim(),
            WPracy = WPracy,
            SekcjaRozkazu = WPracy ? null : SekcjaRozkazu,
            SkrotKlawiszowy = SkrotKlawiszowy.Trim(),
            TekstWyswietlany = string.IsNullOrWhiteSpace(TekstWyswietlany) ? null : TekstWyswietlany,
            Kolejnosc = Kolejnosc,
            EksportDoExcela = EksportDoExcela,
            AdnotacjaRozkazu = AdnotacjaRozkazu.Trim(),
            StylWyswietlania = StylWyswietlania,
            FlagaPozycja = FlagaPozycja
        };

    private static Brush ToBrush(string hex)
    {
        if (RoleKeys.IsBrakWypelnienia(hex))
            return Brushes.Transparent;
        try
        {
            return new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex)!);
        }
        catch
        {
            return Brushes.Transparent;
        }
    }

    private void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
