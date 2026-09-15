using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using BOBER.Core.Enums;
using BOBER.Core.Models;

namespace BOBER.App.ViewModels;

public sealed class GrafikZliczaniePozycjaWyboru : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;
    public int Id { get; init; }
    public string Etykieta { get; init; } = string.Empty;

    private bool _isSelected;
    public bool IsSelected
    {
        get => _isSelected;
        set { _isSelected = value; OnPropertyChanged(); }
    }

    private void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

public sealed class GrafikZliczanieGrupaEdit
{
    public ObservableCollection<GrafikZliczaniePozycjaWyboru> Pozycje { get; } = [];
}

public sealed class GrafikZliczanieSlotEdit : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;
    public string Nazwa { get; set; } = string.Empty;
    public GrafikZliczanieZrodlo Zrodlo { get; set; }
    public short Liczba { get; set; } = 1;
    public short Kolejnosc { get; set; }
    public short? WspoldzielSlotKolejnosc { get; set; }
    public ObservableCollection<GrafikZliczanieGrupaEdit> Grupy { get; } = [];

    public string WspoldzielTekst
    {
        get => WspoldzielSlotKolejnosc?.ToString() ?? string.Empty;
        set
        {
            WspoldzielSlotKolejnosc = short.TryParse(value, out var n) ? n : null;
            OnPropertyChanged();
        }
    }

    private void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

public sealed class GrafikZliczaniePoziomEdit
{
    public string Kod { get; set; } = string.Empty;
    public short Kolejnosc { get; set; }
    public ObservableCollection<GrafikZliczanieSlotEdit> Sloty { get; } = [];
}
