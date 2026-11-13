using System.Data;
using System.Data.OleDb;
using Tukan.App.Models;

namespace Tukan.App.Services;

/// <summary>Odczyt i zapis notatek PA w tabeli PaNotatki wspólnej bazy Access.</summary>
public sealed class PaNotatkiRepository
{
    public async Task<IReadOnlyList<PaNotatka>> PobierzWszystkieAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = await OtworzAsync(cancellationToken);
        await using var command = new OleDbCommand(
            """
            SELECT Id, DataModyfikacji, Naglowek, Tresc
            FROM PaNotatki
            ORDER BY DataModyfikacji DESC, Id DESC
            """,
            connection);

        var wynik = new List<PaNotatka>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            wynik.Add(Odczytaj(reader));
        }

        return wynik;
    }

    public async Task<int> DodajAsync(string naglowek, string tresc, CancellationToken cancellationToken = default)
    {
        var (czystyNaglowek, czystaTresc) = Normalizuj(naglowek, tresc);
        await using var connection = await OtworzAsync(cancellationToken);
        await using var command = new OleDbCommand(
            "INSERT INTO PaNotatki (DataModyfikacji, Naglowek, Tresc) VALUES (?, ?, ?)",
            connection);
        DodajDate(command, DateTime.Now);
        command.Parameters.Add(Tekst(czystyNaglowek, PaNotatka.MaksDlugoscNaglowka));
        command.Parameters.Add(Memo(czystaTresc));
        await command.ExecuteNonQueryAsync(cancellationToken);

        await using var identity = new OleDbCommand("SELECT @@IDENTITY", connection);
        var id = await identity.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt32(id);
    }

    public async Task AktualizujAsync(int id, string naglowek, string tresc, CancellationToken cancellationToken = default)
    {
        var (czystyNaglowek, czystaTresc) = Normalizuj(naglowek, tresc);
        await using var connection = await OtworzAsync(cancellationToken);
        await using var command = new OleDbCommand(
            "UPDATE PaNotatki SET DataModyfikacji = ?, Naglowek = ?, Tresc = ? WHERE Id = ?",
            connection);
        DodajDate(command, DateTime.Now);
        command.Parameters.Add(Tekst(czystyNaglowek, PaNotatka.MaksDlugoscNaglowka));
        command.Parameters.Add(Memo(czystaTresc));
        command.Parameters.AddWithValue("@id", id);

        var zmienione = await command.ExecuteNonQueryAsync(cancellationToken);
        if (zmienione == 0)
        {
            throw new InvalidOperationException("Wybrana notatka nie istnieje już w bazie.");
        }
    }

    public async Task UsunAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var connection = await OtworzAsync(cancellationToken);
        await using var command = new OleDbCommand("DELETE FROM PaNotatki WHERE Id = ?", connection);
        command.Parameters.AddWithValue("@id", id);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static (string Naglowek, string Tresc) Normalizuj(string naglowek, string tresc)
    {
        var czystyNaglowek = (naglowek ?? string.Empty).Trim();
        var czystaTresc = (tresc ?? string.Empty).Trim();

        if (czystyNaglowek.Length == 0)
        {
            throw new ArgumentException("Nagłówek notatki jest wymagany.");
        }

        if (czystyNaglowek.Length > PaNotatka.MaksDlugoscNaglowka)
        {
            throw new ArgumentException(
                $"Nagłówek może mieć najwyżej {PaNotatka.MaksDlugoscNaglowka} znaków.");
        }

        if (czystaTresc.Length > PaNotatka.MaksDlugoscTresci)
        {
            throw new ArgumentException(
                $"Treść może mieć najwyżej {PaNotatka.MaksDlugoscTresci} znaków.");
        }

        return (czystyNaglowek, czystaTresc);
    }

    private static PaNotatka Odczytaj(IDataRecord reader) => new()
    {
        Id = Convert.ToInt32(reader["Id"]),
        DataModyfikacji = Convert.ToDateTime(reader["DataModyfikacji"]),
        Naglowek = reader["Naglowek"]?.ToString() ?? string.Empty,
        Tresc = reader["Tresc"] is DBNull ? string.Empty : reader["Tresc"]?.ToString() ?? string.Empty
    };

    private static void DodajDate(OleDbCommand command, DateTime data) =>
        command.Parameters.Add(new OleDbParameter
        {
            OleDbType = OleDbType.Date,
            Value = data
        });

    private static OleDbParameter Tekst(string wartosc, int dlugosc) => new()
    {
        OleDbType = OleDbType.VarWChar,
        Size = dlugosc,
        Value = wartosc
    };

    private static OleDbParameter Memo(string wartosc) => new()
    {
        OleDbType = OleDbType.LongVarWChar,
        Value = wartosc
    };

    private static async Task<OleDbConnection> OtworzAsync(CancellationToken cancellationToken)
    {
        var connection = new OleDbConnection(
            TukanDatabaseOptions.BuildConnectionString(TukanDatabaseOptions.GetFullPath()));
        await connection.OpenAsync(cancellationToken);
        return connection;
    }
}
