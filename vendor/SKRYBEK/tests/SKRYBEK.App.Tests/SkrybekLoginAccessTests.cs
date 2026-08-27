using SKRYBEK.App.Helpers;

namespace SKRYBEK.App.Tests;

public sealed class SkrybekLoginAccessTests
{
    [Fact]
    public void DcaJrg_UkrywaZapiszIEksport_PokazujeComboZatwierdz()
    {
        Assert.False(SkrybekLoginAccess.ShowSaveButton("DCA JRG", canEditAll: true));
        Assert.False(SkrybekLoginAccess.ShowExportWordButton(canEditAll: true));
        Assert.True(SkrybekLoginAccess.ShowApproveCombo("DCA JRG", canEditAll: true));
    }

    [Fact]
    public void Zmiana_PokazujeZapiszIEksport_BezComboZatwierdz()
    {
        Assert.True(SkrybekLoginAccess.ShowSaveButton("Zmiana 1", canEditAll: false));
        Assert.True(SkrybekLoginAccess.ShowExportWordButton(canEditAll: false));
        Assert.False(SkrybekLoginAccess.ShowApproveCombo("Zmiana 1", canEditAll: false));
    }

    [Fact]
    public void Pa_UkrywaZapiszICombo()
    {
        Assert.False(SkrybekLoginAccess.ShowSaveButton("PA", canEditAll: false));
        Assert.False(SkrybekLoginAccess.ShowApproveCombo("PA", canEditAll: true));
    }
}
