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
    private bool _synchronizacjaDat;
    private readonly DyzurWolnaSluzbaSynchronizer _dyzurSynchronizer = new();

    /// <summary>Odstęp cyklu zmian PSP: służba co 3 dni (1 praca, 2 wolne).</summary>
    private const int PrzesuniecieDniHarmonogramu = 3;

    public event EventHandler<int>? Saved;

    // ── Nagłówek ──────────────────────────────────────────────────────────────
    [ObservableProperty] private int _numerRozkazu;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DataDateTime))]
    private DateOnly _data;

    /// <summary>Data wystawienia („Kraków, dn.”) — o 3 dni wcześniej niż „Na dzień”.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DataWystawieniaDateTime))]
    [NotifyPropertyChangedFor(nameof(RokNumeru))]
    private DateOnly _dataWystawienia;

    /// <summary>Rok w numerze rozkazu (z daty „Kraków, dn.”).</summary>
    public int RokNumeru => DataWystawienia.Year;

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

    public int RozkazId => _rozkaz.Id;

    /// <summary>Kopia katalogu pojazdów aktualnie wyświetlanego w edytorze (do snapshota).</summary>
    public List<Samochod> PobierzSamochodyKatalogu() =>
        _samochody.Select(s => new Samochod
        {
            Id = s.Id,
            Nazwa = s.Nazwa,
            LiczbaPozycji = s.LiczbaPozycji,
            Typ = s.Typ,
            Kolejnosc = s.Kolejnosc,
            CzyAktywny = s.CzyAktywny,
            CzySprawdzajPoziomNurkowy = s.CzySprawdzajPoziomNurkowy,
            WymaganeUprawnieniaIds = s.WymaganeUprawnieniaIds.ToList()
        }).ToList();

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

    /// <summary>Zapisz — nie dla DCA JRG (CanEditAll) ani PA.</summary>
    public bool PokazPrzyciskZapisz => !CzyKontoPa && !_session.CanEditAll;

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

    public DateTime? DataWystawieniaDateTime
    {
        get => DataWystawienia.ToDateTime(TimeOnly.MinValue);
        set
        {
            if (value.HasValue)
                DataWystawienia = DateOnly.FromDateTime(value.Value);
        }
    }

    // ── Personel ──────────────────────────────────────────────────────────────
    public ObservableCollection<Funkcjonariusz> WszystkieOsoby { get; } = [];
    public ObservableCollection<Funkcjonariusz> Przefiltrowane { get; } = [];
    public ObservableCollection<TypUprawnieniaItem> FiltryUprawnien { get; } = [];
    [ObservableProperty] private bool _czyFiltryDropDownOtwarte;
    [ObservableProperty] private int _liczbaDostepnych;
    [ObservableProperty] private string _personelInfo = string.Empty;

    public string OpisFiltrowUprawnien
    {
        get
        {
            var wybrane = FiltryUprawnien.Where(f => f.CzyWybrane).ToList();
            if (wybrane.Count == 0)
                return "Wybierz uprawnienia / kursy…";
            if (wybrane.Count == 1)
                return wybrane[0].Nazwa;
            return $"Wybrano: {wybrane.Count}";
        }
    }

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
        _dataWystawienia = DateOnly.FromDateTime(
            rozkaz.DataUtworzenia == default ? DateTime.Now : rozkaz.DataUtworzenia);
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
        ZbudujListeFiltrowUprawnien();
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
                rozkaz.Nieobecni.Where(n => n.TypNieobecnosci == typ).ToList(),
                personel);
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

    // ── Daty: „Kraków, dn.” +3 dni = „Na dzień”; personel zawsze z „Na dzień”
    partial void OnDataChanged(DateOnly value)
    {
        if (!_synchronizacjaDat)
            UstawKrakowZNaDzien(value);

        _ = PoZmianieDatyAsync(value);
    }

    partial void OnDataWystawieniaChanged(DateOnly value)
    {
        NumerRozkazu = value.DayOfYear;
        if (!_synchronizacjaDat)
            UstawNaDzienZKrakowa(value);
    }

    private void UstawNaDzienZKrakowa(DateOnly krakow)
    {
        var naDzien = krakow.AddDays(PrzesuniecieDniHarmonogramu);
        if (Data == naDzien)
            return;

        _synchronizacjaDat = true;
        try
        {
            Data = naDzien;
        }
        finally
        {
            _synchronizacjaDat = false;
        }
    }

    private void UstawKrakowZNaDzien(DateOnly naDzien)
    {
        var krakow = naDzien.AddDays(-PrzesuniecieDniHarmonogramu);
        if (DataWystawienia == krakow)
            return;

        _synchronizacjaDat = true;
        try
        {
            DataWystawienia = krakow;
        }
        finally
        {
            _synchronizacjaDat = false;
        }
    }

    private async Task PoZmianieDatyAsync(DateOnly data)
    {
        await OdswiezPersonelNaDateAsync(data);
        await SprawdzUnikalnoscNaglowkaAsync();
        if (string.IsNullOrEmpty(StatusMessage))
            StatusMessage = PersonelInfo;
    }

    private async Task SprawdzUnikalnoscNaglowkaAsync()
    {
        try
        {
            var konflikt = await ServiceProvider.Services.Rozkaz.SprawdzUnikalnoscAsync(
                Data, NumerRozkazu, DataWystawienia.Year, _rozkaz.Id);
            StatusMessage = konflikt ?? string.Empty;
        }
        catch (Exception ex)
        {
            SkrybekLog.Error("Błąd sprawdzania unikalności rozkazu po zmianie daty", ex);
        }
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

    /// <summary>
    /// Ustawia zmianę rozkazu na tę, która pełni służbę w dniu „na dzień”
    /// (cykl 3-dniowy BOBER) i odświeża listę funkcjonariuszy tej zmiany.
    /// </summary>
    private async Task<int> UstalZmianeNaDateAsync(DateOnly data)
    {
        var zKalendarza = await ServiceProvider.Services.Personnel.GetZmianaNaDzienAsync(data);
        var nrZmiany = zKalendarza is >= 1 and <= 3
            ? zKalendarza
            : NrZmianyRozkazu;

        var zmianaSieZmienila = nrZmiany != _rozkaz.ZmianaId;
        _rozkaz.ZmianaId = nrZmiany;

        _wszyscyZmiany = await ServiceProvider.Services.Personnel.GetWszyscyZmianaAsync(nrZmiany);

        if (zmianaSieZmienila)
        {
            var ustawienia = await ServiceProvider.Services.RatownikMedycznyUstawieniaRepo
                .GetDlaZmianyAsync(nrZmiany);
            _ustawieniaRatownikow.Clear();
            _ustawieniaRatownikow.AddRange(ustawienia);
            SkrybekLog.Info($"Rozkaz na {data:yyyy-MM-dd}: służbę pełni zmiana {nrZmiany}");
        }

        return nrZmiany;
    }

    private async Task PrzeladujDostepnyPersonelAsync(DateOnly data, bool odswiezNieobecnych)
    {
        IsReloadingPersonel = true;
        try
        {
            var nrZmiany = await UstalZmianeNaDateAsync(data);
            var nowyPersonel = await ServiceProvider.Services.Personnel.GetDostepniAsync(data, nrZmiany);

            WszystkieOsoby.Clear();
            foreach (var osoba in nowyPersonel)
                WszystkieOsoby.Add(osoba);

            ApplyFilter();
            LiczbaDostepnych = Przefiltrowane.Count;
            PersonelInfo = nowyPersonel.Count == 0
                ? $"Brak osób w pracy {data:dd.MM.yyyy} (zmiana {nrZmiany}) — sprawdź grafik BOBER."
                : $"{nowyPersonel.Count} os. dostępnych na {data:dd.MM.yyyy} (zmiana {nrZmiany})";

            UsunNiedostepnePrzypisania(nowyPersonel);

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
                    ? $"Brak personelu na {data:dd.MM.yyyy} (zmiana {nrZmiany}) — sprawdź grafik BOBER."
                    : $"Odświeżono personel z grafiku — {nowyPersonel.Count} os. na {data:dd.MM.yyyy} (zmiana {nrZmiany})";
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
                grp.ZaladujZBobera(dlaTypu, _wszyscyZmiany);
            }

            _dyzurSynchronizer.Reset();
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

        // Dodatkowo upewnij się, że snapshot MEMO trafił do bazy (ACE bywa zawodny przy UPDATE wielu pól).
        _rozkaz.SamochodySnapshotJson = SamochodySnapshot.Serializuj(_samochody);
        await ServiceProvider.Services.Rozkaz.UpdateSamochodySnapshotAsync(
            _rozkaz.Id, _rozkaz.SamochodySnapshotJson);

        _rozkaz.Status = StatusRozkazu.Zatwierdzony;
        IsReadOnly = true;
        OnPropertyChanged(nameof(CzyZatwierdzony));
        OnPropertyChanged(nameof(MozeAkceptowac));
        OnPropertyChanged(nameof(MozeOdblokować));
        OnPropertyChanged(nameof(PokazPrzyciskZatwierdz));
        OnPropertyChanged(nameof(PokazPrzyciskOdblokuj));
        StatusMessage = "Rozkaz zatwierdzony — edycja zablokowana.";
        Saved?.Invoke(this, _rozkaz.Id);
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
        Saved?.Invoke(this, _rozkaz.Id);
    }

    [RelayCommand]
    private async Task AkceptujWszystkieAsync()
    {
        if (!_session.CanEditAll) return;

        var rok = _rozkaz.Rok > 0 ? _rozkaz.Rok : RokNumeru;
        var lista = await ServiceProvider.Services.Rozkaz.GetByRokAsync(rok);
        if (lista.Count == 0)
        {
            SkrybekMessageBox.ShowInfo("Brak rozkazów do zatwierdzenia w wybranym roku.", "Zatwierdź wszystkie");
            return;
        }

        var zatwierdzic = RozkazZatwierdzanieRules.CzyZatwierdzicWszystkie(lista);
        if (zatwierdzic)
        {
            var ile = RozkazZatwierdzanieRules.FiltrujDoZatwierdzenia(lista).Count;
            if (!SkrybekMessageBox.Confirm(
                    $"Zatwierdzić wszystkie niezatwierdzone rozkazy za rok {rok}?\n\nLiczba rozkazów: {ile}",
                    "Zatwierdź wszystkie",
                    SkrybekMessageKind.Question))
                return;

            // Przed zbiorczym zatwierdzeniem zapisz bieżący roboczy rozkaz (treść + snapshot).
            if (_rozkaz.Id > 0 && _rozkaz.Status == StatusRozkazu.Roboczy)
            {
                BuildModelFromViewModels();
                await ServiceProvider.Services.Rozkaz.ZapiszAsync(_rozkaz, WszystkieOsoby.ToList());
                _rozkaz.SamochodySnapshotJson = SamochodySnapshot.Serializuj(_samochody);
                await ServiceProvider.Services.Rozkaz.UpdateSamochodySnapshotAsync(
                    _rozkaz.Id, _rozkaz.SamochodySnapshotJson);
            }
        }
        else
        {
            var ile = RozkazZatwierdzanieRules.FiltrujDoOdblokowania(lista).Count;
            if (ile == 0)
            {
                SkrybekMessageBox.ShowInfo("Brak zatwierdzonych rozkazów do odblokowania.", "Zatwierdź wszystkie");
                return;
            }

            if (!SkrybekMessageBox.Confirm(
                    $"Odblokować wszystkie zatwierdzone rozkazy za rok {rok}?\n\nLiczba rozkazów: {ile}",
                    "Odblokuj wszystkie",
                    SkrybekMessageKind.Warning))
                return;
        }

        try
        {
            var (zmienionych, zatwierdzono) =
                await ServiceProvider.Services.Rozkaz.ZatwierdzLubOdblokujWszystkieAsync(rok);

            if (zatwierdzono)
            {
                if (_rozkaz.Id > 0)
                {
                    _rozkaz.Status = StatusRozkazu.Zatwierdzony;
                    IsReadOnly = true;
                }

                StatusMessage = zmienionych == 0
                    ? "Brak rozkazów do zatwierdzenia."
                    : $"Zatwierdzono {zmienionych} rozkazów — edycja zablokowana.";
            }
            else
            {
                if (_rozkaz.Id > 0)
                {
                    _rozkaz.Status = StatusRozkazu.Roboczy;
                    IsReadOnly = _session.IsReadOnly;
                }

                StatusMessage = zmienionych == 0
                    ? "Brak rozkazów do odblokowania."
                    : $"Odblokowano {zmienionych} rozkazów — można edytować.";
            }

            OnPropertyChanged(nameof(CzyZatwierdzony));
            OnPropertyChanged(nameof(MozeAkceptowac));
            OnPropertyChanged(nameof(MozeOdblokować));
            OnPropertyChanged(nameof(PokazPrzyciskZatwierdz));
            OnPropertyChanged(nameof(PokazPrzyciskOdblokuj));
            Saved?.Invoke(this, _rozkaz.Id);
        }
        catch (Exception ex)
        {
            SkrybekLog.Error("Błąd zbiorczego zatwierdzania/odblokowywania rozkazów", ex);
            StatusMessage = $"Błąd: {ex.Message}";
            SkrybekMessageBox.ShowError(ex.Message, "Błąd zatwierdzania");
        }
    }

    /// <summary>Odświeża personel, pojazdy i listy po zamknięciu okna ustawień.</summary>
    public void OdswiezPoZamknieciuUstawien(
        List<Samochod> samochody,
        List<Funkcjonariusz> personel,
        List<Funkcjonariusz> wszyscyZmiany,
        string nrJrg,
        IReadOnlyList<RatownikMedycznyPozycjaUstawienie>? ustawieniaRatownikow = null)
    {
        // Zablokowany meldunek zachowuje pojazdy i obsadę z chwili zatwierdzenia.
        if (CzyZatwierdzony)
            return;

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

    private void UsunNiedostepnePrzypisania(List<Funkcjonariusz> dostepnyPersonel)
    {
        foreach (var pozycja in Sluzba)
            pozycja.WyczyscJesliOsobaJuzNiedostepna(dostepnyPersonel);

        foreach (var samVm in PodzialBojowy)
        {
            foreach (var pozycja in samVm.Pozycje)
                pozycja.WyczyscJesliOsobaJuzNiedostepna(dostepnyPersonel);
        }

        foreach (var ratVm in RatownicyMedyczni)
            ratVm.WyczyscJesliOsobaJuzNiedostepna(dostepnyPersonel);
    }

    // ── Filtrowanie personelu ─────────────────────────────────────────────────

    private bool _pominOdswiezanieFiltra;

    private void ZbudujListeFiltrowUprawnien()
    {
        FiltryUprawnien.Clear();
        foreach (var (id, nazwa) in _nazwyTypowUprawnien
                     .OrderBy(kv => kv.Value, StringComparer.CurrentCultureIgnoreCase))
        {
            var item = new TypUprawnieniaItem(id, nazwa);
            item.PropertyChanged += OnFiltrUprawnieniaChanged;
            FiltryUprawnien.Add(item);
        }
    }

    private void OnFiltrUprawnieniaChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (_pominOdswiezanieFiltra)
            return;

        if (e.PropertyName == nameof(TypUprawnieniaItem.CzyWybrane))
        {
            OnPropertyChanged(nameof(OpisFiltrowUprawnien));
            ApplyFilter();
        }
    }

    private void ApplyFilter()
    {
        var wymaganeIds = FiltryUprawnien
            .Where(f => f.CzyWybrane)
            .Select(f => f.Id)
            .ToList();

        var filtered = ServiceProvider.Services.Personnel.FiltrujWgUprawnieniami(
            WszystkieOsoby, wymaganeIds);

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

            // Osobny zapis snapshota — pewniejszy na ACE niż wyłącznie kolumna w UPDATE nagłówka.
            await ServiceProvider.Services.Rozkaz.UpdateSamochodySnapshotAsync(
                id, _rozkaz.SamochodySnapshotJson);

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
            var configured = await ServiceProvider.Services.UstawieniaRepo.GetAsync("ExportPathRozkazy");
            var outputDir = !string.IsNullOrWhiteSpace(configured)
                ? configured.Trim()
                : System.IO.Path.Combine(AppContext.BaseDirectory, "Eksport");
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
        _pominOdswiezanieFiltra = true;
        try
        {
            foreach (var filtr in FiltryUprawnien)
                filtr.CzyWybrane = false;
        }
        finally
        {
            _pominOdswiezanieFiltra = false;
        }

        OnPropertyChanged(nameof(OpisFiltrowUprawnien));
        ApplyFilter();
    }

    // ── Walidacja konfliktu pojazd podstawowy ────────────────────────────────

    /// <summary>
    /// Zwraca true, gdy osoba jest już przypisana na innym miejscu pojazdu podstawowego
    /// — zarówno na innym siedzeniu tego samego pojazdu, jak i na innym pojeździe podstawowym.
    /// </summary>
    public bool CzyKonfliktPodstawowy(int funkcjonariuszId, int docelowySamochodId, int docelowaPozycja)
    {
        var obsada = PodzialBojowy
            .SelectMany(samVm => samVm.Pozycje.Select(poz =>
            {
                var model = poz.ToModel();
                return new PozycjaSamochodu
                {
                    SamochodId = samVm.Samochod.Id,
                    Pozycja = poz.Pozycja,
                    FunkcjonariuszId = poz.WybranaOsoba?.Id ?? model.FunkcjonariuszId,
                    Nazwisko = model.Nazwisko
                };
            }));

        return PodzialBojowyRules.CzyKonfliktPodstawowy(
            obsada, _samochody, funkcjonariuszId, docelowySamochodId, docelowaPozycja);
    }

    /// <summary>
    /// Zwraca true, gdy osoba jest wpisana w rozkazie jako dyżurny PA JRG.
    /// Taka osoba nie może być obsadzana na pojeździe podstawowym (dodatkowy — tak).
    /// </summary>
    public bool CzyOsobaJestPa(int funkcjonariuszId) =>
        Sluzba.Any(s => s.Stanowisko == StanowiskoSluzby.DyzurnyPAJRG
                     && s.WybranaOsoba?.Id == funkcjonariuszId);

    /// <summary>
    /// True, gdy osoba jest dowódcą zmiany albo dyżurnym PA na innym stanowisku służby
    /// (z wyjątkiem dozwolonej pary PA + Dowódca działań SGRW-N).
    /// </summary>
    public bool CzyOsobaZajetaNaStanowiskuWylaczonym(int funkcjonariuszId, StanowiskoSluzby pominStanowisko)
        => PobierzStanowiskoWylaczajaceOsoby(funkcjonariuszId, pominStanowisko) is not null;

    public PozycjaSluzbyViewModel? PobierzStanowiskoWylaczajaceOsoby(
        int funkcjonariuszId, StanowiskoSluzby pominStanowisko)
        => Sluzba.FirstOrDefault(s =>
            s.Stanowisko != pominStanowisko
            && StanowiskoSluzbyRules.CzyStanowiskoWylaczaInneWSluzbie(s.Stanowisko)
            && s.WybranaOsoba?.Id == funkcjonariuszId
            && !StanowiskoSluzbyRules.CzyDozwolonyWyjatekWylacznosci(s.Stanowisko, pominStanowisko));

    /// <summary>
    /// Zdejmuje osobę z pozostałych stanowisk służby (np. po wpisaniu na DZ lub PA).
    /// Zachowuje wyjątek: PA + Dowódca działań SGRW-N mogą współistnieć.
    /// </summary>
    public void WyczyscOsobeZInnychStanowiskSluzby(int funkcjonariuszId, StanowiskoSluzby pominStanowisko)
    {
        foreach (var poz in Sluzba)
        {
            if (poz.Stanowisko == pominStanowisko)
                continue;
            if (StanowiskoSluzbyRules.CzyZachowacPrzyCzyszczeniuWylacznosci(pominStanowisko, poz.Stanowisko))
                continue;
            if (poz.WybranaOsoba?.Id == funkcjonariuszId)
                poz.WyczyscOsobe();
        }
    }

    /// <summary>Odświeża listy comboboxów stanowisk służby (poza wskazanym).</summary>
    public void OdswiezStanowiskaSluzby(StanowiskoSluzby? pominStanowisko = null)
    {
        foreach (var poz in Sluzba)
        {
            if (pominStanowisko is { } pomin && poz.Stanowisko == pomin)
                continue;
            poz.OdswiezDostepneOsoby();
        }
    }

    /// <summary>Usuwa osobę z obsady wszystkich pojazdów podstawowych (np. po wpisaniu na PA).</summary>
    public void WyczyscOsobeZPojazdowPodstawowych(int funkcjonariuszId)
    {
        foreach (var samVm in PodzialBojowy.Where(s => s.CzyPodstawowy))
        {
            foreach (var poz in samVm.Pozycje)
            {
                if (poz.WybranaOsoba?.Id == funkcjonariuszId)
                    poz.WybranaOsoba = null;
            }
        }
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
        _rozkaz.Rok          = DataWystawienia.Year;
        _rozkaz.DataUtworzenia = DataWystawienia.ToDateTime(
            TimeOnly.FromDateTime(_rozkaz.DataUtworzenia == default ? DateTime.Now : _rozkaz.DataUtworzenia));
        _rozkaz.Zajecia      = Zajecia;
        _rozkaz.Uwagi        = Uwagi;
        _rozkaz.SamochodySnapshotJson = SamochodySnapshot.Serializuj(_samochody);

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
        SynchronizujDyzurZWolnaSluzba(wymusNoweWpisy: true);
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

    private void SynchronizujDyzurZWolnaSluzba(bool wymusNoweWpisy = false)
    {
        var dyzurGrp = NieobecniGrupy.FirstOrDefault(g => g.Typ == TypNieobecnosci.DyzurDomowy);
        var wolnaGrp = NieobecniGrupy.FirstOrDefault(g => g.Typ == TypNieobecnosci.CzasWolny);
        if (dyzurGrp is null || wolnaGrp is null) return;

        _dyzurSynchronizer.Synchronizuj(dyzurGrp.Items, wolnaGrp.Items, _wszyscyZmiany, wymusNoweWpisy);
    }
}
