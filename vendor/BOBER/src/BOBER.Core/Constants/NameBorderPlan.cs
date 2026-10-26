namespace BOBER.Core.Constants;

/// <summary>
/// Obramowanie imienia: jeden pierścień albo dwa (nurek na zewnątrz, sonar wewnątrz).
/// Pusty hex oznacza brak pierścienia.
/// </summary>
public readonly record struct NameBorderPlan(string? OuterHex, string? InnerHex)
{
    public bool HasOuter => !string.IsNullOrWhiteSpace(OuterHex);

    public bool HasInner => !string.IsNullOrWhiteSpace(InnerHex);
}

public static class NameBorderPlanResolver
{
    /// <summary>
    /// Zewnętrzny pierścień to kolor nurka, a gdy osoby nie wyróżnia nurek — kolor sonara.
    /// Wewnętrzny pierścień pojawia się tylko wtedy, gdy oba wyróżnienia są włączone.
    /// </summary>
    public static NameBorderPlan Resolve(
        bool isNurek,
        bool isSonarzysta,
        bool nurekAktywny,
        bool sonarAktywny,
        string nurekHex,
        string sonarHex)
    {
        var nurek = isNurek && nurekAktywny;
        var sonar = isSonarzysta && sonarAktywny;

        if (nurek && sonar)
            return new NameBorderPlan(nurekHex, sonarHex);
        if (nurek)
            return new NameBorderPlan(nurekHex, null);
        if (sonar)
            return new NameBorderPlan(sonarHex, null);

        return new NameBorderPlan(null, null);
    }
}
