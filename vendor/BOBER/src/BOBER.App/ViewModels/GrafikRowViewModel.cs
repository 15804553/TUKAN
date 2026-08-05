using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Media;

namespace BOBER.App.ViewModels;

public sealed class GrafikRowViewModel : INotifyPropertyChanged
{
    private readonly Dictionary<int, string> _cells = new();
    private readonly DayIndexedTexts _kalendarzNotes = new();
    private readonly DayIndexedFlags _fromUrlopPlan = new();

    public event PropertyChangedEventHandler? PropertyChanged;

    public int? FunkcjonariuszId { get; init; }
    public bool IsSummaryRow { get; init; }
    /// <summary>Wiersz ikon notatek pod sumą (Poziom A/AB) — komórki zawierają treść notatki dnia.</summary>
    public bool IsNotesRow { get; init; }
    /// <summary>Numer porządkowy (Lp.) wynikający z pozycji na liście. Null dla wiersza sumarycznego.</summary>
    public int? Numer { get; init; }
    public string ImieNazwisko { get; init; } = string.Empty;
    public string Stanowisko { get; init; } = string.Empty;
    public string KluczRoli { get; init; } = string.Empty;
    /// <summary>Ma uprawnienia nurka (KPP lub Nurek) — czerwone obramowanie w kolumnie imienia.</summary>
    public bool IsNurek { get; init; }
    public Brush RowBackground { get; set; } = Brushes.Transparent;
    public Brush RowForeground { get; set; } = Brushes.White;
    /// <summary>Kolor obramowania imienia (czerwony dla nurka, przezroczysty w pozostałych przypadkach).</summary>
    public Brush NameBorderBrush { get; set; } = Brushes.Transparent;

    private bool _isSelectionHighlight;

    /// <summary>Wiersz aktywny przy zaznaczeniu komórki dnia — podświetla Lp. i imię.</summary>
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

    private string _uwagaMiesieczna = string.Empty;

    /// <summary>Tekst uwagi miesięcznej w ostatniej kolumnie grafiku (per pracownik / miesiąc).</summary>
    public string UwagaMiesieczna
    {
        get => _uwagaMiesieczna;
        set
        {
            var next = value ?? string.Empty;
            if (_uwagaMiesieczna == next)
                return;
            _uwagaMiesieczna = next;
            OnPropertyChanged();
        }
    }

    /// <summary>Notatki kalendarza DCA per dzień — binding: KalendarzNotes[15].</summary>
    public DayIndexedTexts KalendarzNotes => _kalendarzNotes;

    /// <summary>Urlopy przeniesione z planu urlopów (IsAuto) — binding: FromUrlopPlan[15].</summary>
    public DayIndexedFlags FromUrlopPlan => _fromUrlopPlan;

    /// <summary>Indekser dla kolumn dni DataGrid — binding: {Binding [1]}, {Binding [15]}, itd.</summary>
    public string this[int day]
    {
        get => _cells.TryGetValue(day, out var v) ? v : string.Empty;
        set
        {
            _cells[day] = value ?? string.Empty;
            OnPropertyChanged($"Item[{day}]");
            OnPropertyChanged($"[{day}]");
        }
    }

    public void SetCell(int day, string value, bool fromUrlopPlan = false)
    {
        this[day] = value;
        FromUrlopPlan[day] = fromUrlopPlan && !string.IsNullOrWhiteSpace(value);
    }

    public string GetCell(int day) => this[day];

    public void ClearCell(int day)
    {
        this[day] = string.Empty;
        FromUrlopPlan[day] = false;
    }

    public void SetKalendarzNote(int day, string value) => KalendarzNotes[day] = value;

    private void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

/// <summary>Indeksowane flagi dni do bindingu WPF (np. FromUrlopPlan[3]).</summary>
public sealed class DayIndexedFlags : INotifyPropertyChanged
{
    private readonly Dictionary<int, bool> _flags = new();

    public event PropertyChangedEventHandler? PropertyChanged;

    public bool this[int day]
    {
        get => _flags.TryGetValue(day, out var v) && v;
        set
        {
            if (_flags.TryGetValue(day, out var current) && current == value)
                return;
            _flags[day] = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs($"Item[{day}]"));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs($"[{day}]"));
        }
    }
}

/// <summary>Indeksowane teksty dni do bindingu WPF (np. KalendarzNotes[3]).</summary>
public sealed class DayIndexedTexts : INotifyPropertyChanged
{
    private readonly Dictionary<int, string> _cells = new();

    public event PropertyChangedEventHandler? PropertyChanged;

    public string this[int day]
    {
        get => _cells.TryGetValue(day, out var v) ? v : string.Empty;
        set
        {
            _cells[day] = value ?? string.Empty;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs($"Item[{day}]"));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs($"[{day}]"));
        }
    }
}
