namespace SKRYBEK.Core.Rules;

/// <summary>Poziom gotowości grupy nurkowej na zmianie / w obsadzie.</summary>
public enum PoziomGotowosciNurkowej
{
    Brak = 0,
    A = 1,
    AB = 2
}

/// <summary>Minimalny zestaw cech osoby do oceny poziomu A/AB.</summary>
public readonly record struct OsobaDoOcenyPoziomu(
    int Id,
    bool MaKwalifikacjeNurka,
    bool MaKpp,
    bool MaObslugeLodzi);
