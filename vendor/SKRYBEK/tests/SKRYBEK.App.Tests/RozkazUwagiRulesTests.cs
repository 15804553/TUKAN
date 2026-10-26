using SKRYBEK.Core.Rules;

namespace SKRYBEK.App.Tests;

public sealed class RozkazUwagiRulesTests
{
    [Fact]
    public void DopiszNotatke_PusteUwagi_ZwracaTrescNotatki()
    {
        var wynik = RozkazUwagiRules.DopiszNotatke("  ", "  Briefing o 7:00  ");
        Assert.Equal("Briefing o 7:00", wynik);
    }

    [Fact]
    public void DopiszNotatke_IstniejaceUwagi_DopisujeNowaLinie()
    {
        var wynik = RozkazUwagiRules.DopiszNotatke("Ćwiczenia", "Notatka z kalendarza");
        Assert.Equal($"Ćwiczenia{Environment.NewLine}Notatka z kalendarza", wynik);
    }

    [Fact]
    public void DopiszNotatke_TaSamaTrescJuzJest_NieDuplikuje()
    {
        var uwagi = $"Ćwiczenia{Environment.NewLine}Notatka z kalendarza";
        var wynik = RozkazUwagiRules.DopiszNotatke(uwagi, "Notatka z kalendarza");
        Assert.Equal(uwagi, wynik);
    }

    [Fact]
    public void DopiszNotatke_PustaNotatka_ZwracaIstniejaceUwagi()
    {
        var wynik = RozkazUwagiRules.DopiszNotatke("Ćwiczenia", "   ");
        Assert.Equal("Ćwiczenia", wynik);
    }

    [Fact]
    public void DopiszNotatke_WieloliniowaNotatka_NieDuplikujeBloku()
    {
        var notatka = $"Briefing{Environment.NewLine}zbiórka o 7:00";
        var uwagi = $"Ćwiczenia{Environment.NewLine}{notatka}";
        var wynik = RozkazUwagiRules.DopiszNotatke(uwagi, notatka);
        Assert.Equal(uwagi, wynik);
    }
}
