using BOBER.Core.Constants;

namespace BOBER.Services.Tests.Kolory;

public sealed class NameBorderPlanResolverTests
{
    private const string Nurek = "#F80808";
    private const string Sonar = "#1E88E5";

    [Fact]
    public void Resolve_TylkoNurek_JedenPierscien()
    {
        var plan = NameBorderPlanResolver.Resolve(true, false, true, true, Nurek, Sonar);

        Assert.Equal(Nurek, plan.OuterHex);
        Assert.False(plan.HasInner);
    }

    [Fact]
    public void Resolve_TylkoSonar_JedenPierscien()
    {
        var plan = NameBorderPlanResolver.Resolve(false, true, true, true, Nurek, Sonar);

        Assert.Equal(Sonar, plan.OuterHex);
        Assert.False(plan.HasInner);
    }

    [Fact]
    public void Resolve_ObaUprawnienia_NurekNaZewnatrzSonarWewnatrz()
    {
        var plan = NameBorderPlanResolver.Resolve(true, true, true, true, Nurek, Sonar);

        Assert.Equal(Nurek, plan.OuterHex);
        Assert.Equal(Sonar, plan.InnerHex);
    }

    [Fact]
    public void Resolve_WylaczonySonarPrzyObuUprawnieniach_ZostajeNurek()
    {
        var plan = NameBorderPlanResolver.Resolve(true, true, true, false, Nurek, Sonar);

        Assert.Equal(Nurek, plan.OuterHex);
        Assert.False(plan.HasInner);
    }

    [Fact]
    public void Resolve_WylaczonyNurekPrzyObuUprawnieniach_ZostajeSonar()
    {
        var plan = NameBorderPlanResolver.Resolve(true, true, false, true, Nurek, Sonar);

        Assert.Equal(Sonar, plan.OuterHex);
        Assert.False(plan.HasInner);
    }
}
