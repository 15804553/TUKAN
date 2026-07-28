using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using SKRYBEK.App.Helpers;
using SKRYBEK.Core.Models;
using SKRYBEK.Core.Rules;

namespace SKRYBEK.App.ViewModels;

public sealed class PozycjaSamochoduViewModel : ObservableObject
{
    private readonly PozycjaSamochodu _model;
    private readonly SamochodViewModel _samochod;
    private readonly RozkazEditorViewModel _editor;
    private bool _czyPozycjaRatownika;
    private Funkcjonariusz? _wybranaOsoba;
    private string _tekstOsoby;
    private bool _tekstUstawianyProgramowo;

    public int Pozycja => _model.Pozycja;
    public string NumerPozycji => $"{Pozycja}.";
    public string OznaczeniePozycji =>
        PozycjaSamochoduRules.OznaczenieWyswietlane(Pozycja, _czyPozycjaRatownika);
    public bool MaOznaczenie => !string.IsNullOrEmpty(OznaczeniePozycji);

    public Brush OznaczenieBrush
    {
        get
        {
            if (Pozycja == PozycjaSamochoduRules.PozycjaDowodca)
                return (Brush)Application.Current.FindResource("SamochodOznaczenieDBrush");
            if (Pozycja == PozycjaSamochoduRules.PozycjaKierowca)
                return (Brush)Application.Current.FindResource("SamochodOznaczenieKBrush");
            if (_czyPozycjaRatownika)
                return (Brush)Application.Current.FindResource("SamochodOznaczenieRBrush");
            return Brushes.Transparent;
        }
    }

    private readonly ObservableCollection<OsobaComboBoxItem> _dostepneOsobyRaw = [];
    private ListCollectionView? _dostepneOsobyView;
    private bool _odswiezanieListy;

    /// <summary>
    /// Lista osób dostępnych w danym dniu, przefiltrowana wyłącznie według wymagań pozycji (1.D / 2.K).
    /// </summary>
    public ListCollectionView DostepneOsoby
    {
        get
        {
            if (_dostepneOsobyView is null)
                _dostepneOsobyView = new ListCollectionView(_dostepneOsobyRaw);
            return _dostepneOsobyView;
        }
    }

    public Funkcjonariusz? WybranaOsoba
    {
        get => _wybranaOsoba;
        set => PrzypiszOsobeZListy(value, aktualizujTekst: true);
    }

    public OsobaComboBoxItem? WybranyItem
    {
        get => _wybranaOsoba is null ? null
            : _dostepneOsobyRaw.FirstOrDefault(i => i.Osoba.Id == _wybranaOsoba.Id);
        set
        {
            if (_odswiezanieListy) return;
            PrzypiszOsobeZListy(value?.Osoba, aktualizujTekst: true);
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

            // Ignoruj zmiany tekstu wysyłane przez WPF podczas Clear() w OdswiezDostepneOsoby.
            // WPF ustawia Text="" gdy SelectedItem znika z kolekcji po Clear(), co skutkowało
            // niezamierzonym wyczyszczeniem _wybranaOsoba i _model.Nazwisko.
            if (_odswiezanieListy) return;

            var text = value ?? string.Empty;
            if (!SetProperty(ref _tekstOsoby, text)) return;

            _model.Nazwisko = text;
            var match = PersonelSuggestFilter.ZnajdzDokladnie(_editor.WszystkieOsoby, text);
            if (match is not null)
                PrzypiszOsobeZListy(match, aktualizujTekst: false);
            else
                UstawBezOsobyZListy();
        }
    }

    internal PozycjaSamochoduViewModel(
        PozycjaSamochodu model,
        SamochodViewModel samochod,
        RozkazEditorViewModel editor,
        List<Funkcjonariusz> personel)
    {
        _model    = model;
        _samochod = samochod;
        _editor   = editor;
        _wybranaOsoba = model.FunkcjonariuszId.HasValue
            ? personel.FirstOrDefault(f => f.Id == model.FunkcjonariuszId)
            : PersonelSuggestFilter.ZnajdzDokladnie(personel, model.Nazwisko);

        // Jeśli osoba rozpoznana po nazwisku (brak FunkcjonariuszId w modelu) — napraw FK,
        // aby kolejny zapis zapisał poprawne powiązanie z bazą danych.
        if (_wybranaOsoba is not null && !model.FunkcjonariuszId.HasValue)
            model.FunkcjonariuszId = _wybranaOsoba.Id;

        _tekstOsoby = !string.IsNullOrWhiteSpace(model.Nazwisko)
            ? model.Nazwisko
            : _wybranaOsoba?.StopienINazwisko ?? string.Empty;

        OdswiezDostepneOsoby();
        OdswiezOznaczenieRatownika();
    }

    internal void OdswiezOznaczenieRatownika()
    {
        _czyPozycjaRatownika = _editor.CzyPozycjaZrodlemRatownika(_samochod.Samochod.Kolejnosc, Pozycja);
        OnPropertyChanged(nameof(OznaczeniePozycji));
        OnPropertyChanged(nameof(MaOznaczenie));
        OnPropertyChanged(nameof(OznaczenieBrush));
    }

