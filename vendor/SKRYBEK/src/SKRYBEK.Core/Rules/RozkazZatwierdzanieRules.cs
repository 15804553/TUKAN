using SKRYBEK.Core.Enums;
using SKRYBEK.Core.Models;

namespace SKRYBEK.Core.Rules;

/// <summary>Reguły zbiorczego zatwierdzania / odblokowywania rozkazów (DCA JRG).</summary>
public static class RozkazZatwierdzanieRules
{
    /// <summary>
    /// Gdy istnieją rozkazy robocze — zatwierdź je wszystkie.
    /// W przeciwnym razie (wszystkie zatwierdzone) — odblokuj wszystkie zatwierdzone.
    /// </summary>
    public static bool CzyZatwierdzicWszystkie(IReadOnlyList<RozkazDzienny> rozkazy) =>
        rozkazy.Any(r => r.Status == StatusRozkazu.Roboczy);

    public static IReadOnlyList<RozkazDzienny> FiltrujDoZatwierdzenia(IReadOnlyList<RozkazDzienny> rozkazy) =>
        rozkazy.Where(r => r.Status == StatusRozkazu.Roboczy).ToList();

    public static IReadOnlyList<RozkazDzienny> FiltrujDoOdblokowania(IReadOnlyList<RozkazDzienny> rozkazy) =>
        rozkazy.Where(r => r.Status == StatusRozkazu.Zatwierdzony).ToList();
}
