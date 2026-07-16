using System.ComponentModel;
using System.Runtime.CompilerServices;
using SKRYBEK.Core.Enums;

namespace SKRYBEK.Core.Models;

public sealed class RozkazDzienny : INotifyPropertyChanged
{
    private StatusRozkazu _status;

    public int Id { get; set; }
    public int NumerRozkazu { get; set; }
    public int Rok { get; set; }
    public DateOnly Data { get; set; }
    public int ZmianaId { get; set; }
    public string Zajecia { get; set; } = string.Empty;
    public string Uwagi { get; set; } = string.Empty;
    public DateTime DataUtworzenia { get; set; }

    public StatusRozkazu Status
    {
        get => _status;
        set
        {
            if (_status == value)
                return;

            _status = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(CzyZablokowany));
        }
    }

    /// <summary>Rozkaz zatwierdzony przez DCA JRG — edycja zablokowana.</summary>
    public bool CzyZablokowany => Status == StatusRozkazu.Zatwierdzony;

    public string NumerFormatowany => $"{NumerRozkazu}/{Rok}";
    public string DataFormatowana => Data.ToString("dd.MM.yyyy");

    public List<PozycjaSluzby> Sluzba { get; set; } = [];
    public List<PozycjaSamochodu> PodzialBojowy { get; set; } = [];
    public List<RatownikMedyczny> RatwnicyMedyczni { get; set; } = [];
    public List<NieobecnyWSluzbie> Nieobecni { get; set; } = [];

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
