using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Media;

namespace BOBER.App.ViewModels;

public sealed class KolorRoliViewModel : INotifyPropertyChanged
{
    private string _kolorHex = "#2D2D2D";

    public event PropertyChangedEventHandler? PropertyChanged;

    public string KluczRoli { get; init; } = string.Empty;
    public string Etykieta { get; init; } = string.Empty;

    public string KolorHex
    {
        get => _kolorHex;
        set
        {
            _kolorHex = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(PreviewColor));
        }
    }

    public Color PreviewColor
    {
        get
        {
            try { return (Color)ColorConverter.ConvertFromString(_kolorHex); }
            catch { return Color.FromRgb(0x2D, 0x2D, 0x2D); }
        }
    }

    private void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
