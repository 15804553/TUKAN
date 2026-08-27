using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace Tukan.App.Services;

/// <summary>
/// Umowna nazwa tej instalacji TUKAN (np. JRG4_PA), lokalna dla komputera.
/// Przechowywana w pliku obok EXE — niezależna od wspólnej bazy sieciowej.
/// </summary>
public static partial class InstallationNameStore
{
    public const int MaxLength = 40;
    public const string FileName = "installationname.txt";

    private static readonly string FilePath = Path.Combine(AppContext.BaseDirectory, FileName);

    /// <summary>Odczytuje nazwę. Pusty string gdy plik nie istnieje lub jest pusty.</summary>
    public static string Read()
    {
        if (!File.Exists(FilePath))
            return string.Empty;

        var content = File.ReadAllText(FilePath, Encoding.UTF8).Trim();
        return string.IsNullOrWhiteSpace(content) ? string.Empty : content;
    }

    /// <summary>
    /// Zapisuje nazwę instalacji. Pusta wartość usuwa plik.
    /// Dozwolone: litery, cyfry, podkreślnik, myślnik (np. JRG4_PA).
    /// </summary>
    public static void Write(string? name)
    {
        var normalized = Normalize(name);
        if (string.IsNullOrEmpty(normalized))
        {
            if (File.Exists(FilePath))
                File.Delete(FilePath);
            return;
        }

        File.WriteAllText(FilePath, normalized, Encoding.UTF8);
    }

    public static string Normalize(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return string.Empty;

        var trimmed = name.Trim();
        if (trimmed.Length > MaxLength)
            trimmed = trimmed[..MaxLength];

        if (!ValidNamePattern().IsMatch(trimmed))
        {
            throw new ArgumentException(
                "Nazwa instalacji może zawierać tylko litery, cyfry, podkreślnik (_) i myślnik (-), " +
                $"np. JRG4_PA (max {MaxLength} znaków).");
        }

        return trimmed;
    }

    public static bool TryNormalize(string? name, out string normalized, out string? error)
    {
        try
        {
            normalized = Normalize(name);
            error = null;
            return true;
        }
        catch (ArgumentException ex)
        {
            normalized = string.Empty;
            error = ex.Message;
            return false;
        }
    }

    [GeneratedRegex(@"^[A-Za-z0-9_-]+$", RegexOptions.CultureInvariant)]
    private static partial Regex ValidNamePattern();
}
