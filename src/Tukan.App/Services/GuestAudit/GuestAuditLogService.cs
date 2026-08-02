using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace Tukan.App.Services.GuestAudit;

/// <summary>
/// Audyt zmian Gościa: jeden plik na zmianę (ZmianaN.log), sekcje miesięczne, retencja 12 miesięcy.
/// </summary>
public sealed class GuestAuditLogService
{
    private static readonly TimeSpan Retention = TimeSpan.FromDays(365);
    private static readonly Regex EntryDateRegex = new(
        @"^(\d{2}\.\d{2}\.\d{4})\s+\d{2}:\d{2}\s+",
        RegexOptions.Compiled);
    private static readonly Regex MonthHeaderRegex = new(
        @"^===\s*(\d{4})-(\d{2})\s*===\s*$",
        RegexOptions.Compiled);

    private readonly string _auditDirectory;
    private readonly SemaphoreSlim _gate = new(1, 1);

    public GuestAuditLogService(string? baseDirectory = null)
    {
        var root = baseDirectory ?? AppContext.BaseDirectory;
        _auditDirectory = Path.Combine(root, "LOG", "Audyt");
        Directory.CreateDirectory(_auditDirectory);
    }

    public string GetLogPath(int shiftNumber) =>
        Path.Combine(_auditDirectory, $"Zmiana{shiftNumber}.log");

    public async Task AppendAsync(
        int shiftNumber,
        string message,
        CancellationToken cancellationToken = default)
    {
        if (shiftNumber is < 1 or > 3 || string.IsNullOrWhiteSpace(message))
            return;

        var now = DateTime.Now;
        var line = $"{now:dd.MM.yyyy HH:mm} {message.Trim()}";
        var path = GetLogPath(shiftNumber);

        await _gate.WaitAsync(cancellationToken);
        try
        {
            var existing = File.Exists(path)
                ? await File.ReadAllTextAsync(path, Encoding.UTF8, cancellationToken)
                : string.Empty;
            var purged = PurgeOlderThan(existing, now - Retention);
            var updated = AppendLineWithMonthSection(purged, now, line);
            await File.WriteAllTextAsync(path, updated, Encoding.UTF8, cancellationToken);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<string> ReadAsync(int shiftNumber, CancellationToken cancellationToken = default)
    {
        if (shiftNumber is < 1 or > 3)
            return string.Empty;

        var path = GetLogPath(shiftNumber);
        await _gate.WaitAsync(cancellationToken);
        try
        {
            if (!File.Exists(path))
                return string.Empty;

            var existing = await File.ReadAllTextAsync(path, Encoding.UTF8, cancellationToken);
            var purged = PurgeOlderThan(existing, DateTime.Now - Retention);
            if (!string.Equals(existing, purged, StringComparison.Ordinal))
            {
                await File.WriteAllTextAsync(path, purged, Encoding.UTF8, cancellationToken);
            }

            return purged;
        }
        finally
        {
            _gate.Release();
        }
    }

    internal static string PurgeOlderThan(string content, DateTime cutoff)
    {
        if (string.IsNullOrWhiteSpace(content))
            return string.Empty;

        var result = new StringBuilder();
        string? currentHeader = null;
        var monthHasEntries = false;

        foreach (var rawLine in content.Split(["\r\n", "\n"], StringSplitOptions.None))
        {
            var line = rawLine;
            var headerMatch = MonthHeaderRegex.Match(line);
            if (headerMatch.Success)
            {
                FlushHeader(ref currentHeader, ref monthHasEntries);
                currentHeader = line;
                monthHasEntries = false;
                continue;
            }

            if (string.IsNullOrWhiteSpace(line))
                continue;

            var entryMatch = EntryDateRegex.Match(line);
            if (!entryMatch.Success)
                continue;

            if (!DateTime.TryParseExact(
                    entryMatch.Groups[1].Value,
                    "dd.MM.yyyy",
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out var entryDate))
            {
                continue;
            }

            if (entryDate.Date < cutoff.Date)
                continue;

            if (currentHeader is not null && !monthHasEntries)
            {
                if (result.Length > 0)
                    result.AppendLine();
                result.AppendLine(currentHeader);
                monthHasEntries = true;
            }
            else if (currentHeader is null)
            {
                // Wpis bez nagłówka — dopisz nagłówek z daty wpisu.
                var synthetic = $"=== {entryDate:yyyy-MM} ===";
                if (result.Length > 0)
                    result.AppendLine();
                result.AppendLine(synthetic);
                currentHeader = synthetic;
                monthHasEntries = true;
            }

            result.AppendLine(line);
        }

        return result.ToString().TrimEnd() + (result.Length > 0 ? Environment.NewLine : string.Empty);
    }

    private static void FlushHeader(ref string? currentHeader, ref bool monthHasEntries)
    {
        currentHeader = null;
        monthHasEntries = false;
    }

    internal static string AppendLineWithMonthSection(string content, DateTime now, string line)
    {
        var header = $"=== {now:yyyy-MM} ===";
        var trimmed = content.TrimEnd();
        if (string.IsNullOrEmpty(trimmed))
        {
            return header + Environment.NewLine + line + Environment.NewLine;
        }

        if (trimmed.Contains(header, StringComparison.Ordinal))
        {
            return trimmed + Environment.NewLine + line + Environment.NewLine;
        }

        return trimmed + Environment.NewLine + Environment.NewLine + header + Environment.NewLine + line + Environment.NewLine;
    }
}
