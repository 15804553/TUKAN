using SKRYBEK.Services;

namespace SKRYBEK.App;

/// <summary>Dostęp do serwisów SKRYBEK — ustawiane przy starcie TUKAN (lub legacy standalone).</summary>
public static class ServiceProvider
{
    public static AppServices Services { get; set; } = null!;
}
