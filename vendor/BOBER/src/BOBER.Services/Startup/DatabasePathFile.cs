namespace BOBER.Services.Startup;

/// <summary>
/// Zarządza plikiem databasepath.txt przechowującym ścieżkę do ChomikDatabase.accdb.
/// Plik tworzony jest obok BOBER.exe. Domyślna ścieżka wskazuje na podfolder CHOMIK w katalogu aplikacji.
/// </summary>
public static class DatabasePathFile
{
    private static readonly string FilePath = Path.Combine(
        AppContext.BaseDirectory,
        "databasepath.txt");

    public static readonly string DefaultPath = Path.Combine(
        AppContext.BaseDirectory,
        "CHOMIK",
        "ChomikDatabase.accdb");

    /// <summary>Odczytuje ścieżkę z pliku. Zwraca null gdy plik nie istnieje lub jest pusty.</summary>
    public static string? TryRead()
    {
        if (!File.Exists(FilePath))
            return null;

        var content = File.ReadAllText(FilePath, System.Text.Encoding.UTF8).Trim();
        return string.IsNullOrWhiteSpace(content) ? null : content;
    }

    /// <summary>Zapisuje podaną ścieżkę do pliku databasepath.txt.</summary>
    public static void Write(string path) =>
        File.WriteAllText(FilePath, path, System.Text.Encoding.UTF8);

    /// <summary>Tworzy plik z domyślną ścieżką, jeśli plik jeszcze nie istnieje.</summary>
    public static void EnsureExists()
    {
        if (!File.Exists(FilePath))
            Write(DefaultPath);
    }
}
