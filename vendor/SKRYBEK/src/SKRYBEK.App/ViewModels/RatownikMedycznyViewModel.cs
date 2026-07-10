using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Data;
using SKRYBEK.App.Helpers;
using SKRYBEK.Core.Models;

namespace SKRYBEK.App.ViewModels;

public sealed partial class RatownikMedycznyViewModel : ObservableObject
{
    private readonly RatownikMedyczny _model;
    private List<Funkcjonariusz> _personel;
    private Funkcjonariusz? _wybranaOsoba;
    private string _tekstOsoby;
    private bool _tekstUstawianyProgramowo;

    public int Pozycja => _model.Pozycja;

    private readonly ObservableCollection<OsobaComboBoxItem> _dostepneOsobyRaw = [];
    private ListCollectionView? _dostepneOsobyView;
    private bool _odswiezanieListy;

    public ListCollectionView DostepneOsoby
    {
        get
        {
            if (_dostepneOsobyView is null)
            {
                _dostepneOsobyView = new ListCollectionView(_dostepneOsobyRaw);
                _dostepneOsobyView.GroupDescriptions.Add(
                    new PropertyGroupDescription(nameof(OsobaComboBoxItem.NazwaGrupy)));
            }
            return _dostepneOsobyView;
        }
    }

    public Funkcjonariusz? WybranaOsoba
    {
        get => _wybranaOsoba;
        set => UstawWybranaOsobe(value, aktualizujTekst: true);
    }

    public OsobaComboBoxItem? WybranyItem
    {
        get => _wybranaOsoba is null ? null
            : _dostepneOsobyRaw.FirstOrDefault(i => i.Osoba.Id == _wybranaOsoba.Id);
        set
        {
            if (_odswiezanieListy) return;
            UstawWybranaOsobe(value?.Osoba, aktualizujTekst: true);
        }
    }

    public string TekstOsoby
    {
        get => _tekstOsoby;
        set
        {
            if (_tekstUstawianyProgramowo)
            {
                SetProperty(ref _tekstOsoby, value ?? string.Empty);
                return;
            }

            if (_odswiezanieListy) return;

            var text = value ?? string.Empty;
            if (!SetProperty(ref _tekstOsoby, text)) return;

            _model.Nazwisko = text;
            var match = PersonelSuggestFilter.ZnajdzDokladnie(_personel, text);
            if (match is not null)
                UstawWybranaOsobe(match, aktualizujTekst: false);
            else
                UstawWybranaOsobe(null, aktualizujTekst: false);
        }
    }

    public RatownikMedycznyViewModel(RatownikMedyczny model, List<Funkcjonariusz> personel)
    {
        _model = model;
        _personel = personel;
        _wybranaOsoba = model.FunkcjonariuszId.HasValue
            ? personel.FirstOrDefault(f => f.Id == model.FunkcjonariuszId)
            : PersonelSuggestFilter.ZnajdzDokladnie(personel, model.Nazwisko);

        if (_wybranaOsoba is not null && !model.FunkcjonariuszId.HasValue)
            model.FunkcjonariuszId = _wybranaOsoba.Id;

        _tekstOsoby = !string.IsNullOrWhiteSpace(model.Nazwisko)
            ? model.Nazwisko
            : _wybranaOsoba?.StopienINazwisko ?? string.Empty;

        OdswiezDostepneOsoby();
    }

    public void OdswiezDostepneOsoby()
    {
        _odswiezanieListy = true;
        try
        {
            _dostepneOsobyRaw.Clear();

            foreach (var osoba in _personel)
                _dostepneOsobyRaw.Add(new OsobaComboBoxItem(osoba, czySugerowana: true));

            if (_wybranaOsoba is not null && _dostepneOsobyRaw.All(i => i.Osoba.Id != _wybranaOsoba.Id))
                _dostepneOsobyRaw.Insert(0, new OsobaComboBoxItem(_wybranaOsoba, czySugerowana: false));
        }
        finally
        {
            _odswiezanieListy = false;
        }

        OnPropertyChanged(nameof(WybranyItem));
        if (_wybranaOsoba is not null)
            UstawTekstProgramowo(_model.Nazwisko);
    }

    public void OdswiezPersonel(List<Funkcjonariusz> nowyPersonel)
    {
        _personel = nowyPersonel;
        OdswiezDostepneOsoby();
    }

    /// <summary>Programowe ustawienie osoby (np. auto-uzupełnienie z obsady pojazdu).</summary>
    public void UstawZOsoby(Funkcjonariusz? osoba, string? nazwisko = null)
    {
        if (osoba is not null)
        {
            UstawWybranaOsobe(osoba, aktualizujTekst: true);
            return;
        }

        if (!string.IsNullOrWhiteSpace(nazwisko))
        {
            _model.FunkcjonariuszId = null;
            _model.Nazwisko = nazwisko;
            SetProperty(ref _wybranaOsoba, null);
            OnPropertyChanged(nameof(WybranaOsoba));
            OnPropertyChanged(nameof(WybranyItem));
            UstawTekstProgramowo(nazwisko);
            return;
        }

        _model.FunkcjonariuszId = null;
        _model.Nazwisko = string.Empty;
        SetProperty(ref _wybranaOsoba, null);
        OnPropertyChanged(nameof(WybranaOsoba));
        OnPropertyChanged(nameof(WybranyItem));
        UstawTekstProgramowo(string.Empty);
    }

    private void UstawWybranaOsobe(Funkcjonariusz? osoba, bool aktualizujTekst)
    {
        if (_wybranaOsoba?.Id == osoba?.Id)
        {
            if (aktualizujTekst && osoba is not null)
                UstawTekstProgramowo(_model.Nazwisko);
            return;
        }

        SetProperty(ref _wybranaOsoba, osoba);
        OnPropertyChanged(nameof(WybranaOsoba));
        OnPropertyChanged(nameof(WybranyItem));

        _model.FunkcjonariuszId = osoba?.Id;
        _model.Nazwisko = osoba is not null
            ? $"{osoba.Stopien} {osoba.Nazwisko}"
            : _tekstOsoby;

        if (!aktualizujTekst) return;
        UstawTekstProgramowo(_model.Nazwisko);
    }

    private void UstawTekstProgramowo(string tekst)
    {
        _tekstUstawianyProgramowo = true;
        if (!SetProperty(ref _tekstOsoby, tekst, nameof(TekstOsoby)))
            OnPropertyChanged(nameof(TekstOsoby));
        _tekstUstawianyProgramowo = false;
    }

    public RatownikMedyczny ToModel() => _model;
}
