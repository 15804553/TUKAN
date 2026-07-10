using System.Data.OleDb;
using BOBER.Core.Models;

namespace BOBER.Data.Repositories;

public sealed class KoloryRepository(BoberConnectionFactory connectionFactory) : IKoloryRepository
{
    public async Task<IReadOnlyList<KolorStanowiska>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateOpenConnection();
        await using var command = new OleDbCommand(
            "SELECT KluczRoli, KolorHex FROM KoloryStanowisk", connection);

        var result = new List<KolorStanowiska>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            result.Add(new KolorStanowiska
            {
                KluczRoli = reader.GetString(0),
                KolorHex = reader.GetString(1)
            });
        }

        return result;
    }

    public async Task SaveAsync(
        IReadOnlyList<KolorStanowiska> kolory,
        CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateOpenConnection();

        await using var deleteCmd = new OleDbCommand("DELETE FROM KoloryStanowisk", connection);
        await deleteCmd.ExecuteNonQueryAsync(cancellationToken);

        foreach (var kolor in kolory)
        {
            await using var insertCmd = new OleDbCommand(
                "INSERT INTO KoloryStanowisk (KluczRoli, KolorHex) VALUES (?, ?)", connection);
            insertCmd.Parameters.AddWithValue("@p1", kolor.KluczRoli);
            insertCmd.Parameters.AddWithValue("@p2", kolor.KolorHex);
            await insertCmd.ExecuteNonQueryAsync(cancellationToken);
        }
    }
}
