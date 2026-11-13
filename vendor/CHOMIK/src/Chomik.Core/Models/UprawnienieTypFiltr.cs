namespace Chomik.Core.Models;

/// <summary>Jedno uprawnienie lub kurs używane jako warunek filtra listy osób.</summary>
public sealed class UprawnienieTypFiltr
{
    public required string Nazwa { get; init; }

    public string? Podtyp { get; init; }

    public bool Pasuje(string nazwa, string? podtyp)
    {
        if (!Nazwa.Trim().Equals(nazwa.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var oczekiwany = Podtyp?.Trim();
        var faktyczny = podtyp?.Trim();
        if (string.IsNullOrWhiteSpace(oczekiwany))
        {
            return string.IsNullOrWhiteSpace(faktyczny);
        }

        return oczekiwany.Equals(faktyczny, StringComparison.OrdinalIgnoreCase);
    }
}
