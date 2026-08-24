using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace BOBER.App.ViewModels;

public sealed class UrlopPlanRowViewModel : INotifyPropertyChanged
{
    private readonly Dictionary<int, string> _cells = new();
    private int _wypoczynkowyCount;
    private int _dodatkowyCount;
    private int _rodzicielskiCount;

    public event PropertyChangedEventHandler? PropertyChanged;

    public int? FunkcjonariuszId { get; init; }
    public bool IsSummaryRow { get; init; }
    public int? Numer { get; init; }
    public string ImieNazwisko { get; init; } = string.Empty;

    private bool _isSelectionHighlight;

    /// <summary>Wiersz aktywny przy zaznaczeniu komórki dnia — podświetla nazwisko.</summary>
    public bool IsSelectionHighlight
    {
        get => _isSelectionHighlight;
        set
        {
            if (_isSelectionHighlight == value)
                return;
            _isSelectionHighlight = value;
            OnPropertyChanged();
        }
    }

    public int WypoczynkowyCount
    {
        get => _wypoczynkowyCount;
        set { _wypoczynkowyCount = value; OnPropertyChanged(); }
    }

    public int DodatkowyCount
    {
        get => _dodatkowyCount;
        set { _dodatkowyCount = value; OnPropertyChanged(); }
    }

    public int RodzicielskiCount
    {
        get => _rodzicielskiCount;
        set { _rodzicielskiCount = value; OnPropertyChanged(); }
    }

    public string this[int day]
    {
        get => _cells.TryGetValue(day, out var v) ? v : string.Empty;
        set
        {
            _cells[day] = value ?? string.Empty;
            OnPropertyChanged($"[{day}]");
        }
    }

    public void SetCell(int day, string value) => this[day] = value;
    public string GetCell(int day) => this[day];
    public void ClearCell(int day) => this[day] = string.Empty;

    private void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
