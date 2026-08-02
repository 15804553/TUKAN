using Tukan.App.Services.GuestAudit;

namespace Tukan.App.Tests.GuestAudit;

public sealed class GuestAuditScopeTests
{
    [Theory]
    [InlineData(GuestAuditModule.Grafik)]
    [InlineData(GuestAuditModule.Personel)]
    [InlineData(GuestAuditModule.Rozkazy)]
    [InlineData(GuestAuditModule.Urlopy)]
    [InlineData(GuestAuditModule.Ustawienia)]
    public void IsEnabled_Domyslnie_ZwracaTrue(GuestAuditModule module)
    {
        var scope = new GuestAuditScope();
        scope.IsEnabled(module).Should().BeTrue();
    }

    [Fact]
    public void IsEnabled_WylaczonyModul_ZwracaFalse()
    {
        var scope = new GuestAuditScope { Grafik = false };
        scope.IsEnabled(GuestAuditModule.Grafik).Should().BeFalse();
        scope.IsEnabled(GuestAuditModule.Personel).Should().BeTrue();
    }

    [Fact]
    public void IsEnabled_NieznanyEnum_ZwracaFalse()
    {
        var scope = new GuestAuditScope();
        scope.IsEnabled((GuestAuditModule)999).Should().BeFalse();
    }
}
