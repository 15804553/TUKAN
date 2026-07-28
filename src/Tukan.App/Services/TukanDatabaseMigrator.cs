using System.IO;
using System.Text;
using Chomik.Data;
using SKRYBEK.Core.Configuration;
using SKRYBEK.Services.Logging;

namespace Tukan.App.Services;

/// <summary>
/// Wyszukuje dane ze standalone CHOMIK / BOBER / SKRYBEK i scala je w jeden plik TukanDatabase.accdb.
/// </summary>
public static class TukanDatabaseMigrator
{
    private const long LikelyEmptyDatabaseBytes = 320_000;
    private static readonly HashSet<string> IgnoredDirectoryNames = new(StringComparer.OrdinalIgnoreCase)
    {
        ".git",
        ".vs",
        "obj",
        "node_modules",
        "Config.Msi",
        "System Volume Information",
        "$Recycle.Bin",
        "Windows",
        "Program Files",
        "Program Files (x86)"
    };

    public sealed record MigrationEntry(string FileName, string SourcePath, string TargetPath);

    public sealed record MigrationResult(IReadOnlyList<MigrationEntry> Migrated)
    {
        public bool Any => Migrated.Count > 0;
    }

    public static async Task<MigrationResult> MigrateAllAsync(
        string targetDirectory,
        CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(targetDirectory);

        var migrated = new List<MigrationEntry>();
        var unifiedPath = Path.Combine(targetDirectory, TukanDatabaseOptions.FileName);

        // Warm start: wspólna baza już istnieje — bez rekurencyjnego skanu dysku.
        if (IsReadyUnifiedDatabase(unifiedPath))
        {
            var localChomik = ResolveLocalLegacy(targetDirectory, "ChomikDatabase.accdb", unifiedPath);
            var localBober = ResolveLocalLegacy(targetDirectory, "BoberDatabase.accdb", unifiedPath);
            var localSkrybek = ResolveLocalLegacy(targetDirectory, "SkrybekDatabase.accdb", unifiedPath);

            if (localChomik is null && localBober is null && localSkrybek is null)
            {
                UpdateUnifiedConfigFiles(targetDirectory, unifiedPath);
                MigrateTextDictionary(targetDirectory, "Stopnie.txt", [targetDirectory], migrated);
                MigrateTextDictionary(targetDirectory, "Stanowiska.txt", [targetDirectory], migrated);
                return new MigrationResult(migrated);
            }

            var localConsolidation = await TukanDatabaseConsolidator.ConsolidateAsync(
                targetDirectory,
                localChomik,
                localBober,
                localSkrybek,
                cancellationToken);

            foreach (var action in localConsolidation.Actions)
            {
                migrated.Add(new MigrationEntry(action.Description, action.SourcePath, localConsolidation.UnifiedPath));
            }

            MigrateTextDictionary(targetDirectory, "Stopnie.txt", [targetDirectory], migrated);
            MigrateTextDictionary(targetDirectory, "Stanowiska.txt", [targetDirectory], migrated);
            UpdateUnifiedConfigFiles(targetDirectory, localConsolidation.UnifiedPath);
            WriteMigrationJournal(targetDirectory, migrated, localConsolidation.UnifiedPath);
            return new MigrationResult(migrated);
        }

        var chomikCandidates = CollectChomikCandidates(targetDirectory).ToList();
        var boberCandidates = CollectBoberCandidates(targetDirectory).ToList();
        var skrybekCandidates = CollectSkrybekCandidates(targetDirectory).ToList();

        var chomikSource = FindBestDatabaseSource("ChomikDatabase.accdb", chomikCandidates, unifiedPath);
        var boberSource = FindBestDatabaseSource("BoberDatabase.accdb", boberCandidates, unifiedPath);
        var skrybekSource = FindBestDatabaseSource("SkrybekDatabase.accdb", skrybekCandidates, unifiedPath);

        var consolidation = await TukanDatabaseConsolidator.ConsolidateAsync(
            targetDirectory,
            chomikSource,
            boberSource,
            skrybekSource,
            cancellationToken);

        foreach (var action in consolidation.Actions)
        {
            migrated.Add(new MigrationEntry(action.Description, action.SourcePath, consolidation.UnifiedPath));
        }

        MigrateTextDictionary(targetDirectory, "Stopnie.txt", chomikCandidates, migrated);
        MigrateTextDictionary(targetDirectory, "Stanowiska.txt", chomikCandidates, migrated);

        UpdateUnifiedConfigFiles(targetDirectory, consolidation.UnifiedPath);

        foreach (var entry in migrated)
        {
            SkrybekLog.Info($"TUKAN migracja: {entry.FileName} ← {entry.SourcePath}");
        }

        WriteMigrationJournal(targetDirectory, migrated, consolidation.UnifiedPath);

        return new MigrationResult(migrated);
    }

