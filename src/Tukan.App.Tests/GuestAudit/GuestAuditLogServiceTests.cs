using Tukan.App.Services.GuestAudit;

namespace Tukan.App.Tests.GuestAudit;

public sealed class GuestAuditLogServiceTests
{
    [Fact]
    public void AppendLineWithMonthSection_PustyPlik_TworzyNaglowekIWpis()
    {
        var now = new DateTime(2026, 8, 2, 14, 0, 0);
        var result = GuestAuditLogService.AppendLineWithMonthSection(
            "", now, "02.08.2026 14:00 Zmiana X");

        result.Should().StartWith("=== 2026-08 ===");
        result.Should().Contain("02.08.2026 14:00 Zmiana X");
    }

    [Fact]
    public void AppendLineWithMonthSection_IstniejacaSekcja_DopisujeWpis()
    {
        var now = new DateTime(2026, 8, 2, 15, 0, 0);
        var existing = "=== 2026-08 ===" + Environment.NewLine + "01.08.2026 10:00 pierwszy" + Environment.NewLine;

        var result = GuestAuditLogService.AppendLineWithMonthSection(
            existing, now, "02.08.2026 15:00 drugi");

        result.Should().Contain("pierwszy");
        result.Should().Contain("drugi");
        result.Split("=== 2026-08 ===", StringSplitOptions.None).Should().HaveCount(2);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(4)]
    [InlineData(-1)]
    public async Task AppendAsync_NieprawidlowaZmiana_NicNieZapisuje(int shift)
    {
        var dir = CreateTempRoot();
        try
        {
            var sut = new GuestAuditLogService(dir);
            await sut.AppendAsync(shift, "wiadomość");

            File.Exists(sut.GetLogPath(1)).Should().BeFalse();
            File.Exists(sut.GetLogPath(2)).Should().BeFalse();
            File.Exists(sut.GetLogPath(3)).Should().BeFalse();
        }
        finally
        {
            TryDelete(dir);
        }
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task AppendAsync_PustaWiadomosc_Ignoruje(string? message)
    {
        var dir = CreateTempRoot();
        try
        {
            var sut = new GuestAuditLogService(dir);
            await sut.AppendAsync(1, message!);
            (await sut.ReadAsync(1)).Should().BeEmpty();
        }
        finally
        {
            TryDelete(dir);
        }
    }

    [Fact]
    public async Task AppendAsync_HappyPath_ZapisujeWpis()
    {
        var dir = CreateTempRoot();
        try
        {
            var sut = new GuestAuditLogService(dir);
            await sut.AppendAsync(2, "Edycja grafiku");

            var content = await sut.ReadAsync(2);
            content.Should().Contain("Edycja grafiku");
            content.Should().MatchRegex(@"===\s*\d{4}-\d{2}\s*===");
        }
        finally
        {
            TryDelete(dir);
        }
    }

    [Fact]
    public void PurgeOlderThan_UsuwaWpisyStarszeNizCutoff()
    {
        var content =
            "=== 2024-01 ===\n" +
            "15.01.2024 10:00 stary\n" +
            "=== 2026-07 ===\n" +
            "01.07.2026 10:00 nowy\n";

        var purged = GuestAuditLogService.PurgeOlderThan(
            content, new DateTime(2025, 8, 2));

        purged.Should().NotContain("stary");
        purged.Should().Contain("nowy");
        purged.Should().Contain("=== 2026-07 ===");
    }

    [Fact]
    public void PurgeOlderThan_WpisBezNaglowka_DopisujeSyntetyczny()
    {
        var content = "02.08.2026 12:00 wpis bez sekcji\n";
        var purged = GuestAuditLogService.PurgeOlderThan(
            content, new DateTime(2026, 1, 1));

        purged.Should().Contain("=== 2026-08 ===");
        purged.Should().Contain("wpis bez sekcji");
    }

    [Fact]
    public void PurgeOlderThan_PustaTresc_ZwracaPusty()
    {
        GuestAuditLogService.PurgeOlderThan("   ", DateTime.Now).Should().BeEmpty();
        GuestAuditLogService.PurgeOlderThan("", DateTime.Now).Should().BeEmpty();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(99)]
    public async Task ReadAsync_NieprawidlowaZmiana_ZwracaPusty(int shift)
    {
        var dir = CreateTempRoot();
        try
        {
            var sut = new GuestAuditLogService(dir);
            (await sut.ReadAsync(shift)).Should().BeEmpty();
        }
        finally
        {
            TryDelete(dir);
        }
    }

    private static string CreateTempRoot() =>
        Path.Combine(Path.GetTempPath(), "tukan-audit-" + Guid.NewGuid().ToString("N"));

    private static void TryDelete(string dir)
    {
        try
        {
            if (Directory.Exists(dir))
                Directory.Delete(dir, recursive: true);
        }
        catch
        {
            // katalog tymczasowy — pomijamy błędy sprzątania
        }
    }
}
