using System.Data.Common;
using System.Data.OleDb;
using BOBER.Core.Models;

namespace BOBER.Data.Repositories;

public sealed class KoloryRepository(BoberConnectionFactory connectionFactory) : IKoloryRepository
{
    public async Task<IReadOnlyList<KolorStanowiska>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateOpenConnection();

        var hasAktywny = await HasAktywnyColumnAsync(connection, cancellationToken);
        return await ReadAllAsync(
            connection,
            hasAktywny
                ? "SELECT KluczRoli, KolorHex, Aktywny FROM KoloryStanowisk"
                : "SELECT KluczRoli, KolorHex FROM KoloryStanowisk",
            includeAktywny: hasAktywny,
            cancellationToken);
    }

    public async Task SaveAsync(
        IReadOnlyList<KolorStanowiska> kolory,
        CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateOpenConnection();
        // Sprawdzenie kolumny przed transakcją — ACE wymaga, by komendy na połączeniu
        // z otwartą transakcją miały Transaction ustawione.
        var hasAktywny = await HasAktywnyColumnAsync(connection, cancellationToken);

        // DELETE + seria INSERT w jednej transakcji: przerwanie w połowie pętli
        // nie może zostawić aplikacji bez kolorów.
        await using var transaction = connection.BeginTransaction();

        try
        {
            await using var deleteCmd = new OleDbCommand(
                "DELETE FROM KoloryStanowisk", connection, transaction);
            await deleteCmd.ExecuteNonQueryAsync(cancellationToken);

            foreach (var kolor in kolory)
            {
                if (hasAktywny)
                {
                    await using var insertCmd = new OleDbCommand(
                        "INSERT INTO KoloryStanowisk (KluczRoli, KolorHex, Aktywny) VALUES (?, ?, ?)",
                        connection,
                        transaction);
                    insertCmd.Parameters.AddWithValue("@p1", kolor.KluczRoli);
                    insertCmd.Parameters.AddWithValue("@p2", kolor.KolorHex);
                    insertCmd.Parameters.Add("@p3", OleDbType.Boolean).Value = kolor.Aktywny;
                    await insertCmd.ExecuteNonQueryAsync(cancellationToken);
                }
                else
                {
                    await using var insertCmd = new OleDbCommand(
                        "INSERT INTO KoloryStanowisk (KluczRoli, KolorHex) VALUES (?, ?)",
                        connection,
                        transaction);
                    insertCmd.Parameters.AddWithValue("@p1", kolor.KluczRoli);
                    insertCmd.Parameters.AddWithValue("@p2", kolor.KolorHex);
                    await insertCmd.ExecuteNonQueryAsync(cancellationToken);
                }
            }

            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(CancellationToken.None);
            throw;
        }
    }

    private static async Task<IReadOnlyList<KolorStanowiska>> ReadAllAsync(
        OleDbConnection connection,
        string sql,
        bool includeAktywny,
        CancellationToken cancellationToken)
    {
        await using var command = new OleDbCommand(sql, connection);
        var result = new List<KolorStanowiska>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            result.Add(new KolorStanowiska
            {
                KluczRoli = reader.GetString(0),
                KolorHex = reader.GetString(1),
                Aktywny = includeAktywny ? ReadAktywny(reader, 2) : true
            });
        }

        return result;
    }

    private static async Task<bool> HasAktywnyColumnAsync(
        OleDbConnection connection,
        CancellationToken cancellationToken)
    {
        try
        {
            await using var command = new OleDbCommand(
                "SELECT TOP 1 Aktywny FROM KoloryStanowisk",
                connection);
            await command.ExecuteScalarAsync(cancellationToken);
            return true;
        }
        catch (OleDbException)
        {
            return false;
        }
    }

    private static bool ReadAktywny(DbDataReader reader, int ordinal)
    {
        if (reader.FieldCount <= ordinal || reader.IsDBNull(ordinal))
            return true;

        var value = reader.GetValue(ordinal);
        return value switch
        {
            bool flag => flag,
            _ => Convert.ToBoolean(value)
        };
    }
}
