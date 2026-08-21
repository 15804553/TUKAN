using System.Linq;
using SKRYBEK.Core.Chomik;
using SKRYBEK.Core.Rules;

namespace SKRYBEK.Core.Models;

public sealed class Funkcjonariusz
{
    public int Id { get; set; }
    public int NumerZmiany { get; set; }
    /// <summary>Numer kolejności w zmianie — ta sama numeracja co w widoku ogólnym i grafiku.</summary>
    public int NumerPorzadkowy { get; set; }
    public int StopienId { get; set; }
    public int StanowiskoId { get; set; }
    public string Stopien { get; set; } = string.Empty;
    public string Imie { get; set; } = string.Empty;
    public string Nazwisko { get; set; } = string.Empty;
    public string Stanowisko { get; set; } = string.Empty;
    public string? Telefon { get; set; }
    public int? StazLat { get; set; }

    public string PelneImieNazwisko => $"{Imie} {Nazwisko}".Trim();

    /// <summary>Stopień, imię i nazwisko — format używany w kontrolkach i eksporcie Word.</summary>
    public string StopienINazwisko =>
        string.Join(" ", new[] { Stopien, Imie, Nazwisko }.Where(s => !string.IsNullOrWhiteSpace(s)));

    /// <summary>Stary format bez imienia — dopasowanie zapisów sprzed uzupełnienia imion.</summary>
    public string StopienINazwiskoBezImienia =>
        string.Join(" ", new[] { Stopien, Nazwisko }.Where(s => !string.IsNullOrWhiteSpace(s)));

    /// <summary>Id typów uprawnień z tabeli TypyUprawnien (CHOMIK).</summary>
    public List<int> IdUprawnien { get; set; } = [];

    /// <summary>Pełne nazwy uprawnień (Nazwa + Podtyp) z CHOMIK — do wyświetlania i filtrów.</summary>
    public List<string> NazwyUprawnien { get; set; } = [];

    public bool MaUprawnieniaKierowcaC =>
        IdUprawnien.Contains(ChomikSlowniki.UprawnienieKierowcaKatC);

    public bool MaUprawnieniaKierowcaCE =>
        IdUprawnien.Contains(ChomikSlowniki.UprawnienieKierowcaKatCE);

    public bool MaUprawnieniaKierowca => MaUprawnieniaKierowcaC || MaUprawnieniaKierowcaCE;

    public bool MaUprawnieniaNumek =>
        IdUprawnien.Contains(ChomikSlowniki.UprawnienieNurek)
        || NazwyUprawnien.Any(u => u.Contains("Nurek", StringComparison.OrdinalIgnoreCase));

    public bool MaUprawnieniaKPP =>
        IdUprawnien.Contains(ChomikSlowniki.UprawnienieKPP);

    public bool MaUprawnieniaObslugaLodzi =>
        NazwyUprawnien.Any(PoziomGotowosciNurkowejRules.CzyEtykietaObslugiLodzi);

    public bool MaUprawnienieDowodzeniePrzyAkcji =>
        NazwyUprawnien.Any(ChomikSlowniki.CzyUprawnienieDowodzeniePrzyAkcji);

    /// <summary>Miejsce 1.D — stanowisko dowódcze z CHOMIK albo uprawnienie „Dowodzenie przy akcji”.</summary>
    public bool CzyMozeNaMiejsce1DPojazdu =>
        ChomikSlowniki.CzyMozeNaMiejsce1DPojazdu(StanowiskoId)
        || MaUprawnienieDowodzeniePrzyAkcji;
}
