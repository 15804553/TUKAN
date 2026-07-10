using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SKRYBEK.Core.Models;
using SKRYBEK.Core.Rules;

namespace SKRYBEK.App.ViewModels;

public sealed partial class RatownikMedycznyUstawieniaViewModel : ObservableObject
{
    private readonly int _zmianaId;

    [ObservableProperty] private string _naglowekZmiany = string.Empty;
    [ObservableProperty] private RatownikMedycznyPozycjaEdycjaViewModel? _ratownik1;
    [ObservableProperty] private RatownikMedycznyPozycjaEdycjaViewModel? _ratownik2;
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private bool _isSaving;
    [ObservableProperty] private string _statusMessage = string.Empty;

    public bool CanSave => !IsLoading && !IsSaving;

    public event EventHandler? SettingsSaved;

    public RatownikMedycznyUstawieniaViewModel(int zmianaId)
    {
        _zmianaId = zmianaId;
        _naglowekZmiany = $"Zmiana {zmianaId}";
    }

    public async Task LoadAsync()
    {
        IsLoading = true;
        StatusMessage = string.Empty;
        try
        {
            var samochody = await ServiceProvider.Services.SamochodyRepo.GetAktywneAsync();
            var ustawienia = await ServiceProvider.Services.RatownikMedycznyUstawieniaRepo
                .GetDlaZmianyAsync(_zmianaId);

            Ratownik1 = UtworzWiersz(1, ustawienia, samochody);
            Ratownik2 = UtworzWiersz(2, ustawienia, samochody);
        }
        catch (Exception ex)
        {
            StatusMessage = $"Błąd wczytywania: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
            OnPropertyChanged(nameof(CanSave));
        }
    }

    [RelayCommand]
    private async Task ZapiszAsync()
    {
        if (Ratownik1 is null || Ratownik2 is null)
            return;

        IsSaving = true;
        StatusMessage = string.Empty;
        try
        {
            var ustawienia = new List<RatownikMedycznyPozycjaUstawienie>
            {
                Ratownik1.ToModel(),
                Ratownik2.ToModel()
            };

            await ServiceProvider.Services.RatownikMedycznyUstawieniaRepo
                .SaveDlaZmianyAsync(_zmianaId, ustawienia);

            StatusMessage = "Ustawienia ratowników medycznych zapisane.";
            SettingsSaved?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception ex)
        {
            StatusMessage = $"Błąd zapisu: {ex.Message}";
        }
        finally
        {
            IsSaving = false;
            OnPropertyChanged(nameof(CanSave));
        }
    }

    private static RatownikMedycznyPozycjaEdycjaViewModel UtworzWiersz(
        int ratownikPozycja,
        IReadOnlyList<RatownikMedycznyPozycjaUstawienie> ustawienia,
        IReadOnlyList<Samochod> samochody)
    {
        var wpis = ustawienia.FirstOrDefault(u => u.RatownikPozycja == ratownikPozycja)
            ?? new RatownikMedycznyPozycjaUstawienie
            {
                RatownikPozycja = ratownikPozycja,
                SamochodKolejnosc = ratownikPozycja,
                PozycjaPojazdu = RatownikMedycznyUstawieniaDomyslne.OstatniaPozycja(
                    samochody.FirstOrDefault(s => s.Kolejnosc == ratownikPozycja))
            };

        return new RatownikMedycznyPozycjaEdycjaViewModel(ratownikPozycja, wpis, samochody);
    }
}

public sealed partial class RatownikMedycznyPozycjaEdycjaViewModel : ObservableObject
{
    private readonly int _ratownikPozycja;
    private readonly IReadOnlyList<Samochod> _samochody;
    private readonly int _wpisPozycjaPojazdu;

    public string Etykieta => $"{_ratownikPozycja}. dyżurny ratownik medyczny";

    public IReadOnlyList<SamochodOpcjaViewModel> DostepneSamochody { get; }

    [ObservableProperty] private SamochodOpcjaViewModel? _wybranySamochod;

    public IReadOnlyList<PozycjaPojazduOpcjaViewModel> DostepnePozycje { get; private set; } = [];

