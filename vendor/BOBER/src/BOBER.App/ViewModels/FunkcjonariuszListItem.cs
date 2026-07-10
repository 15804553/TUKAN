using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace BOBER.App.ViewModels;

public sealed class FunkcjonariuszListItem : INotifyPropertyChanged
{
    private int _numer;

    public int Id { get; init; }
    public string ImieNazwisko { get; init; } = string.Empty;
    public string Stanowisko { get; init; } = string.Empty;

    public int Numer
    {
        get => _numer;
        set
        {
            if (_numer == value) return;
            _numer = value;
            OnPropertyChanged();
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    public override string ToString() => $"{ImieNazwisko} ({Stanowisko})";
}
