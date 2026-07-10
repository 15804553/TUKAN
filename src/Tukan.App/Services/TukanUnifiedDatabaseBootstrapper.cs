using System.IO;
using BOBER.Data;
using BOBER.Data.Database;
using Chomik.Data;
using Chomik.Data.Database;
using SKRYBEK.Data.Connections;
using BoberDatabaseBootstrapper = BOBER.Data.Database.DatabaseBootstrapper;
using ChomikDatabaseBootstrapper = Chomik.Data.Database.DatabaseBootstrapper;
using SkrybekDatabaseBootstrapper = SKRYBEK.Data.Database.DatabaseBootstrapper;

namespace Tukan.App.Services;

/// <summary>Tworzy pełny schemat trzech modułów w jednym pliku .accdb.</summary>
public static class TukanUnifiedDatabaseBootstrapper
{
    public static async Task EnsureSchemaAsync(string unifiedPath, CancellationToken cancellationToken = default)
    {
        var directory = Path.GetDirectoryName(unifiedPath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var chomikOptions = new DatabaseOptions
        {
            FilePath = unifiedPath,
            DatabasePassword = TukanDatabaseOptions.Password,
            UseDatabasePassword = true
        };

        var chomikBootstrapper = new ChomikDatabaseBootstrapper(chomikOptions);
        await chomikBootstrapper.EnsureReadyAsync(cancellationToken);

        var boberOptions = new BoberDatabaseOptions
        {
            FilePath = unifiedPath,
            DatabasePassword = TukanDatabaseOptions.Password,
            UseDatabasePassword = true
        };

        var boberBootstrapper = new BoberDatabaseBootstrapper(boberOptions);
        await boberBootstrapper.EnsureReadyAsync(cancellationToken);

        var skrybekFactory = new SkrybekConnectionFactory(unifiedPath);
        var skrybekBootstrapper = new SkrybekDatabaseBootstrapper(skrybekFactory);
        await skrybekBootstrapper.EnsureCreatedAsync();
    }
}
