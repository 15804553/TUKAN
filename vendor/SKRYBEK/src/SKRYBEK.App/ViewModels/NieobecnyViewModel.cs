using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using SKRYBEK.App.Helpers;
using SKRYBEK.Core.Enums;
using SKRYBEK.Core.Models;

namespace SKRYBEK.App.ViewModels;

public sealed partial class NieobecnyViewModel : ObservableObject
{
    [ObservableProperty] private string _nazwisko = string.Empty;
    [ObservableProperty] private TypNieobecnosci _typNieobecnosci;
    [ObservableProperty] private Funkcjonariusz? _wybranaOsoba;

    private readonly int? _funkcjonariuszId;

    public TypNieobecnosci Typ => TypNieobecnosci;

    public NieobecnyViewModel(NieobecnyWSluzbie model, IEnumerable<Funkcjonariusz>? personel = null)
    {
        _typNieobecnosci = model.TypNieobecnosci;
        _funkcjonariuszId = model.FunkcjonariuszId;

        if (personel is not null)
        {
            _wybranaOsoba = model.FunkcjonariuszId.HasValue
                ? personel.FirstOrDefault(f => f.Id == model.FunkcjonariuszId)
                : PersonelSuggestFilter.ZnajdzDokladnie(personel, model.Nazwisko);

            if (_wybranaOsoba is not null)
            {
                _nazwisko = _wybranaOsoba.StopienINazwisko;
                return;
            }
        }

        _nazwisko = model.Nazwisko;
    }

    public NieobecnyWSluzbie ToModel()
        => new()
        {
            FunkcjonariuszId = WybranaOsoba?.Id ?? _funkcjonariuszId,
            Nazwisko         = !string.IsNullOrWhiteSpace(Nazwisko) ? Nazwisko
                               : WybranaOsoba?.StopienINazwisko ?? string.Empty,
            TypNieobecnosci  = TypNieobecnosci
        };
}

public sealed partial class NieobecniGroupViewModel : ObservableObject
{
    public TypNieobecnosci Typ { get; }
    public string Tytul { get; }
    public ObservableCollection<NieobecnyViewModel> Items { get; } = [];

    public NieobecniGroupViewModel(TypNieobecnosci typ, IEnumerable<NieobecnyWSluzbie> initial, IEnumerable<Funkcjonariusz>? personel = null)
    {
        Typ   = typ;
        Tytul = typ switch
        {
            TypNieobecnosci.Urlop       => "URLOP",
            TypNieobecnosci.CzasWolny   => "WOLNA SŁUŻBA",
            TypNieobecnosci.Chory       => "CHORZY",
            TypNieobecnosci.Delegowany  => "DELEGACJA",
            TypNieobecnosci.DyzurDomowy => "DYŻUR",
            _                           => typ.ToString()
        };
        foreach (var n in initial)
            Items.Add(new NieobecnyViewModel(n, personel));
    }

    /// <summary>
    /// Zastępuje zawartość listy danymi pobranymi z BOBER.
    /// Zachowuje wpisy dodane ręcznie przez użytkownika (bez FunkcjonariuszId z BOBER).
    /// </summary>
    public void ZaladujZBobera(IEnumerable<NieobecnyWSluzbie> nieobecni, IEnumerable<Funkcjonariusz>? personel = null)
    {
        Items.Clear();
        foreach (var n in nieobecni)
            Items.Add(new NieobecnyViewModel(n, personel));
    }

    [RelayCommand]
    private void DodajNieobecnego()
        => Items.Add(new NieobecnyViewModel(new NieobecnyWSluzbie { TypNieobecnosci = Typ }));

    [RelayCommand]
    private void UsunNieobecnego(NieobecnyViewModel? vm)
    {
        if (vm is not null)
            Items.Remove(vm);
    }

    public IEnumerable<NieobecnyWSluzbie> GetModele()
        => Items.Select(i => i.ToModel()).Where(m => !string.IsNullOrWhiteSpace(m.Nazwisko));
}
