using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using SKRYBEK.App.Helpers;
using SKRYBEK.Core.Enums;
using SKRYBEK.Core.Models;
using SKRYBEK.Services.Logging;

namespace SKRYBEK.App.ViewModels;

public sealed partial class SettingsViewModel : ObservableObject
{
    private readonly SessionInfo _session;

    [ObservableProperty] private string _nrJrg = "4";
    [ObservableProperty] private string _statusMessage = string.Empty;
    [ObservableProperty] private bool _isLoading;

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

    /// <summary>Edycja pojazdów i grup — tylko DCA JRG (wymaganie 1).</summary>
    public bool CanEditPojazdy => _session.CanEditAll;

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
    }

    [RelayCommand]
    private async Task ZapiszUstawieniaAsync()
    {
        try
        {
            await ServiceProvider.Services.UstawieniaRepo.SetAsync(UstawieniaKlucze.NrJRG, NrJrg);

            if (Wybranysamochod is not null)
                await ZapiszSamochodCoreAsync(Wybranysamochod);

            StatusMessage = Wybranysamochod is not null
                ? "Ustawienia zapisane (w tym wymagania wybranego pojazdu)."
                : "Ustawienia zapisane.";
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
    }

    [RelayCommand]
    private Task ZapiszSamochodAsync(Samochod s) => ZapiszSamochodCoreAsync(s);

    private async Task ZapiszSamochodCoreAsync(Samochod s, bool odswiezListe = true)
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
        }
        catch (Exception ex)
        {
            StatusMessage = $"Błąd usuwania pojazdu: {ex.Message}";
            SkrybekLog.Error($"Błąd usuwania pojazdu Id={s.Id}, Nazwa={s.Nazwa}", ex);
        }
    }

    // ── Backup ────────────────────────────────────────────────────────────────

    [RelayCommand]
    private async Task WykonajBackupAsync()
    {
        try
        {
            await ServiceProvider.Services.Backup.WykonajBackupAsync();
            StatusMessage = "Backup wykonany pomyślnie.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Błąd backupu: {ex.Message}";
        }
    }
}
