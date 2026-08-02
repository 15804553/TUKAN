namespace Tukan.App.Services.GuestAudit;

public enum GuestAuditModule
{
    Grafik,
    Personel,
    Rozkazy,
    Urlopy,
    Ustawienia
}

public sealed class GuestAuditScope
{
    public bool Grafik { get; set; } = true;
    public bool Personel { get; set; } = true;
    public bool Rozkazy { get; set; } = true;
    public bool Urlopy { get; set; } = true;
    public bool Ustawienia { get; set; } = true;

    public bool IsEnabled(GuestAuditModule module) => module switch
    {
        GuestAuditModule.Grafik => Grafik,
        GuestAuditModule.Personel => Personel,
        GuestAuditModule.Rozkazy => Rozkazy,
        GuestAuditModule.Urlopy => Urlopy,
        GuestAuditModule.Ustawienia => Ustawienia,
        _ => false
    };
}