    private void PrzypiszOsobeZListy(Funkcjonariusz? value, bool aktualizujTekst)
    {
        // Jeśli to ta sama osoba — tylko ewentualnie odśwież tekst i wyjdź.
        // Zapobiega pętli: set → ZarejestrujZmianeOsoby → event → rebuild → set ponownie.
        if (_wybranaOsoba?.Id == value?.Id)
        {
            if (aktualizujTekst && value is not null)
                UstawTekstProgramowo(_model.Nazwisko);
            return;
        }

        if (value is not null)
        {
            if (!PozycjaSamochoduRules.CzyOsobaDozwolonaNaPozycji(value, Pozycja))
            {
                SkrybekMessageBox.ShowWarning(
                    PozycjaSamochoduRules.OpisWymagania(Pozycja),
                    "Niedozwolone przypisanie");
                OnPropertyChanged(nameof(WybranaOsoba));
                OnPropertyChanged(nameof(WybranyItem));
                if (aktualizujTekst)
                    UstawTekstProgramowo(_model.Nazwisko);
                return;
            }

            if (_editor.CzyKonfliktPodstawowy(value.Id, _samochod.Samochod.Id, Pozycja))
            {
                SkrybekMessageBox.ShowWarning(
                    $"{value.StopienINazwisko} jest już przypisany/a do innego pojazdu podstawowego.\n" +
                    "Ta sama osoba nie może siedzieć na dwóch pojazdach podstawowych.",
                    "Konflikt pojazdów podstawowych");
                OnPropertyChanged(nameof(WybranaOsoba));
                OnPropertyChanged(nameof(WybranyItem));
                if (aktualizujTekst)
                    UstawTekstProgramowo(_model.Nazwisko);
                return;
            }
        }

        SetProperty(ref _wybranaOsoba, value);
        OnPropertyChanged(nameof(WybranaOsoba));
        OnPropertyChanged(nameof(WybranyItem));
        _model.FunkcjonariuszId = value?.Id;
        _model.Nazwisko = value is not null
            ? $"{value.Stopien} {value.Nazwisko}"
            : _tekstOsoby;

        if (aktualizujTekst)
            UstawTekstProgramowo(_model.Nazwisko);

        OdswiezPozycjePoZmianieObsady();
    }

    private void UstawBezOsobyZListy()
    {
        if (_wybranaOsoba is null) return;
        SetProperty(ref _wybranaOsoba, null);
        OnPropertyChanged(nameof(WybranaOsoba));
        OnPropertyChanged(nameof(WybranyItem));
        _model.FunkcjonariuszId = null;

        OdswiezPozycjePoZmianieObsady();
    }

    private void OdswiezPozycjePoZmianieObsady()
    {
        _samochod.OdswiezWszystkiePozycje();

        if (_samochod.CzyPodstawowy)
            _editor.OdswiezInnePojazdyPodstawowe(_samochod.Samochod.Id);

        _editor.OnZmianaObsadyPojazdu(_samochod.Samochod.Kolejnosc, Pozycja);
    }

    private void UstawTekstProgramowo(string tekst)
    {
        _tekstUstawianyProgramowo = true;
        // Jeśli wartość nie zmieniła się, SetProperty nie wyśle PropertyChanged — wymuszamy je
        // ręcznie, żeby WPF zaktualizował ComboBox.Text (który mógł być wyzerowany przez Clear()).
        // Flaga _tekstUstawianyProgramowo chroni re-entrantne TekstOsoby.set przed logiką wyszukiwania.
        if (!SetProperty(ref _tekstOsoby, tekst, nameof(TekstOsoby)))
            OnPropertyChanged(nameof(TekstOsoby));
        _tekstUstawianyProgramowo = false;
    }

    /// <summary>
    /// Odświeża listę dostępnych osób — tylko osoby spełniające wymagania pozycji (1.D / 2.K).
    /// Na pojazdach podstawowych ukrywa osoby już przypisane do innego pojazdu podstawowego.
    /// </summary>
    public void OdswiezDostepneOsoby()
    {
        _odswiezanieListy = true;
        try
        {
            _dostepneOsobyRaw.Clear();

            foreach (var osoba in _editor.WszystkieOsoby)
            {
                if (!PozycjaSamochoduRules.CzyOsobaDozwolonaNaPozycji(osoba, Pozycja))
                    continue;

                if (_samochod.CzyPodstawowy &&
                    _editor.CzyKonfliktPodstawowy(osoba.Id, _samochod.Samochod.Id, Pozycja))
                    continue;

                _dostepneOsobyRaw.Add(new OsobaComboBoxItem(osoba, czySugerowana: true));
            }

            if (_wybranaOsoba is not null && _dostepneOsobyRaw.All(i => i.Osoba.Id != _wybranaOsoba.Id))
                _dostepneOsobyRaw.Insert(0, new OsobaComboBoxItem(_wybranaOsoba, czySugerowana: false));
        }
        finally
        {
            _odswiezanieListy = false;
        }

        OnPropertyChanged(nameof(WybranyItem));
        // Przywróć tekst jeśli WPF wyczyścił go podczas Clear().
        // UstawTekstProgramowo wymusza PropertyChanged z flagą ochronną — re-entrantne
        // TekstOsoby.set nie wykona wyszukiwania w liście personelu, więc zmiana daty
        // (nowy personel) nie skasuje wcześniej wybranej osoby.
        if (_wybranaOsoba is not null)
            UstawTekstProgramowo(_model.Nazwisko);
    }