    [ObservableProperty] private PozycjaPojazduOpcjaViewModel? _wybranaPozycja;

    public RatownikMedycznyPozycjaEdycjaViewModel(
        int ratownikPozycja,
        RatownikMedycznyPozycjaUstawienie wpis,
        IReadOnlyList<Samochod> samochody)
    {
        _ratownikPozycja = ratownikPozycja;
        _samochody = samochody;
        _wpisPozycjaPojazdu = wpis.PozycjaPojazdu;

        DostepneSamochody = samochody
            .Where(s => s.Kolejnosc is 1 or 2)
            .OrderBy(s => s.Kolejnosc)
            .Select(s => new SamochodOpcjaViewModel(s))
            .ToList();

        if (DostepneSamochody.Count == 0)
        {
            DostepneSamochody =
            [
                new SamochodOpcjaViewModel(new Samochod { Kolejnosc = 1, Nazwa = "Samochód 1", LiczbaPozycji = 6 }),
                new SamochodOpcjaViewModel(new Samochod { Kolejnosc = 2, Nazwa = "Samochód 2", LiczbaPozycji = 6 })
            ];
        }

        _wybranySamochod = DostepneSamochody.FirstOrDefault(s => s.Kolejnosc == wpis.SamochodKolejnosc)
            ?? DostepneSamochody.FirstOrDefault(s => s.Kolejnosc == ratownikPozycja)
            ?? DostepneSamochody.First();

        OdswiezPozycje(wpis.PozycjaPojazdu);
    }

    partial void OnWybranySamochodChanged(SamochodOpcjaViewModel? value)
    {
        if (value is null)
            return;

        var preferowana = WybranaPozycja?.Numer ?? _wpisPozycjaPojazdu;
        OdswiezPozycje(preferowana);
    }

    private void OdswiezPozycje(int preferowanaPozycja)
    {
        var liczba = WybranySamochod?.LiczbaPozycji ?? 6;
        var pierwszaDozwolona = PozycjaSamochoduRules.PozycjaKierowca + 1;
        var ostatniaDozwolona = Math.Max(pierwszaDozwolona, liczba);

        DostepnePozycje = pierwszaDozwolona <= ostatniaDozwolona
            ? Enumerable.Range(pierwszaDozwolona, ostatniaDozwolona - pierwszaDozwolona + 1)
                .Select(n => new PozycjaPojazduOpcjaViewModel(n))
                .ToList()
            : [];
        OnPropertyChanged(nameof(DostepnePozycje));

        var numer = PozycjaSamochoduRules.NormalizujPozycjeRatownika(preferowanaPozycja, liczba);
        WybranaPozycja = DostepnePozycje.FirstOrDefault(p => p.Numer == numer)
            ?? DostepnePozycje.LastOrDefault();
    }

    public RatownikMedycznyPozycjaUstawienie ToModel() => new()
    {
        RatownikPozycja = _ratownikPozycja,
        SamochodKolejnosc = WybranySamochod?.Kolejnosc ?? _ratownikPozycja,
        PozycjaPojazdu = WybranaPozycja?.Numer
            ?? PozycjaSamochoduRules.NormalizujPozycjeRatownika(
                _wpisPozycjaPojazdu,
                WybranySamochod?.LiczbaPozycji ?? 6)
    };
}

public sealed class PozycjaPojazduOpcjaViewModel
{
    public PozycjaPojazduOpcjaViewModel(int numer)
    {
        Numer = numer;
        Etykieta = PozycjaSamochoduRules.EtykietaPozycjiRatownika(numer);
    }

    public int Numer { get; }
    public string Etykieta { get; }
}

public sealed class SamochodOpcjaViewModel
{
    public SamochodOpcjaViewModel(Samochod samochod)
    {
        Kolejnosc = samochod.Kolejnosc;
        LiczbaPozycji = samochod.LiczbaPozycji;
        NazwaWyswietlana = $"Samochód {samochod.Kolejnosc} — {samochod.Nazwa}";
    }

    public int Kolejnosc { get; }
    public int LiczbaPozycji { get; }
    public string NazwaWyswietlana { get; }
}
