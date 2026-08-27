using SKRYBEK.Core.Models;

namespace SKRYBEK.Core.Rules;

/// <summary>
/// Reguły obsady pojazdów podstawowych w podziale bojowym rozkazu dziennego.
/// Jedna osoba może zajmować co najwyżej jedno miejsce łącznie na wszystkich
/// pojazdach oznaczonych jako podstawowe (także w ramach jednego pojazdu).
/// </summary>
public static class PodzialBojowyRules
{
    public const string KomunikatKonfliktMiejsc =
        "Ta sama osoba nie może zajmować dwóch miejsc na pojazdach podstawowych.";

    /// <summary>
    /// True, gdy przypisanie osoby na wskazane miejsce pojazdu podstawowego koliduje
    /// z innym już obsadzonym miejscem na dowolnym pojeździe podstawowym.
    /// </summary>
    public static bool CzyKonfliktPodstawowy(
        IEnumerable<PozycjaSamochodu> podzialBojowy,
        IEnumerable<Samochod> samochody,
        int funkcjonariuszId,
        int docelowySamochodId,
        int docelowaPozycja)
    {
        var docelowy = samochody.FirstOrDefault(s => s.Id == docelowySamochodId);
        if (docelowy?.CzyPodstawowy != true)
            return false;

        var podstawoweIds = samochody
            .Where(s => s.CzyPodstawowy)
            .Select(s => s.Id)
            .ToHashSet();

        return podzialBojowy.Any(p =>
            p.FunkcjonariuszId == funkcjonariuszId
            && podstawoweIds.Contains(p.SamochodId)
            && !(p.SamochodId == docelowySamochodId && p.Pozycja == docelowaPozycja));
    }

    /// <summary>
    /// Znajduje pierwszą osobę przypisaną więcej niż raz na pojazdach podstawowych
    /// (dwa miejsca tego samego pojazdu albo dwa różne pojazdy podstawowe).
    /// </summary>
    public static string? ZnajdzKomunikatDuplikatuNaPodstawowych(
        IEnumerable<PozycjaSamochodu> podzialBojowy,
        IEnumerable<Samochod> samochody)
    {
        var podstawoweIds = samochody.Where(s => s.CzyPodstawowy).Select(s => s.Id).ToHashSet();
        var duplikat = podzialBojowy
            .Where(p => p.FunkcjonariuszId.HasValue && podstawoweIds.Contains(p.SamochodId))
            .GroupBy(p => p.FunkcjonariuszId!.Value)
            .FirstOrDefault(g => g.Count() > 1);

        if (duplikat is null)
            return null;

        var nazwisko = duplikat.First().Nazwisko;
        var liczbaPojazdow = duplikat.Select(p => p.SamochodId).Distinct().Count();

        if (liczbaPojazdow == 1)
        {
            return $"Osoba {nazwisko} jest przypisana do więcej niż jednego miejsca na tym samym pojeździe podstawowym. " +
                   KomunikatKonfliktMiejsc;
        }

        return $"Osoba {nazwisko} jest przypisana do więcej niż jednego pojazdu podstawowego. " +
               KomunikatKonfliktMiejsc;
    }
}
