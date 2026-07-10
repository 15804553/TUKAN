namespace Chomik.Core.Models;

public sealed class TypUprawnienia
{
    public int Id { get; set; }
    public string Nazwa { get; set; } = string.Empty;
    public string? Podtyp { get; set; }
    public bool WymagaDaty { get; set; }

    public string Etykieta => string.IsNullOrWhiteSpace(Podtyp) ? Nazwa : $"{Nazwa} ({Podtyp})";
}
