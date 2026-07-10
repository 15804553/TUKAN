using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using SKRYBEK.App.Helpers;
using SKRYBEK.Core.Enums;
using SKRYBEK.Core.Models;
using SKRYBEK.Core.Rules;
using SKRYBEK.Services.Logging;
using SKRYBEK.Services.Personnel;

namespace SKRYBEK.App.ViewModels;

public sealed partial class RozkazEditorViewModel : ObservableObject
{
    private readonly RozkazDzienny _rozkaz;
    private List<Samochod> _samochody;
    private List<Funkcjonariusz> _wszyscyZmiany;
    private readonly SessionInfo _session;
    private readonly bool _isNew;
    private readonly Dictionary<int, string> _nazwyTypowUprawnien;
    private readonly List<RatownikMedycznyPozycjaUstawienie> _ustawieniaRatownikow;

    public event EventHandler<int>? Saved;

    // ── Nagłówek ──────────────────────────────────────────────────────────────
    [ObservableProperty] private int _numerRozkazu;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DataDateTime))]
    private DateOnly _data;

    [ObservableProperty] private string _zajecia = string.Empty;
    [ObservableProperty] private string _uwagi = string.Empty;
    [ObservableProperty] private string _nrJrg = "4";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(MozeAkceptowac))]
    [NotifyPropertyChangedFor(nameof(MozeOdblokować))]
    [NotifyPropertyChangedFor(nameof(CzyZatwierdzony))]
    [NotifyPropertyChangedFor(nameof(CanPrzeladujPersonelZGrafiku))]
    private bool _isReadOnly;

    [ObservableProperty] private bool _isSaving;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanPrzeladujPersonelZGrafiku))]
    private bool _isReloadingPersonel;

    [ObservableProperty] private string _statusMessage = string.Empty;

    public bool CanPrzeladujPersonelZGrafiku => !IsReadOnly && !IsReloadingPersonel;

    public bool IsNew => _isNew;

    // ── Akceptacja rozkazu (wymaganie 3) ──────────────────────────────────────
    public bool CzyZatwierdzony => _rozkaz.Status == StatusRozkazu.Zatwierdzony;

    public bool MozeAkceptowac =>
        _session.CanEditAll && _rozkaz.Status == StatusRozkazu.Roboczy && _rozkaz.Id > 0;

    public bool MozeOdblokować =>
        _session.CanEditAll && _rozkaz.Status == StatusRozkazu.Zatwierdzony;

    public bool CzyKontoPa { get; }

    public string LoginSesji { get; } = string.Empty;

    public bool PokazPanelPersonelu => !CzyKontoPa;

    public bool PokazPrzyciskZatwierdz => MozeAkceptowac && !CzyKontoPa;

    public bool PokazPrzyciskOdblokuj => MozeOdblokować && !CzyKontoPa;

    public bool PokazPrzyciskZapisz => !CzyKontoPa;

    public string? NazwaTypuUprawnienia(int id) =>
        _nazwyTypowUprawnien.TryGetValue(id, out var nazwa) ? nazwa : null;

    // ── Wybór daty (wymaganie 5) — DatePicker binduje się przez DateTime? ─────
    public DateTime? DataDateTime
    {
        get => Data.ToDateTime(TimeOnly.MinValue);
        set
        {
            if (value.HasValue)
                Data = DateOnly.FromDateTime(value.Value);
        }
    }

    // ── Personel ──────────────────────────────────────────────────────────────
    public ObservableCollection<Funkcjonariusz> WszystkieOsoby { get; } = [];
    public ObservableCollection<Funkcjonariusz> Przefiltrowane { get; } = [];
    [ObservableProperty] private bool _filterKierowcaC;
    [ObservableProperty] private bool _filterKierowcaCE;
    [ObservableProperty] private bool _filterNurek;
    [ObservableProperty] private bool _filterKPP;
    [ObservableProperty] private int _liczbaDostepnych;
    [ObservableProperty] private string _personelInfo = string.Empty;

    // ── Sekcje ────────────────────────────────────────────────────────────────
    public ObservableCollection<PozycjaSluzbyViewModel> Sluzba { get; } = [];
    public ObservableCollection<SamochodViewModel> PodzialBojowy { get; } = [];
    public ObservableCollection<NieobecniGroupViewModel> NieobecniGrupy { get; } = [];

    public ObservableCollection<RatownikMedycznyViewModel> RatownicyMedyczni { get; } = [];

    public RozkazEditorViewModel(
        RozkazDzienny rozkaz,
        List<Samochod> samochody,
        List<Funkcjonariusz> personel,
        List<Funkcjonariusz> wszyscyZmiany,
        string nrJrg,
        SessionInfo session,
        bool isNew,
        IReadOnlyDictionary<int, string>? nazwyTypowUprawnien = null,
        IReadOnlyList<RatownikMedycznyPozycjaUstawienie>? ustawieniaRatownikow = null)
    {
        _rozkaz        = rozkaz;
        _samochody     = samochody;
        _wszyscyZmiany = wszyscyZmiany;
        _session       = session;
        _isNew         = isNew;
        _nazwyTypowUprawnien = nazwyTypowUprawnien is not null
            ? new Dictionary<int, string>(nazwyTypowUprawnien)
            : [];
        _ustawieniaRatownikow = ustawieniaRatownikow?.ToList() ?? [];

        session.NormalizePaFlags();
        CzyKontoPa = session.IsPaUser;
        LoginSesji = session.Login;

        SkrybekLog.Info($"RozkazEditorViewModel: login={LoginSesji}, CzyKontoPa={CzyKontoPa}");

        NumerRozkazu = rozkaz.NumerRozkazu;
        _data        = rozkaz.Data;
        Zajecia      = rozkaz.Zajecia;
        Uwagi        = rozkaz.Uwagi;
        NrJrg        = nrJrg;
        IsReadOnly   = session.IsReadOnly || rozkaz.Status == StatusRozkazu.Zatwierdzony;

        foreach (var osoba in personel)
        {
            WszystkieOsoby.Add(osoba);
            Przefiltrowane.Add(osoba);
        }

        LiczbaDostepnych = personel.Count;
        PersonelInfo = personel.Count == 0
            ? "Brak osób w pracy w tym dniu — sprawdź grafik BOBER."
            : $"{personel.Count} os. dostępnych na {rozkaz.Data:dd.MM.yyyy}";

        // SŁUŻBA
        foreach (var p in rozkaz.Sluzba)
            Sluzba.Add(new PozycjaSluzbyViewModel(p, personel, this));

        // PODZIAŁ BOJOWY
        foreach (var sam in samochody)
        {
            var pozycjeModelu = rozkaz.PodzialBojowy.Where(p => p.SamochodId == sam.Id).ToList();
            PodzialBojowy.Add(new SamochodViewModel(sam, pozycjeModelu, personel, this));
        }

        // RATOWNICY MEDYCZNI
        foreach (var pozycja in new[] { 1, 2 })
        {
            var model = rozkaz.RatwnicyMedyczni.FirstOrDefault(r => r.Pozycja == pozycja)
                ?? new RatownikMedyczny { Pozycja = pozycja };
            RatownicyMedyczni.Add(new RatownikMedycznyViewModel(model, personel));
        }

        // NIEOBECNI
        foreach (TypNieobecnosci typ in Enum.GetValues<TypNieobecnosci>())
        {
            var group = new NieobecniGroupViewModel(typ,
                rozkaz.Nieobecni.Where(n => n.TypNieobecnosci == typ).ToList());
            NieobecniGrupy.Add(group);
        }

        PodlaczSynchronizacjeDyzuru();
        SynchronizujDyzurZWolnaSluzba();

        if (_isNew && CzyAutoRatownicyAktywne)
            ZastosujAutoFillRatownikow();
        else
            OdswiezOznaczeniaPozycjiRatownika();
    }

    private int NrZmianyRozkazu =>
        _rozkaz.ZmianaId > 0 ? _rozkaz.ZmianaId
        : (_session.NumerZmiany > 0 ? _session.NumerZmiany : 1);

    private bool CzyAutoRatownicyAktywne =>
        !CzyKontoPa && NrZmianyRozkazu is >= 1 and <= 3 && _ustawieniaRatownikow.Count > 0;

    /// <summary>Wywoływane po zmianie obsady pojazdu — synchronizuje ratowników medycznych.</summary>
    public void OnZmianaObsadyPojazdu(int samochodKolejnosc, int pozycjaPojazdu)
    {
        if (!CzyAutoRatownicyAktywne)
            return;

        if (!ServiceProvider.Services.RatownikMedycznyAutoFill.CzyWplywaNaRatownika(
                samochodKolejnosc, pozycjaPojazdu, _ustawieniaRatownikow))
            return;

        ZastosujAutoFillRatownikow();
    }

    public void ZaktualizujUstawieniaRatownikow(IReadOnlyList<RatownikMedycznyPozycjaUstawienie> ustawienia)
    {
        _ustawieniaRatownikow.Clear();
        _ustawieniaRatownikow.AddRange(ustawienia);
        OdswiezOznaczeniaPozycjiRatownika();
        if (CzyAutoRatownicyAktywne)
            ZastosujAutoFillRatownikow();
    }

    public void ZastosujAutoFillRatownikow()
    {
        if (!CzyAutoRatownicyAktywne)
            return;

        var podzial = PodzialBojowy.SelectMany(s => s.GetModele()).ToList();
        var ratownicy = RatownicyMedyczni.Select(r => r.ToModel()).ToList();

        ServiceProvider.Services.RatownikMedycznyAutoFill.Zastosuj(
            ratownicy,
            podzial,
            _samochody,
            _ustawieniaRatownikow,
            WszystkieOsoby.ToList());

        foreach (var ratVm in RatownicyMedyczni)
        {
            var model = ratownicy.FirstOrDefault(r => r.Pozycja == ratVm.Pozycja);
            if (model is null)
                continue;

            var osoba = model.FunkcjonariuszId is int fid
                ? WszystkieOsoby.FirstOrDefault(f => f.Id == fid)
                : null;
            ratVm.UstawZOsoby(osoba, model.Nazwisko);
        }

        OdswiezOznaczeniaPozycjiRatownika();
    }

    public bool CzyPozycjaZrodlemRatownika(int samochodKolejnosc, int pozycjaPojazdu) =>
        CzyAutoRatownicyAktywne &&
        PozycjaSamochoduRules.CzyPozycjaDozwolonaDlaRatownika(pozycjaPojazdu) &&
        _ustawieniaRatownikow.Any(u =>
            u.SamochodKolejnosc == samochodKolejnosc &&
            u.PozycjaPojazdu == pozycjaPojazdu);

    public void OdswiezOznaczeniaPozycjiRatownika()
    {
        foreach (var samVm in PodzialBojowy)
            samVm.OdswiezOznaczeniaRatownika();
    }

    // ── Zmiana daty (wymaganie 5) — odświeża personel ────────────────────────
    // NumerRozkazu NIE jest aktualizowany automatycznie przy zmianie daty,
    // gdyż numer wskazuje dzień roku kiedy rozkaz był pisany (dziś), nie dzień służby.
    partial void OnDataChanged(DateOnly value)
    {
        _ = OdswiezPersonelNaDateAsync(value);
    }

    private async Task OdswiezPersonelNaDateAsync(DateOnly data) =>
        await PrzeladujDostepnyPersonelAsync(data, odswiezNieobecnych: true);

    /// <summary>
    /// Odświeża dane personelu (stopień, uprawnienia) i dostępność z grafiku BOBER
    /// po powrocie z innego widoku (edycja personelu, grafik służb).
    /// </summary>
    public async Task OdswiezPoPowrocieZInnegoWidokuAsync()
    {
        if (IsReadOnly || IsReloadingPersonel) return;

        var nrZmiany = _rozkaz.ZmianaId > 0 ? _rozkaz.ZmianaId
            : (_session.NumerZmiany > 0 ? _session.NumerZmiany : 1);

        _wszyscyZmiany = await ServiceProvider.Services.Personnel.GetWszyscyZmianaAsync(nrZmiany);
        await PrzeladujDostepnyPersonelAsync(Data, odswiezNieobecnych: true);
    }

    /// <summary>
    /// Ponownie odczytuje dostępny personel z grafiku BOBER na bieżącą datę rozkazu.
    /// Nie czyści wpisanych osób w comboboxach ani sekcji nieobecnych.
    /// </summary>
    [RelayCommand]
    private async Task PrzeladujPersonelZGrafikuAsync()
    {
        if (IsReadOnly || IsReloadingPersonel) return;

        await PrzeladujDostepnyPersonelAsync(Data, odswiezNieobecnych: false);
    }

    private async Task PrzeladujDostepnyPersonelAsync(DateOnly data, bool odswiezNieobecnych)
    {
        IsReloadingPersonel = true;
        try
        {
            var nrZmiany = _rozkaz.ZmianaId > 0 ? _rozkaz.ZmianaId
                : (_session.NumerZmiany > 0 ? _session.NumerZmiany : 1);

            var nowyPersonel = await ServiceProvider.Services.Personnel.GetDostepniAsync(data, nrZmiany);

            WszystkieOsoby.Clear();
            foreach (var osoba in nowyPersonel)
                WszystkieOsoby.Add(osoba);

            ApplyFilter();
            LiczbaDostepnych = Przefiltrowane.Count;
            PersonelInfo = nowyPersonel.Count == 0
                ? "Brak osób w pracy w tym dniu — sprawdź grafik BOBER."
                : $"{nowyPersonel.Count} os. dostępnych na {data:dd.MM.yyyy}";

            foreach (var samVm in PodzialBojowy)
                samVm.OdswiezWszystkiePozycje();

            foreach (var pozVm in Sluzba)
            {
                var tekst = pozVm.TekstOsoby;
                pozVm.OdswiezPersonel(nowyPersonel);
                var match = PersonelSuggestFilter.ZnajdzDokladnie(nowyPersonel, tekst);
                if (match is not null)
                    pozVm.TekstOsoby = match.StopienINazwisko;
            }

            foreach (var ratVm in RatownicyMedyczni)
            {
                var tekst = ratVm.TekstOsoby;
                ratVm.OdswiezPersonel(nowyPersonel);
                var match = PersonelSuggestFilter.ZnajdzDokladnie(nowyPersonel, tekst);
                if (match is not null)
                    ratVm.TekstOsoby = match.StopienINazwisko;
            }

            if (CzyAutoRatownicyAktywne)
                ZastosujAutoFillRatownikow();

            if (odswiezNieobecnych)
                await OdswiezNieobecnychZBoberaAsync(data, nrZmiany);
            else
                StatusMessage = nowyPersonel.Count == 0
                    ? $"Brak personelu na {data:dd.MM.yyyy} — sprawdź grafik BOBER."
                    : $"Odświeżono personel z grafiku — dostępnych: {nowyPersonel.Count}";
        }
        catch (Exception ex)
        {
            SkrybekLog.Error($"Błąd odświeżania personelu na {data}", ex);
            if (!odswiezNieobecnych)
                StatusMessage = $"Błąd odświeżania personelu: {ex.Message}";
        }
        finally
        {
            IsReloadingPersonel = false;
        }
    }

    private async Task OdswiezNieobecnychZBoberaAsync(DateOnly data, int nrZmiany)
    {
        try
        {
            var nieobecni = await ServiceProvider.Services.Personnel.GetNieobecniWDniuAsync(
                data, nrZmiany, _wszyscyZmiany);

            foreach (var grp in NieobecniGrupy)
            {
                var dlaTypu = nieobecni.Where(n => n.TypNieobecnosci == grp.Typ).ToList();
                grp.ZaladujZBobera(dlaTypu);
            }

            SynchronizujDyzurZWolnaSluzba();
        }
        catch (Exception ex)
        {
            SkrybekLog.Error($"Błąd przeładowania nieobecnych z BOBER na {data}", ex);
        }
    }

    // ── Akceptacja / odblokowanie (wymaganie 3) ───────────────────────────────

    [RelayCommand]
    private async Task AkceptujRozkazAsync()
    {
        if (!_session.CanEditAll || _rozkaz.Id == 0) return;

        BuildModelFromViewModels();
        await ServiceProvider.Services.Rozkaz.ZapiszAsync(
            _rozkaz,
            WszystkieOsoby.ToList());
        await ServiceProvider.Services.Rozkaz.UpdateStatusAsync(_rozkaz.Id, StatusRozkazu.Zatwierdzony);

        _rozkaz.Status = StatusRozkazu.Zatwierdzony;
        IsReadOnly = true;
        OnPropertyChanged(nameof(CzyZatwierdzony));
        OnPropertyChanged(nameof(MozeAkceptowac));
        OnPropertyChanged(nameof(MozeOdblokować));
        OnPropertyChanged(nameof(PokazPrzyciskZatwierdz));
        OnPropertyChanged(nameof(PokazPrzyciskOdblokuj));
        StatusMessage = "Rozkaz zatwierdzony — edycja zablokowana.";
    }

    [RelayCommand]
    private async Task OdblokujRozkazAsync()
    {
        if (!_session.CanEditAll) return;

        await ServiceProvider.Services.Rozkaz.UpdateStatusAsync(_rozkaz.Id, StatusRozkazu.Roboczy);

        _rozkaz.Status = StatusRozkazu.Roboczy;
        IsReadOnly = _session.IsReadOnly;
        OnPropertyChanged(nameof(CzyZatwierdzony));
        OnPropertyChanged(nameof(MozeAkceptowac));
        OnPropertyChanged(nameof(MozeOdblokować));
        OnPropertyChanged(nameof(PokazPrzyciskZatwierdz));
        OnPropertyChanged(nameof(PokazPrzyciskOdblokuj));
        StatusMessage = "Rozkaz odblokowany — można edytować.";
    }

    /// <summary>Odświeża personel, pojazdy i listy po zamknięciu okna ustawień.</summary>
    public void OdswiezPoZamknieciuUstawien(
        List<Samochod> samochody,
        List<Funkcjonariusz> personel,
        List<Funkcjonariusz> wszyscyZmiany,
        string nrJrg,
        IReadOnlyList<RatownikMedycznyPozycjaUstawienie>? ustawieniaRatownikow = null)
    {
        BuildModelFromViewModels();

        _samochody     = samochody;
        _wszyscyZmiany = wszyscyZmiany;

        WszystkieOsoby.Clear();
        foreach (var osoba in personel)
            WszystkieOsoby.Add(osoba);

        ApplyFilter();
        LiczbaDostepnych = Przefiltrowane.Count;
        PersonelInfo = personel.Count == 0
            ? "Brak osób w pracy w tym dniu — sprawdź grafik BOBER."
            : $"{personel.Count} os. dostępnych na {Data:dd.MM.yyyy}";
        NrJrg = nrJrg;

        // Zaktualizuj _personel w stanowiskach i przebuduj listy z nową datą/personelem.
        foreach (var pozycja in Sluzba)
        {
            var tekst = pozycja.TekstOsoby;
            var match = Helpers.PersonelSuggestFilter.ZnajdzDokladnie(personel, tekst);
            pozycja.OdswiezPersonel(personel);
            if (match is not null)
                pozycja.TekstOsoby = match.StopienINazwisko;
        }

        var istniejace = _rozkaz.PodzialBojowy.ToDictionary(p => (p.SamochodId, p.Pozycja));
        _rozkaz.PodzialBojowy.Clear();
        foreach (var sam in samochody)
        {
            for (int poz = 1; poz <= sam.LiczbaPozycji; poz++)
            {
                if (istniejace.TryGetValue((sam.Id, poz), out var stara))
                {
                    _rozkaz.PodzialBojowy.Add(stara);
                    continue;
                }

                _rozkaz.PodzialBojowy.Add(new PozycjaSamochodu
                {
                    SamochodId = sam.Id,
                    Pozycja    = poz
                });
            }
        }

        PodzialBojowy.Clear();
        foreach (var sam in samochody)
        {
            var pozycjeModelu = _rozkaz.PodzialBojowy.Where(p => p.SamochodId == sam.Id).ToList();
            PodzialBojowy.Add(new SamochodViewModel(sam, pozycjeModelu, personel, this));
        }

        OdswiezOznaczeniaPozycjiRatownika();

        foreach (var ratVm in RatownicyMedyczni)
        {
            var tekst = ratVm.TekstOsoby;
            var match = Helpers.PersonelSuggestFilter.ZnajdzDokladnie(personel, tekst);
            ratVm.OdswiezPersonel(personel);
            if (match is not null)
                ratVm.TekstOsoby = match.StopienINazwisko;
        }

        if (ustawieniaRatownikow is not null)
            ZaktualizujUstawieniaRatownikow(ustawieniaRatownikow);
        else if (CzyAutoRatownicyAktywne)
            ZastosujAutoFillRatownikow();
    }

    // ── Filtrowanie personelu ─────────────────────────────────────────────────

    partial void OnFilterKierowcaCChanged(bool value) => ApplyFilter();
    partial void OnFilterKierowcaCEChanged(bool value) => ApplyFilter();
    partial void OnFilterNurekChanged(bool value) => ApplyFilter();
    partial void OnFilterKPPChanged(bool value) => ApplyFilter();

    private void ApplyFilter()
    {
        var filtered = ServiceProvider.Services.Personnel.FiltrujWgKryteriow(
            WszystkieOsoby,
            FilterKierowcaC,
            FilterKierowcaCE,
            FilterNurek,
            FilterKPP);

        Przefiltrowane.Clear();
        foreach (var osoba in filtered)
            Przefiltrowane.Add(osoba);

        LiczbaDostepnych = Przefiltrowane.Count;
    }

    // ── Zapis ─────────────────────────────────────────────────────────────────

    [RelayCommand]
    private async Task ZapiszAsync()
    {
        IsSaving = true;
        StatusMessage = string.Empty;
        try
        {
            BuildModelFromViewModels();
            var id = await ServiceProvider.Services.Rozkaz.ZapiszAsync(
                _rozkaz,
                WszystkieOsoby.ToList());

            // Po pierwszym zapisie nowego rozkazu odblokuj przycisk Akceptuj
            OnPropertyChanged(nameof(MozeAkceptowac));
            OnPropertyChanged(nameof(MozeOdblokować));
            OnPropertyChanged(nameof(PokazPrzyciskZatwierdz));
            OnPropertyChanged(nameof(PokazPrzyciskOdblokuj));

            StatusMessage = $"Zapisano rozkaz Nr {_rozkaz.NumerFormatowany}";
            Saved?.Invoke(this, id);
        }
        catch (Exception ex)
        {
            var msg = ex.Message;
            SkrybekLog.Error("Błąd zapisu rozkazu", ex);
            StatusMessage = $"Błąd zapisu: {msg}";
            SkrybekMessageBox.ShowError(msg, "Błąd zapisu");
        }
        finally
        {
            IsSaving = false;
        }
    }

    [RelayCommand]
    private async Task EksportujDoWordAsync()
    {
        BuildModelFromViewModels();
        try
        {
            var outputDir = System.IO.Path.Combine(AppContext.BaseDirectory, "Eksport");
            var path = ServiceProvider.Services.WordExport.ExportRozkaz(_rozkaz, _samochody, NrJrg, outputDir);
            StatusMessage = $"Wyeksportowano: {System.IO.Path.GetFileName(path)}";
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(path) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            SkrybekMessageBox.ShowError($"Błąd eksportu:\n{ex.Message}", "Błąd eksportu");
        }
    }

    [RelayCommand]
    private void ResetujFiltr()
    {
        FilterKierowcaC  = false;
        FilterKierowcaCE = false;
        FilterNurek      = false;
        FilterKPP        = false;
        Przefiltrowane.Clear();
        foreach (var osoba in WszystkieOsoby)
            Przefiltrowane.Add(osoba);
    }

    // ── Walidacja konfliktu pojazd podstawowy ────────────────────────────────

    /// <summary>
    /// Zwraca true, gdy osoba jest już przypisana na innym miejscu pojazdu podstawowego
    /// (w tym na innym pojeździe podstawowym). Jedna osoba może siedzieć tylko na jednym
    /// samochodzie oznaczonym jako podstawowy.
    /// </summary>
    public bool CzyKonfliktPodstawowy(int funkcjonariuszId, int docelowySamochodId, int docelowaPozycja)
    {
        var docelowy = _samochody.FirstOrDefault(s => s.Id == docelowySamochodId);
        if (docelowy?.CzyPodstawowy != true) return false;

        foreach (var samVm in PodzialBojowy.Where(s => s.CzyPodstawowy))
        {
            foreach (var poz in samVm.Pozycje)
            {
                if (poz.WybranaOsoba?.Id != funkcjonariuszId) continue;
                if (samVm.Samochod.Id == docelowySamochodId && poz.Pozycja == docelowaPozycja)
                    continue;

                return true;
            }
        }

        return false;
    }

    /// <summary>Odświeża listy comboboxów na pozostałych pojazdach podstawowych (konflikt obsady).</summary>
    public void OdswiezInnePojazdyPodstawowe(int pomijanySamochodId)
    {
        foreach (var samVm in PodzialBojowy.Where(s => s.CzyPodstawowy && s.Samochod.Id != pomijanySamochodId))
            samVm.OdswiezWszystkiePozycje();
    }

    /// <summary>Odświeża listy comboboxów na wszystkich pojazdach podstawowych.</summary>
    public void OdswiezPozycjePodstawowe()
    {
        foreach (var samVm in PodzialBojowy.Where(s => s.CzyPodstawowy))
            samVm.OdswiezWszystkiePozycje();
    }

    private RozkazDzienny BuildModelFromViewModels()
    {
        _rozkaz.NumerRozkazu = NumerRozkazu;
        _rozkaz.Data         = Data;
        _rozkaz.Rok          = Data.Year;
        _rozkaz.Zajecia      = Zajecia;
        _rozkaz.Uwagi        = Uwagi;

        _rozkaz.Sluzba.Clear();
        foreach (var vm in Sluzba)
            _rozkaz.Sluzba.Add(vm.ToModel());

        _rozkaz.PodzialBojowy.Clear();
        foreach (var samVm in PodzialBojowy)
            _rozkaz.PodzialBojowy.AddRange(samVm.GetModele());

        _rozkaz.RatwnicyMedyczni.Clear();
        foreach (var ratVm in RatownicyMedyczni)
            _rozkaz.RatwnicyMedyczni.Add(ratVm.ToModel());

        _rozkaz.Nieobecni.Clear();
        SynchronizujDyzurZWolnaSluzba();
        foreach (var grp in NieobecniGrupy)
            _rozkaz.Nieobecni.AddRange(grp.GetModele());

        return _rozkaz;
    }

    private void PodlaczSynchronizacjeDyzuru()
    {
        var dyzurGrp = NieobecniGrupy.FirstOrDefault(g => g.Typ == TypNieobecnosci.DyzurDomowy);
        if (dyzurGrp is null) return;

        dyzurGrp.Items.CollectionChanged += OnDyzurItemsChanged;
        foreach (var item in dyzurGrp.Items)
            PodlaczObserwatorNazwiskaDyzuru(item);
    }

    private void OnDyzurItemsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.NewItems is not null)
        {
            foreach (NieobecnyViewModel item in e.NewItems)
                PodlaczObserwatorNazwiskaDyzuru(item);
        }

        SynchronizujDyzurZWolnaSluzba();
    }

    private void PodlaczObserwatorNazwiskaDyzuru(NieobecnyViewModel item)
    {
        item.PropertyChanged -= OnDyzurNazwiskoChanged;
        item.PropertyChanged += OnDyzurNazwiskoChanged;
    }

    private void OnDyzurNazwiskoChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(NieobecnyViewModel.Nazwisko))
            SynchronizujDyzurZWolnaSluzba();
    }

    private void SynchronizujDyzurZWolnaSluzba()
    {
        var dyzurGrp = NieobecniGrupy.FirstOrDefault(g => g.Typ == TypNieobecnosci.DyzurDomowy);
        var wolnaGrp = NieobecniGrupy.FirstOrDefault(g => g.Typ == TypNieobecnosci.CzasWolny);
        if (dyzurGrp is null || wolnaGrp is null) return;

        var wolnaPoId = wolnaGrp.Items
            .Select(i => i.ToModel())
            .Where(m => m.FunkcjonariuszId.HasValue)
            .Select(m => m.FunkcjonariuszId!.Value)
            .ToHashSet();
        var wolnaPoNazwisku = wolnaGrp.Items
            .Select(i => i.ToModel())
            .Where(m => !string.IsNullOrWhiteSpace(m.Nazwisko))
            .Select(m => m.Nazwisko.Trim())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var dyzur in dyzurGrp.Items.ToList())
        {
            var model = dyzur.ToModel();
            if (string.IsNullOrWhiteSpace(model.Nazwisko) && model.FunkcjonariuszId is null)
                continue;

            if (model.FunkcjonariuszId is int fid && wolnaPoId.Contains(fid))
                continue;

            var nazwisko = model.Nazwisko?.Trim() ?? string.Empty;
            if (model.FunkcjonariuszId is null &&
                !string.IsNullOrWhiteSpace(nazwisko) &&
                wolnaPoNazwisku.Contains(nazwisko))
                continue;

            wolnaGrp.Items.Add(new NieobecnyViewModel(new NieobecnyWSluzbie
            {
                FunkcjonariuszId = model.FunkcjonariuszId,
                Nazwisko = nazwisko,
                TypNieobecnosci = TypNieobecnosci.CzasWolny
            }));

            if (model.FunkcjonariuszId is int noweId)
                wolnaPoId.Add(noweId);
            if (!string.IsNullOrWhiteSpace(nazwisko))
                wolnaPoNazwisku.Add(nazwisko);
        }
    }
}
