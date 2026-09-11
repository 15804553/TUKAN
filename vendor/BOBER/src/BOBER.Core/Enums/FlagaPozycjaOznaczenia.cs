namespace BOBER.Core.Enums;

/// <summary>
/// Pozycja znaczka w komórce.
/// <see cref="Nie"/> = zwykłe oznaczenie (tekst główny).
/// <see cref="Lewa"/> / <see cref="Prawa"/> = mały znacznik w rogu (jak „?” / „.”).
/// <see cref="Centrum"/> = nakładka na środku (jak Oddaje — styl na bazowym symbolu).
/// </summary>
public enum FlagaPozycjaOznaczenia : short
{
    Nie = 0,
    Lewa = 1,
    Prawa = 2,
    Centrum = 3
}
