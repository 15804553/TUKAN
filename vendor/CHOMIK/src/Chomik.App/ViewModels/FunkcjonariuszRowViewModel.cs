using System.ComponentModel;
using System.Runtime.CompilerServices;
using Chomik.Core;

namespace Chomik.App.ViewModels;

public sealed class FunkcjonariuszRowViewModel : INotifyPropertyChanged
{
    public int FunkcjonariuszId { get; init; }
    public int? WybraneUprawnieniePrzypisanieId { get; init; }
    private int _numerZmiany;
    public int NumerZmiany
    {
        get => _numerZmiany;
        set => SetField(ref _numerZmiany, value);
    }

    public bool CanEditZmiana { get; init; }

    public bool CanEditStopien { get; init; }

    private int _stopienId;
    public int StopienId
    {
        get => _stopienId;
        set => SetField(ref _stopienId, value);
    }

    private string _stopien = string.Empty;
    public string Stopien
    {
        get => _stopien;
        set => SetField(ref _stopien, value);
    }
    public string PelneImieNazwisko { get; init; } = string.Empty;
    public string Stanowisko { get; init; } = string.Empty;
    public string? Telefon { get; init; }
    public DateTime? DataWstepieniaDoSluzby { get; init; }

    public int? StazLat { get; init; }

    private DateTime? _badaniaOkresoweDo;
    public DateTime? BadaniaOkresoweDo
    {
        get => _badaniaOkresoweDo;
        set => SetField(ref _badaniaOkresoweDo, value);
    }

    private DateTime? _komoraDymowaDo;
    public DateTime? KomoraDymowaDo
    {
        get => _komoraDymowaDo;
        set => SetField(ref _komoraDymowaDo, value);
    }

    private DateTime? _kppDo;
    public DateTime? KppDo
    {
        get => _kppDo;
        set => SetField(ref _kppDo, value);
    }

    public string UprawnieniaSkrot { get; init; } = string.Empty;

    public bool HasUprawnieniaAlert { get; init; }

    public DateValidityStatus? UprawnieniaAlertSeverity { get; init; }

    public string? UprawnieniaAlertTooltip { get; init; }

    private DateTime? _wybraneUprawnienieWazneDo;
    public DateTime? WybraneUprawnienieWazneDo
    {
        get => _wybraneUprawnienieWazneDo;
        set => SetField(ref _wybraneUprawnienieWazneDo, value);
    }

    public string? DodatekMotywacyjny { get; init; }
    public string? DataAwansuStopien { get; init; }
    public string? OdznaczeniaSkrot { get; init; }

    public string? InneUwagi { get; init; }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return;
        }

        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

public sealed class FunkcjonariuszRowFilter
{
    public int? NumerZmiany { get; init; }
    public string? UprawnienieNazwa { get; init; }
    public string? UprawnieniePodtyp { get; init; }
    public string? Szukaj { get; init; }
}

public sealed class PermissionFilterOption
{
    public string Label { get; init; } = string.Empty;
    public string? Nazwa { get; init; }
    public string? Podtyp { get; init; }

    public override string ToString() => Label;
}

public sealed class ShiftFilterOption
{
    public string Label { get; init; } = string.Empty;
    public int? Value { get; init; }

    public override string ToString() => Label;
}
