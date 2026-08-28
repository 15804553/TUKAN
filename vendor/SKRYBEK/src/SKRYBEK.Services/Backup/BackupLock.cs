using SKRYBEK.Services.Logging;

namespace SKRYBEK.Services.Backup;

/// <summary>
/// Blokada plikowa na wspólnym katalogu backupu — zapobiega równoległemu backupowi z wielu PC.
/// </summary>
internal sealed class BackupLock : IDisposable
{
    public const string LockFileName = "TukanBackup.lock";

    private static readonly TimeSpan StaleLockAge = TimeSpan.FromMinutes(15);
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(2);

    private readonly string _lockPath;
    private FileStream? _stream;

    private BackupLock(string lockPath, FileStream stream)
    {
        _lockPath = lockPath;
        _stream = stream;
    }

    public static async Task<BackupLock?> TryAcquireAsync(
        string lockPath,
        TimeSpan maxWait,
        CancellationToken cancellationToken = default)
    {
        var deadline = DateTime.UtcNow + maxWait;

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var acquired = TryAcquireOnce(lockPath);
            if (acquired is not null)
                return acquired;

            if (DateTime.UtcNow >= deadline)
                return null;

            await Task.Delay(PollInterval, cancellationToken);
        }
    }

    private static BackupLock? TryAcquireOnce(string lockPath)
    {
        try
        {
            var stream = new FileStream(
                lockPath,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None,
                bufferSize: 256,
                FileOptions.DeleteOnClose);

            var payload =
                $"TUKAN backup lock{Environment.NewLine}" +
                $"Machine: {Environment.MachineName}{Environment.NewLine}" +
                $"Process: {Environment.ProcessId}{Environment.NewLine}" +
                $"StartedUtc: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}{Environment.NewLine}";

            using var writer = new StreamWriter(stream, leaveOpen: true) { AutoFlush = true };
            writer.Write(payload);
            stream.Flush(flushToDisk: true);

            return new BackupLock(lockPath, stream);
        }
        catch (IOException)
        {
            if (!TryRemoveStaleLock(lockPath))
                return null;

            return TryAcquireOnce(lockPath);
        }
    }

    private static bool TryRemoveStaleLock(string lockPath)
    {
        try
        {
            if (!File.Exists(lockPath))
                return false;

            var age = DateTime.UtcNow - File.GetLastWriteTimeUtc(lockPath);
            if (age <= StaleLockAge)
                return false;

            File.Delete(lockPath);
            SkrybekLog.Warning($"Usunięto przeterminowaną blokadę backupu: {lockPath}");
            return true;
        }
        catch (Exception ex)
        {
            SkrybekLog.Warning($"Nie udało się usunąć blokady backupu {lockPath}: {ex.Message}");
            return false;
        }
    }

    public void Dispose()
    {
        try
        {
            _stream?.Dispose();
        }
        catch
        {
            // DeleteOnClose — plik znika przy dispose strumienia.
        }
        finally
        {
            _stream = null;
        }

        try
        {
            if (File.Exists(_lockPath))
                File.Delete(_lockPath);
        }
        catch
        {
            // Inny proces mógł już zwolnić blokadę.
        }
    }
}
