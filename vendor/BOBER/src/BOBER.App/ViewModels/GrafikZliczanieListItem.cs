using System.ComponentModel;
using System.Runtime.CompilerServices;
using BOBER.Core.Enums;
using BOBER.Core.Models;
using BOBER.Core.Rules;

namespace BOBER.App.ViewModels;

public sealed class GrafikZliczanieListItem : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    public bool IsFixed { get; init; }
    public GrafikZliczanieWiersz? Wiersz { get; set; }

    private string _nazwa = string.Empty;
    public string Nazwa
    {
        get => _nazwa;
        set { _nazwa = value; OnPropertyChanged(); }
    }

    private string _typTekst = string.Empty;
    public string TypTekst
    {
        get => _typTekst;
        set { _typTekst = value; OnPropertyChanged(); }
    }

    private string _skrot = string.Empty;
    public string Skrot
    {
        get => _skrot;
        set { _skrot = value; OnPropertyChanged(); }
    }

    public static GrafikZliczanieListItem WolneMiejsca() =>
        new()
        {
            IsFixed = true,
            Nazwa = GrafikZliczanieEvaluator.WolneMiejscaNazwa,
            TypTekst = "Stałe",
            Skrot = "Stan zmiany − minimum − nieobecni"
        };

    public static GrafikZliczanieListItem ZWiersza(
        GrafikZliczanieWiersz wiersz,
        IReadOnlyDictionary<int, string> etykietyUprawnien,
        IReadOnlyDictionary<int, string> etykietyStanowisk)
    {
        return new GrafikZliczanieListItem
        {
            Wiersz = wiersz,
            Nazwa = wiersz.Nazwa,
            TypTekst = wiersz.Typ == GrafikZliczanieTyp.Poziom ? "Poziom" : "Zliczanie",
            Skrot = FormatSkrot(wiersz, etykietyUprawnien, etykietyStanowisk)
        };
    }

    public static string FormatSkrot(
        GrafikZliczanieWiersz wiersz,
        IReadOnlyDictionary<int, string> etykietyUprawnien,
        IReadOnlyDictionary<int, string> etykietyStanowisk)
    {
        if (wiersz.Typ == GrafikZliczanieTyp.Poziom)
        {
            var kody = string.Join(" / ", wiersz.Poziomy.OrderBy(p => p.Kolejnosc).Select(p => p.Kod));
            return string.IsNullOrWhiteSpace(kody) ? "Poziom (brak kodów)" : kody;
        }

        var etykiety = wiersz.Zrodlo == GrafikZliczanieZrodlo.Stanowiska
            ? etykietyStanowisk
            : etykietyUprawnien;
        var grupy = wiersz.Grupy
            .OrderBy(g => g.Kolejnosc)
            .Select(g => string.Join(" LUB ", g.RefIds.Select(id => etykiety.GetValueOrDefault(id, $"#{id}"))));
        var joined = string.Join(" ORAZ ", grupy);
        return string.IsNullOrWhiteSpace(joined) ? "Brak kryteriów" : joined;
    }

    private void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
