using System.ComponentModel;
using System.Runtime.CompilerServices;
using BOBER.Core.Enums;

namespace BOBER.App.ViewModels;

/// <summary>Zaznaczenie kilku sekcji nieobecnych dla jednego oznaczenia.</summary>
public sealed class GrupyRozkazuWybor : INotifyPropertyChanged
{
    private static readonly (SekcjaRozkazuGrafiku Sekcja, string Etykieta)[] Kolejnosc =
    [
        (SekcjaRozkazuGrafiku.Urlop, "Urlopy"),
        (SekcjaRozkazuGrafiku.CzasWolny, "Wolna służba"),
        (SekcjaRozkazuGrafiku.Chory, "Chorzy"),
        (SekcjaRozkazuGrafiku.Delegowany, "Delegacje"),
        (SekcjaRozkazuGrafiku.DyzurDomowy, "Dyżur domowy")
    ];

    private readonly HashSet<SekcjaRozkazuGrafiku> _sekcje = [];

    public event PropertyChangedEventHandler? PropertyChanged;

    public bool Urlopy
    {
        get => Zawiera(SekcjaRozkazuGrafiku.Urlop);
        set => Ustaw(SekcjaRozkazuGrafiku.Urlop, value, nameof(Urlopy));
    }

    public bool WolnaSluzba
    {
        get => Zawiera(SekcjaRozkazuGrafiku.CzasWolny);
        set => Ustaw(SekcjaRozkazuGrafiku.CzasWolny, value, nameof(WolnaSluzba));
    }

    public bool Chorzy
    {
        get => Zawiera(SekcjaRozkazuGrafiku.Chory);
        set => Ustaw(SekcjaRozkazuGrafiku.Chory, value, nameof(Chorzy));
    }

    public bool Delegacje
    {
        get => Zawiera(SekcjaRozkazuGrafiku.Delegowany);
        set => Ustaw(SekcjaRozkazuGrafiku.Delegowany, value, nameof(Delegacje));
    }

    public bool DyzurDomowy
    {
        get => Zawiera(SekcjaRozkazuGrafiku.DyzurDomowy);
        set => Ustaw(SekcjaRozkazuGrafiku.DyzurDomowy, value, nameof(DyzurDomowy));
    }

    public bool MaWybor => _sekcje.Count > 0;

    public string Podsumowanie
    {
        get
        {
            var nazwy = Kolejnosc
                .Where(e => _sekcje.Contains(e.Sekcja))
                .Select(e => e.Etykieta)
                .ToList();
            return nazwy.Count == 0 ? "—" : string.Join(", ", nazwy);
        }
    }

    public IReadOnlyList<SekcjaRozkazuGrafiku> Wybrane =>
        Kolejnosc.Where(e => _sekcje.Contains(e.Sekcja)).Select(e => e.Sekcja).ToArray();

    public void Ustaw(IEnumerable<SekcjaRozkazuGrafiku> sekcje)
    {
        _sekcje.Clear();
        foreach (var sekcja in sekcje)
            _sekcje.Add(sekcja);
        PowiadomWszystko();
    }

    public void Wyczysc()
    {
        if (_sekcje.Count == 0)
            return;
        _sekcje.Clear();
        PowiadomWszystko();
    }

    private bool Zawiera(SekcjaRozkazuGrafiku sekcja) => _sekcje.Contains(sekcja);

    private void Ustaw(SekcjaRozkazuGrafiku sekcja, bool wybrana, string nazwa)
    {
        var zmieniono = wybrana ? _sekcje.Add(sekcja) : _sekcje.Remove(sekcja);
        if (!zmieniono)
            return;
        OnPropertyChanged(nazwa);
        OnPropertyChanged(nameof(Podsumowanie));
        OnPropertyChanged(nameof(MaWybor));
    }

    private void PowiadomWszystko()
    {
        OnPropertyChanged(nameof(Urlopy));
        OnPropertyChanged(nameof(WolnaSluzba));
        OnPropertyChanged(nameof(Chorzy));
        OnPropertyChanged(nameof(Delegacje));
        OnPropertyChanged(nameof(DyzurDomowy));
        OnPropertyChanged(nameof(Podsumowanie));
        OnPropertyChanged(nameof(MaWybor));
    }

    private void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
