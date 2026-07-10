namespace Chomik.Core.Models;

public sealed class Funkcjonariusz
{
    public int Id { get; set; }
    public int NumerZmiany { get; set; }

    /// <summary>
    /// Kolejność na liście (1 = pierwszy wiersz u góry), w obrębie zmiany.
    /// </summary>
    public int NumerPorzadkowy { get; set; }
    public int StopienId { get; set; }
    public int StanowiskoId { get; set; }
    public string Stopien { get; set; } = string.Empty;
    public string Imie { get; set; } = string.Empty;
    public string Nazwisko { get; set; } = string.Empty;
    public string Stanowisko { get; set; } = string.Empty;
    public string? Telefon { get; set; }
    public int? StazLat { get; set; }
    public DateTime? BadaniaOkresoweDo { get; set; }
    public DateTime? KomoraDymowaDo { get; set; }
    public DateTime? KppDo { get; set; }
    public DateTime? DataWstepieniaDoSluzby { get; set; }
    public string? InformacjaDodatkowa { get; set; }

    public DateTime? DataAwansuStopien { get; set; }
    public DateTime? DataAwansuGrupa { get; set; }
    public decimal? DodatekMotywacyjny { get; set; }

    public string PelneImieNazwisko => $"{Imie} {Nazwisko}".Trim();

    public List<UprawnieniePrzypisanie> Uprawnienia { get; set; } = [];
    public List<OdznaczeniePrzypisanie> Odznaczenia { get; set; } = [];
}

