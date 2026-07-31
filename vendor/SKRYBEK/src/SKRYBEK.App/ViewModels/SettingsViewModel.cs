using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using Microsoft.Win32;
using SKRYBEK.App.Helpers;
using SKRYBEK.Core.Enums;
using SKRYBEK.Core.Models;
using SKRYBEK.Services.Backup;
using SKRYBEK.Services.Logging;

namespace SKRYBEK.App.ViewModels;

public sealed record CzestotliwoscBackupuOpcja(string Wartosc, string Etykieta);

public sealed partial class SettingsViewModel : ObservableObject
{
    private readonly SessionInfo _session;

    /// <summary>Wyzwalane po udanym zapisie ustawień ogólnych lub pojazdów.</summary>
    public event EventHandler? SettingsSaved;

    [ObservableProperty] private string _nrJrg = "4";
    [ObservableProperty] private string _statusMessage = string.Empty;
    [ObservableProperty] private bool _isLoading;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(OpisBackupu))]
    private string _wybranaCzestotliwoscBackupu = CzestotliwoscBackupu.Domyslna;

    public IReadOnlyList<CzestotliwoscBackupuOpcja> OpcjeCzestotliwosciBackupu { get; } =
    [
        new(CzestotliwoscBackupu.Codziennie, "Codziennie"),
        new(CzestotliwoscBackupu.CoTydzien, "Co tydzień"),
        new(CzestotliwoscBackupu.CoMiesiac, "Co miesiąc")
    ];

    public string OpisBackupu =>
        $"{BackupService.OpisCzestotliwosci(WybranaCzestotliwoscBackupu)} " +
        "Pliki backupu są przechowywane w katalogu BACKUP\\ obok bazy z rozszerzeniem .bck.";

    public ObservableCollection<Samochod> Samochody { get; } = [];
    public Array TypySamochodow { get; } = Enum.GetValues<TypSamochodu>();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(WymaganiaPojazdu))]
    [NotifyPropertyChangedFor(nameof(MaWybranyPojazd))]
    [NotifyPropertyChangedFor(nameof(BrakTypowUprawnien))]
    private Samochod? _wybranysamochod;

    public ObservableCollection<TypUprawnieniaItem> WymaganiaPojazdu { get; } = [];

    public bool MaWybranyPojazd => Wybranysamochod is not null;

    public bool BrakTypowUprawnien =>
        MaWybranyPojazd && WymaganiaPojazdu.Count == 0 && !IsLoading;

    public bool CanEditAll => _session.CanEditAll;

    /// <summary>Edycja pojazdów i wymaganych kursów — DCA JRG oraz Zmiana 1–3.</summary>
    public bool CanEditPojazdy => _session.CanEditPojazdy;

    public SettingsViewModel(SessionInfo session)
    {
        _session = session;
    }


    public async Task LoadAsync()
    {
        IsLoading = true;
        try
        {
            NrJrg = await ServiceProvider.Services.UstawieniaRepo.GetAsync(UstawieniaKlucze.NrJRG, "4");
            WybranaCzestotliwoscBackupu = await ServiceProvider.Services.Backup.PobierzCzestotliwoscAsync();

            var poprzedniId = Wybranysamochod?.Id;
            Wybranysamochod = null;

            Samochody.Clear();
            foreach (var s in await ServiceProvider.Services.SamochodyRepo.GetAllAsync())
                Samochody.Add(s);

            Wybranysamochod = poprzedniId is > 0
                ? Samochody.FirstOrDefault(s => s.Id == poprzedniId.Value)
                : Samochody.FirstOrDefault();

            OnPropertyChanged(nameof(BrakTypowUprawnien));
        }
        finally
        {
            IsLoading = false;
        }
    }

    partial void OnWybranysamochodChanged(Samochod? value) => _ = ZaladujWymaganiaPojazdAsync(value);

    private async Task ZaladujWymaganiaPojazdAsync(Samochod? samochod)
    {
        WymaganiaPojazdu.Clear();
        OnPropertyChanged(nameof(BrakTypowUprawnien));
        if (samochod is null || !CanEditPojazdy) return;

        var wszystkieTypy = await ServiceProvider.Services.Personnel.GetTypyUprawnienAsync();
        foreach (var (id, nazwa) in wszystkieTypy)
        {
            var item = new TypUprawnieniaItem(id, nazwa,
                czyWybrane: samochod.WymaganeUprawnieniaIds.Contains(id));
            item.PropertyChanged += (_, _) => AktualizujWymaganiaWModelu(samochod);
            WymaganiaPojazdu.Add(item);
        }

        OnPropertyChanged(nameof(BrakTypowUprawnien));
    }

    private void AktualizujWymaganiaWModelu(Samochod samochod)
    {
        samochod.WymaganeUprawnieniaIds.Clear();
        foreach (var item in WymaganiaPojazdu.Where(i => i.CzyWybrane))
            samochod.WymaganeUprawnieniaIds.Add(item.Id);
    }

    /// <summary>
    /// Przenosi zaznaczenia z panelu po prawej do pojazdu zapisywanego w bazie.
    /// Panel aktualizuje tylko aktualnie wybrany pojazd — przy zapisie wiersza trzeba to zsynchronizować.
    /// </summary>
    private void PrzeniesWymaganiaZPaneluDo(Samochod samochod)
    {
        if (Wybranysamochod is null) return;

        var tenSamPojazd = ReferenceEquals(Wybranysamochod, samochod)
            || (Wybranysamochod.Id > 0 && Wybranysamochod.Id == samochod.Id);

        if (!tenSamPojazd) return;

        AktualizujWymaganiaWModelu(Wybranysamochod);

        if (ReferenceEquals(Wybranysamochod, samochod)) return;

        samochod.WymaganeUprawnieniaIds.Clear();
        foreach (var typId in Wybranysamochod.WymaganeUprawnieniaIds)
            samochod.WymaganeUprawnieniaIds.Add(typId);
        samochod.CzySprawdzajPoziomNurkowy = Wybranysamochod.CzySprawdzajPoziomNurkowy;
    }

    [RelayCommand]
    private async Task ZapiszUstawieniaAsync()
    {
        try
        {
            await ServiceProvider.Services.UstawieniaRepo.SetAsync(UstawieniaKlucze.NrJRG, NrJrg);
            await ServiceProvider.Services.UstawieniaRepo.SetAsync(
                UstawieniaKlucze.CzestotliwoscBackupu,
                BackupService.NormalizujCzestotliwosc(WybranaCzestotliwoscBackupu));

            if (Wybranysamochod is not null)
                await ZapiszSamochodCoreAsync(Wybranysamochod, odswiezListe: true, powiadom: false);

            StatusMessage = Wybranysamochod is not null
                ? "Ustawienia zapisane (w tym wymagania wybranego pojazdu)."
                : "Ustawienia zapisane.";
            PowiadomOZapisie();
        }
        catch (Exception ex)
        {
            StatusMessage = $"Błąd: {ex.Message}";
        }
    }

    // ── Samochody ─────────────────────────────────────────────────────────────

    [RelayCommand]
    private async Task DodajSamochodAsync()
    {
        var s = new Samochod
        {
            Nazwa         = "Nowy pojazd",
            LiczbaPozycji = 4,
            Typ           = TypSamochodu.Dodatkowy,
            Kolejnosc     = Samochody.Count + 1,
            CzyAktywny    = true
        };
        await ServiceProvider.Services.SamochodyRepo.UpsertAsync(s);
        var noweId = s.Id;
        await LoadAsync();
        Wybranysamochod = Samochody.FirstOrDefault(x => x.Id == noweId)
            ?? Samochody.LastOrDefault();
        StatusMessage = "Dodano pojazd — ustaw wymagane uprawnienia po prawej i kliknij „Zapisz zmiany”.";
        PowiadomOZapisie();
    }

    [RelayCommand]
    private Task ZapiszSamochodAsync(Samochod s) => ZapiszSamochodCoreAsync(s);

    private async Task ZapiszSamochodCoreAsync(Samochod s, bool odswiezListe = true, bool powiadom = true)
    {
        try
        {
            PrzeniesWymaganiaZPaneluDo(s);
            await ServiceProvider.Services.SamochodyRepo.UpsertAsync(s);
            var id = s.Id;

            if (odswiezListe)
            {
                await LoadAsync();
                Wybranysamochod = Samochody.FirstOrDefault(x => x.Id == id) ?? s;
            }

            StatusMessage = $"Zapisano pojazd: {s.Nazwa}";
            if (powiadom)
                PowiadomOZapisie();
        }
        catch (Exception ex)
        {
            StatusMessage = $"Błąd zapisu pojazdu: {ex.Message}";
            SkrybekLog.Error($"Błąd zapisu pojazdu Id={s.Id}, Nazwa={s.Nazwa}", ex);
        }
    }

    [RelayCommand]
    private async Task UsunSamochodAsync(Samochod s)
    {
        if (!SkrybekMessageBox.Confirm(
            $"Czy usunąć pojazd '{s.Nazwa}'?",
            "TUKAN",
            SkrybekMessageKind.Warning)) return;

        try
        {
            if (s.Id <= 0)
            {
                StatusMessage = "Nie można usunąć pojazdu — brak identyfikatora w bazie. Odśwież listę i spróbuj ponownie.";
                return;
            }

            await ServiceProvider.Services.SamochodyRepo.DeleteAsync(s.Id);
            Wybranysamochod = null;
            await LoadAsync();
            StatusMessage = $"Usunięto pojazd: {s.Nazwa}";
            PowiadomOZapisie();
        }
        catch (Exception ex)
        {
            StatusMessage = $"Błąd usuwania pojazdu: {ex.Message}";
            SkrybekLog.Error($"Błąd usuwania pojazdu Id={s.Id}, Nazwa={s.Nazwa}", ex);
        }
    }

    private void PowiadomOZapisie() => SettingsSaved?.Invoke(this, EventArgs.Empty);

    // ── Backup ────────────────────────────────────────────────────────────────

    [RelayCommand]
    private async Task WykonajBackupAsync()
    {
        try
        {
            await ServiceProvider.Services.Backup.WykonajBackupAsync(WybranaCzestotliwoscBackupu);
            StatusMessage = "Backup wykonany pomyślnie.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Błąd backupu: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task OdzyskajBazeZBackupuAsync()
    {
        var katalog = ServiceProvider.Services.Backup.PobierzKatalogBackupu();
        System.IO.Directory.CreateDirectory(katalog);

        var dialog = new OpenFileDialog
        {
            Title = "Wybierz plik backupu bazy danych",
            Filter = "Backup bazy (*.bck)|*.bck|Wszystkie pliki (*.*)|*.*",
            InitialDirectory = katalog,
            CheckFileExists = true,
            Multiselect = false
        };

        if (dialog.ShowDialog() != true)
            return;

        if (!SkrybekMessageBox.Confirm(
                "Odzyskanie bazy nadpisze bieżące dane plikiem backupu.\n\n" +
                "Przed nadpisaniem zostanie utworzona kopia bezpieczeństwa bieżącej bazy.\n" +
                "Po odzyskaniu zalecany jest restart aplikacji.\n\n" +
                "Kontynuować?",
                "TUKAN",
                SkrybekMessageKind.Warning))
            return;

        try
        {
            await ServiceProvider.Services.Backup.OdzyskajZBackupuAsync(dialog.FileName);
            StatusMessage = "Baza odzyskana z backupu. Zrestartuj aplikację.";
            SkrybekMessageBox.ShowInfo(
                "Baza danych została odzyskana z backupu.\n\n" +
                "Zalecany jest restart aplikacji, aby wszystkie moduły wczytały przywrócone dane.",
                "TUKAN");
        }
        catch (Exception ex)
        {
            StatusMessage = $"Błąd odzyskiwania: {ex.Message}";
            SkrybekLog.Error($"Błąd odzyskiwania bazy z backupu: {dialog.FileName}", ex);
            SkrybekMessageBox.ShowError($"Nie udało się odzyskać bazy:\n{ex.Message}", "Błąd odzyskiwania");
        }
    }
}
