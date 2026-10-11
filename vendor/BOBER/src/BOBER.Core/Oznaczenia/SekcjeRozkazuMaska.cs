using BOBER.Core.Enums;

namespace BOBER.Core.Oznaczenia;

/// <summary>
/// Zapis wielu sekcji rozkazu w jednej kolumnie SHORT.
/// Bit (wartość enum − 1): Urlop=1, Czas wolny=2, Chory=4, Delegacja=8, Dyżur=16.
/// </summary>
public static class SekcjeRozkazuMaska
{
    public static short ToMask(IEnumerable<SekcjaRozkazuGrafiku> sekcje)
    {
        short mask = 0;
        foreach (var sekcja in sekcje)
        {
            var bit = Bit(sekcja);
            if (bit != 0)
                mask |= bit;
        }

        return mask;
    }

    public static List<SekcjaRozkazuGrafiku> FromMask(short mask)
    {
        var wynik = new List<SekcjaRozkazuGrafiku>();
        if (mask <= 0)
            return wynik;

        foreach (var sekcja in Enum.GetValues<SekcjaRozkazuGrafiku>())
        {
            var bit = Bit(sekcja);
            if (bit != 0 && (mask & bit) != 0)
                wynik.Add(sekcja);
        }

        return wynik;
    }

    private static short Bit(SekcjaRozkazuGrafiku sekcja)
    {
        var index = (int)sekcja - 1;
        if (index is < 0 or > 14)
            return 0;
        return (short)(1 << index);
    }
}