    private static bool IsReadyUnifiedDatabase(string unifiedPath)
    {
        if (!File.Exists(unifiedPath))
        {
            return false;
        }

        try
        {
            return new FileInfo(unifiedPath).Length >= LikelyEmptyDatabaseBytes;
        }
        catch (IOException)
        {
            return false;
        }
    }

    private static string? ResolveLocalLegacy(string targetDirectory, string fileName, string unifiedPath)
    {
        var candidate = Path.Combine(targetDirectory, fileName);
        if (!File.Exists(candidate) || PathsEqual(candidate, unifiedPath))
        {
            return null;
        }

        return candidate;
    }

    private static string? FindBestDatabaseSource(
        string fileName,
        IReadOnlyList<string> sourceRoots,
        string unifiedPath) =>
        FindBestSource(fileName, sourceRoots, unifiedPath);

    private static void MigrateTextDictionary(
        string targetDirectory,
        string fileName,
        IReadOnlyList<string> sourceRoots,
        List<MigrationEntry> migrated)
    {
        var targetPath = Path.Combine(targetDirectory, fileName);
        if (File.Exists(targetPath))
        {
            return;
        }

        foreach (var root in sourceRoots)
        {
            if (string.IsNullOrWhiteSpace(root))
            {
                continue;
            }

            var candidate = Path.Combine(root, fileName);
            if (!File.Exists(candidate))
            {
                continue;
            }

            File.Copy(candidate, targetPath);
            migrated.Add(new MigrationEntry(fileName, candidate, targetPath));
            return;
        }
    }

    private static void UpdateUnifiedConfigFiles(string targetDirectory, string unifiedPath)
    {
        File.WriteAllText(
            Path.Combine(targetDirectory, "databasepath.txt"),
            unifiedPath,
            Encoding.UTF8);

        var patchContent = new StringBuilder()
            .AppendLine("# TUKAN — jedna wspólna baza danych dla CHOMIK, BOBER i SKRYBEK.")
            .AppendLine($"{DatabasePatch.ChomikKey}={unifiedPath}")
            .AppendLine($"{DatabasePatch.BoberKey}={unifiedPath}")
            .ToString();

        File.WriteAllText(
            Path.Combine(targetDirectory, DatabasePatch.FileName),
            patchContent,
            Encoding.UTF8);
    }

    private static void WriteMigrationJournal(
        string targetDirectory,
        IReadOnlyList<MigrationEntry> migrated,
        string unifiedPath)
    {
        if (migrated.Count == 0)
        {
            return;
        }

        var journalPath = Path.Combine(targetDirectory, "TukanMigration.log");
        var lines = new List<string>
        {
            $"# Migracja TUKAN — {DateTime.Now:yyyy-MM-dd HH:mm:ss}",
            $"# Wspólna baza: {unifiedPath}",
            string.Empty
        };

        foreach (var entry in migrated)
        {
            lines.Add(entry.FileName);
            lines.Add($"  źródło: {entry.SourcePath}");
            lines.Add($"  baza:   {entry.TargetPath}");
            lines.Add(string.Empty);
        }

        File.WriteAllLines(journalPath, lines, Encoding.UTF8);
    }

    private static string? FindBestSource(
        string fileName,
        IReadOnlyList<string> sourceRoots,
        string targetPath)
    {
        string? best = null;
        long bestSize = -1;

        foreach (var root in sourceRoots)
        {
            if (string.IsNullOrWhiteSpace(root))
            {
                continue;
            }

            var candidate = Path.IsPathRooted(root) && root.EndsWith(".accdb", StringComparison.OrdinalIgnoreCase)
                ? root
                : Path.Combine(root, fileName);

            if (!File.Exists(candidate))
            {
                continue;
            }

            if (PathsEqual(candidate, targetPath))
            {
                continue;
            }

            var size = new FileInfo(candidate).Length;
            if (size > bestSize)
            {
                bestSize = size;
                best = candidate;
            }
        }

        return best;
    }

    private static bool ShouldCopyToTarget(string targetPath, string sourcePath)
    {
        if (!File.Exists(targetPath))
        {
            return true;
        }

        var targetInfo = new FileInfo(targetPath);
        var sourceInfo = new FileInfo(sourcePath);

        if (PathsEqual(targetPath, sourcePath))
        {
            return false;
        }

        if (targetInfo.Length >= LikelyEmptyDatabaseBytes
            && targetInfo.Length >= sourceInfo.Length * 0.9)
        {
            return false;
        }

        return sourceInfo.Length > targetInfo.Length;
    }

