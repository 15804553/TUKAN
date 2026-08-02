using Tukan.App.ViewModels;

namespace Tukan.App.Tests.ViewModels;

public sealed class DutyAssignmentsRowViewModelTests
{
    [Fact]
    public void AddAssignment_HappyPath_UstawiaKod()
    {
        var row = new DutyAssignmentsRowViewModel { Numer = 1, ImieNazwisko = "Jan Kowalski" };
        row.AddAssignment(5, "DZ");
        row[5].Should().Be("DZ");
    }

    [Fact]
    public void AddAssignment_DrugiKod_LaczySlash()
    {
        var row = new DutyAssignmentsRowViewModel();
        row.AddAssignment(1, "DZ");
        row.AddAssignment(1, "PA");
        row[1].Should().Be("DZ / PA");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void AddAssignment_PustyKod_Ignoruje(string? code)
    {
        var row = new DutyAssignmentsRowViewModel();
        row.AddAssignment(1, code!);
        row[1].Should().BeEmpty();
    }

    [Fact]
    public void AddAssignment_DuplikatCaseInsensitive_NieDodaje()
    {
        var row = new DutyAssignmentsRowViewModel();
        row.AddAssignment(1, "dz");
        row.AddAssignment(1, "DZ");
        row[1].Should().Be("dz");
    }

    [Fact]
    public void Indeksator_BrakDnia_ZwracaPusty()
    {
        var row = new DutyAssignmentsRowViewModel();
        row[31].Should().BeEmpty();
    }
}
