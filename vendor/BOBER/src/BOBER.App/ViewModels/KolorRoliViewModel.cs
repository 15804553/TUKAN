using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Media;
using BOBER.Core.Constants;

namespace BOBER.App.ViewModels;

public sealed class KolorRoliViewModel : INotifyPropertyChanged
{
    private string _kolorHex = "#2D2D2D";

    public event PropertyChangedEventHandler? PropertyChanged;

    public string KluczRoli { get; init; } = string.Empty;
    public string Etykieta { get; init; } = string.Empty;

    /// <summary>Czy dozwolony brak wypełnienia (pusty hex / None) — Del, S.</summary>
    public bool AllowEmpty { get; init; }

    public string KolorHex
    {
        get => _kolorHex;
        set
        {
            _kolorHex = value ?? string.Empty;
            OnPropertyChanged();
            OnPropertyChanged(nameof(PreviewColor));
            OnPropertyChanged(nameof(HasFill));
            OnPropertyChanged(nameof(DisplayHex));
        }
    }

    /// <summary>Wartość w textboxie — puste gdy brak wypełnienia.</summary>
    public string DisplayHex
    {
        get => RoleKeys.IsBrakWypelnienia(_kolorHex) ? string.Empty : _kolorHex;
        set
        {
            if (AllowEmpty && string.IsNullOrWhiteSpace(value))
            {
                KolorHex = RoleKeys.BrakWypelnienia;
                return;
            }

            KolorHex = value?.Trim() ?? string.Empty;
        }
    }

    public bool HasFill => !RoleKeys.IsBrakWypelnienia(_kolorHex);

    public Color PreviewColor
    {
        get
        {
            if (!HasFill)
                return Color.FromArgb(0x40, 0xA8, 0x98, 0x68);

            try { return (Color)ColorConverter.ConvertFromString(_kolorHex); }
            catch { return Color.FromRgb(0x2D, 0x2D, 0x2D); }
        }
    }

    public void ClearFill()
    {
        if (AllowEmpty)
            KolorHex = RoleKeys.BrakWypelnienia;
    }

    private void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