    private static void BackupExistingTargetIfNeeded(string targetPath)
    {
        if (!File.Exists(targetPath))
        {
            return;
        }

        var backupPath = targetPath + $".before-migration-{DateTime.Now:yyyyMMdd-HHmmss}.bak";
        File.Copy(targetPath, backupPath, overwrite: false);
        SkrybekLog.Info($"TUKAN migracja: kopia zapasowa {backupPath}");
    }

    private static IEnumerable<string> CollectChomikCandidates(string targetDirectory)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var path in EnumerateKnownInstallRoots())
        {
            AddCandidate(seen, path);
            AddCandidate(seen, Path.Combine(path, "Data"));
            AddCandidate(seen, Path.Combine(path, "CHOMIK"));
        }

        foreach (var path in ReadDatabasePathFromLegacyConfigs())
        {
            AddCandidate(seen, Path.GetDirectoryName(path));
            AddCandidate(seen, path);
        }

        foreach (var path in ReadPathsFromDatabasePatchFiles(DatabasePatch.ChomikKey))
        {
            AddCandidate(seen, Path.GetDirectoryName(path));
            AddCandidate(seen, path);
        }

        AddCandidate(seen, Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) is var la
            ? Path.Combine(la, "CHOMIK")
            : null);

        foreach (var path in FindDatabaseFilesInTree(DatabaseOptions.DatabaseFileName, targetDirectory))
        {
            AddCandidate(seen, Path.GetDirectoryName(path));
            AddCandidate(seen, path);
        }

        return seen;
    }

    private static IEnumerable<string> CollectBoberCandidates(string targetDirectory)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var path in EnumerateKnownInstallRoots())
        {
            AddCandidate(seen, path);
        }

        AddCandidate(seen, Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) is var la
            ? Path.Combine(la, "BOBER")
            : null);

        foreach (var path in ReadPathsFromDatabasePatchFiles(DatabasePatch.BoberKey))
        {
            AddCandidate(seen, Path.GetDirectoryName(path));
            AddCandidate(seen, path);
        }

        foreach (var path in FindDatabaseFilesInTree("BoberDatabase.accdb", targetDirectory))
        {
            AddCandidate(seen, Path.GetDirectoryName(path));
            AddCandidate(seen, path);
        }

        foreach (var path in FindDatabaseFilesInTree("SkrybekDatabase.accdb", targetDirectory))
        {
            AddCandidate(seen, Path.GetDirectoryName(path));
        }

        return seen;
    }

    private static IEnumerable<string> CollectSkrybekCandidates(string targetDirectory)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var path in EnumerateKnownInstallRoots())
        {
            AddCandidate(seen, path);
        }

        AddCandidate(seen, Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) is var la
            ? Path.Combine(la, "SKRYBEK")
            : null);

        foreach (var path in FindDatabaseFilesInTree("SkrybekDatabase.accdb", targetDirectory))
        {
            AddCandidate(seen, Path.GetDirectoryName(path));
            AddCandidate(seen, path);
        }

        return seen;
    }

    private static IEnumerable<string> EnumerateKnownInstallRoots()
    {
        yield return AppContext.BaseDirectory;

        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        if (!string.IsNullOrWhiteSpace(localAppData))
        {
            yield return Path.Combine(localAppData, "CHOMIK");
            yield return Path.Combine(localAppData, "BOBER");
            yield return Path.Combine(localAppData, "SKRYBEK");
        }

        foreach (var parent in EnumerateParentDirectories(AppContext.BaseDirectory, maxDepth: 6))
        {
            foreach (var appFolder in new[] { "CHOMIK", "BOBER", "SKRYBEK", "TUKAN" })
            {
                var candidate = Path.Combine(parent, appFolder);
                if (Directory.Exists(candidate))
                {
                    yield return candidate;

                    foreach (var sub in new[] { "artifacts", "publish", "win-x86", "win-x64" })
                    {
                        var nested = Path.Combine(candidate, "artifacts", "publish", sub);
                        if (Directory.Exists(nested))
                        {
                            yield return nested;
                        }
                    }

                    var binDebug = Path.Combine(candidate, "src", appFolder + ".App", "bin", "Debug", "net10.0-windows");
                    if (Directory.Exists(binDebug))
                    {
                        yield return binDebug;
                    }

                    var binRelease = Path.Combine(candidate, "src", appFolder + ".App", "bin", "Release", "net10.0-windows", "win-x86");
                    if (Directory.Exists(binRelease))
                    {
                        yield return binRelease;
                    }
                }
            }
        }
    }

    private static IEnumerable<string> EnumerateParentDirectories(string startPath, int maxDepth)
    {
        var current = Directory.GetParent(startPath);
        for (var depth = 0; depth < maxDepth && current is not null; depth++)
        {
            yield return current.FullName;
            current = current.Parent;
        }
    }

    private static IEnumerable<string> ReadDatabasePathFromLegacyConfigs()
    {
        foreach (var root in EnumerateKnownInstallRoots())
        {
            var configPath = Path.Combine(root, "databasepath.txt");
            if (!File.Exists(configPath))
            {
                continue;
            }

            var line = File.ReadAllText(configPath, Encoding.UTF8).Trim();
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            var expanded = Environment.ExpandEnvironmentVariables(line);
            var full = Path.IsPathRooted(expanded)
                ? expanded
                : Path.GetFullPath(Path.Combine(root, expanded));

            if (File.Exists(full))
            {
                yield return full;
            }
        }
    }

    private static IEnumerable<string> ReadPathsFromDatabasePatchFiles(string key)
    {
        foreach (var root in EnumerateKnownInstallRoots())
        {
            foreach (var fileName in new[] { DatabasePatch.FileName, Path.Combine("Config", DatabasePatch.FileName) })
            {
                var patchPath = Path.Combine(root, fileName);
                if (!File.Exists(patchPath))
                {
                    continue;
                }

                foreach (var line in File.ReadAllLines(patchPath, Encoding.UTF8))
                {
                    var trimmed = line.Trim();
                    if (trimmed.Length == 0 || trimmed.StartsWith('#'))
                    {
                        continue;
                    }

                    var separator = trimmed.IndexOf('=');
                    if (separator <= 0)
                    {
                        continue;
                    }

                    var patchKey = trimmed[..separator].Trim();
                    if (!patchKey.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    var value = trimmed[(separator + 1)..].Trim();
                    if (string.IsNullOrWhiteSpace(value))
                    {
                        continue;
                    }

                    var resolved = DatabasePatch.ResolveDatabasePath(value);
                    if (!string.IsNullOrWhiteSpace(resolved))
                    {
                        yield return resolved;
                    }
                }
            }
        }
    }

    private static IEnumerable<string> FindDatabaseFilesInTree(string fileName, string excludeDirectory)
    {
        foreach (var root in EnumerateParentDirectories(AppContext.BaseDirectory, maxDepth: 5))
        {
            if (PathsEqual(root, excludeDirectory))
            {
                continue;
            }

            foreach (var match in SafeEnumerateFiles(root, fileName))
            {
                if (match.Contains("\\obj\\", StringComparison.OrdinalIgnoreCase)
                    || match.Contains("\\.git\\", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                yield return match;
            }
        }
    }

    private static IEnumerable<string> SafeEnumerateFiles(string root, string fileName)
    {
        var pending = new Stack<string>();
        pending.Push(root);

        while (pending.Count > 0)
        {
            var current = pending.Pop();
            if (ShouldSkipDirectory(current))
            {
                continue;
            }

            IEnumerable<string> files;
            try
            {
                files = Directory.EnumerateFiles(current, fileName, SearchOption.TopDirectoryOnly);
            }
            catch (UnauthorizedAccessException)
            {
                continue;
            }
            catch (DirectoryNotFoundException)
            {
                continue;
            }
            catch (IOException)
            {
                continue;
            }

            foreach (var file in files)
            {
                yield return file;
            }

            IEnumerable<string> directories;
            try
            {
                directories = Directory.EnumerateDirectories(current, "*", SearchOption.TopDirectoryOnly);
            }
            catch (UnauthorizedAccessException)
            {
                continue;
            }
            catch (DirectoryNotFoundException)
            {
                continue;
            }
            catch (IOException)
            {
                continue;
            }

            foreach (var directory in directories)
            {
                if (!ShouldSkipDirectory(directory))
                {
                    pending.Push(directory);
                }
            }
        }
    }

    private static bool ShouldSkipDirectory(string path)
    {
        var directoryName = Path.GetFileName(path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
        return IgnoredDirectoryNames.Contains(directoryName);
    }

    private static void AddCandidate(HashSet<string> seen, string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        try
        {
            var full = Path.GetFullPath(path);
            seen.Add(full);
        }
        catch (IOException)
        {
            seen.Add(path);
        }
    }

    private static bool PathsEqual(string a, string b) =>
        string.Equals(
            Path.GetFullPath(a),
            Path.GetFullPath(b),
            StringComparison.OrdinalIgnoreCase);
}