    public void WyczyscJesliOsobaJuzNiedostepna(IReadOnlyCollection<Funkcjonariusz> dostepnyPersonel)
    {
        if (_wybranaOsoba is null)
            return;

        if (dostepnyPersonel.Any(f => f.Id == _wybranaOsoba.Id))
            return;

        SetProperty(ref _wybranaOsoba, null);
        OnPropertyChanged(nameof(WybranaOsoba));
        OnPropertyChanged(nameof(WybranyItem));
        _model.FunkcjonariuszId = null;
        _model.Nazwisko = string.Empty;
        UstawTekstProgramowo(string.Empty);
        OdswiezPozycjePoZmianieObsady();
    }

    public PozycjaSamochodu ToModel() => _model;
}

public sealed class SamochodViewModel : ObservableObject
{
    private readonly RozkazEditorViewModel _editor;

    public Samochod Samochod { get; }
    public ObservableCollection<PozycjaSamochoduViewModel> Pozycje { get; } = [];
    public string Nazwa => Samochod.Nazwa;
    public bool CzyPodstawowy => Samochod.CzyPodstawowy;
    public bool CzyPokazujIkoneWymagan => Samochod.LiczbaPozycji >= PozycjaSamochoduRules.PozycjaDowodca;
    public bool CzyWymaganiaSpelnione { get; private set; }
    public string WymaganeKursyTooltip { get; private set; } = string.Empty;
    public string PoziomNurkowyTekst { get; private set; } = string.Empty;
    public bool CzyPokazujPoziomNurkowy => Samochod.CzySprawdzajPoziomNurkowy;

    public Brush WymaganeKursyKolor => CzyWymaganiaSpelnione
        ? (Brush)Application.Current.FindResource("OkBrush")
        : (Brush)Application.Current.FindResource("AlertBrush");

    public SamochodViewModel(
        Samochod samochod,
        IEnumerable<PozycjaSamochodu> modele,
        List<Funkcjonariusz> personel,
        RozkazEditorViewModel editor)
    {
        Samochod = samochod;
        _editor = editor;
        foreach (var m in modele.OrderBy(m => m.Pozycja))
            Pozycje.Add(new PozycjaSamochoduViewModel(m, this, editor, personel));
        OdswiezOznaczeniaRatownika();
        OdswiezStatusWymagan();
    }

    public void OdswiezOznaczeniaRatownika()
    {
        foreach (var pozycja in Pozycje)
            pozycja.OdswiezOznaczenieRatownika();
    }

    public void OdswiezWszystkiePozycje()
    {
        foreach (var p in Pozycje)
            p.OdswiezDostepneOsoby();
        OdswiezStatusWymagan();
    }

    public void OdswiezStatusWymagan()
    {
        var pozycje = Pozycje.Select(p => (p.Pozycja, p.WybranaOsoba)).ToList();
        CzyWymaganiaSpelnione = PozycjaSamochoduRules.CzySpelniaWymaganiaPojazdu(pozycje, Samochod);
        WymaganeKursyTooltip = PozycjaSamochoduRules.BudujTooltipWymaganPojazdu(
            Samochod, pozycje, _editor.NazwaTypuUprawnienia);

        if (Samochod.CzySprawdzajPoziomNurkowy)
        {
            var poziom = PozycjaSamochoduRules.OcenaPoziomuNurkowego(pozycje);
            PoziomNurkowyTekst = poziom == PoziomGotowosciNurkowej.Brak
                ? "—"
                : PoziomGotowosciNurkowejRules.Format(poziom);
        }
        else
        {
            PoziomNurkowyTekst = string.Empty;
        }

        OnPropertyChanged(nameof(CzyPokazujIkoneWymagan));
        OnPropertyChanged(nameof(CzyWymaganiaSpelnione));
        OnPropertyChanged(nameof(WymaganeKursyTooltip));
        OnPropertyChanged(nameof(WymaganeKursyKolor));
        OnPropertyChanged(nameof(PoziomNurkowyTekst));
        OnPropertyChanged(nameof(CzyPokazujPoziomNurkowy));
    }

    public IEnumerable<PozycjaSamochodu> GetModele()
        => Pozycje.Select(p => p.ToModel());
}
