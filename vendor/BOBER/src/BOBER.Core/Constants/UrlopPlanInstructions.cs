namespace BOBER.Core.Constants;

/// <summary>Wytyczne planowania urlopów (zgodne ze wzorcem Excel i regułami R1–R8).</summary>
public static class UrlopPlanInstructions
{
    public const int LimitWypoczynkowy = 20;
    public const int LimitDodatkowy = 13;
    public const int LimitRodzicielski = 63;
    public const int LimitWakacjeWypoczynkowy = 15;
    public const int DefaultMaxUrlopowNaSluzbie = 5;
    public const int MinCzescDodatkowy = 6;
    public const int MaxCzesciDodatkowy = 2;
    public const int MaxCzesciRodzicielski = 5;

    public static IReadOnlyList<string> Rules =>
    [
        "Urlopy planujemy „służbami” — w interwale 3-dniowym cyklu zmiany.",
        "Urlop zaczyna się w dniu służby i kończy w dniu bezpośrednio przed następną służbą.",
        $"Urlop wypoczynkowy (w): planujemy {LimitWypoczynkowy} dni z 26 przysługujących w roku.",
        $"Urlop dodatkowy (d): planujemy {LimitDodatkowy} dni; można podzielić maksymalnie na {MaxCzesciDodatkowy} części, z których każda musi trwać co najmniej {MinCzescDodatkowy} dni.",
        $"Urlop rodzicielski (r): planujemy {LimitRodzicielski} dni; można podzielić maksymalnie na {MaxCzesciRodzicielski} części. W grafiku służb oznaczany jako Uᵣ (jak Uₚ dla pozostałych urlopów z planu).",
        "Nie wpisujemy w plan urlopów urlopów zaległych.",
        $"W sezonie czerwiec–wrzesień planujemy maksymalnie {LimitWakacjeWypoczynkowy} dni wypoczynkowych.",
        $"W jednym dniu służby na urlopie mogą być maksymalnie {DefaultMaxUrlopowNaSluzbie} osoby ze zmiany (wartość w Ustawieniach).",
        "Nie planujemy urlopów w święta Wielkanocne oraz Bożego Narodzenia."
    ];

    public static IReadOnlyList<string> Skroty =>
    [
        "Shift+klik lub Ctrl+klik — zaznacz kilka dni w wierszu",
        "W — urlop wypoczynkowy (dla zaznaczonych komórek)",
        "D — urlop dodatkowy (dla zaznaczonych komórek)",
        "R — urlop rodzicielski (dla zaznaczonych komórek)",
        "Spacja — wyczyść zaznaczone komórki",
        "Prawy przycisk myszy — menu kontekstowe"
    ];
}
