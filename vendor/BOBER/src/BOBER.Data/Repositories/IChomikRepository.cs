using BOBER.Core.Models;

namespace BOBER.Data.Repositories;

public interface IChomikRepository
{
    Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Funkcjonariusz>> GetByZmianaAsync(int zmianaId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<UserAccount>> GetUserAccountsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Aktualizuje kolumnę NumerPorzadkowy w tabeli Funkcjonariusze dla podanych identyfikatorów.
    /// Klucz słownika: Id funkcjonariusza, wartość: nowy numer porządkowy (1-bazowany).
    /// </summary>
    Task UpdateNrAsync(IReadOnlyDictionary<int, int> idToNr, CancellationToken cancellationToken = default);
}
