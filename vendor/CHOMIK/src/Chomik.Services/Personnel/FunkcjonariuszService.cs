using Chomik.Core;
using Chomik.Core.Models;
using Chomik.Core.Security;
using Chomik.Data.Repositories;

namespace Chomik.Services.Personnel;

public sealed class FunkcjonariuszService(
    IFunkcjonariuszRepository repository,
    ISlownikRepository slownikRepository) : IFunkcjonariuszService
{
    private PersonnelDictionaries? _dictionariesCache;

    public async Task<IReadOnlyList<FunkcjonariuszListItem>> GetFilteredAsync(
        SessionUser user,
        FunkcjonariuszFilter filter,
        CancellationToken cancellationToken = default)
    {
        if (!user.CanViewGeneralView)
        {
            throw new UnauthorizedAccessException("Brak dostępu do widoku personelu.");
        }

        var listQuery = BuildListQuery(user, filter);
        var bundle = await repository.LoadGeneralViewBundleAsync(
            listQuery,
            user.CanViewSensitiveData,
            cancellationToken);

        return bundle.Personnel
            .Select(f =>
            {
                bundle.UprawnieniaByPersonId.TryGetValue(f.Id, out var uprawnienia);
                uprawnienia ??= [];
                return new FunkcjonariuszListItem
                {
                    Entity = f,
                    Uprawnienia = uprawnienia,
                    ShowSensitiveFields = user.CanViewSensitiveData && user.CanAccessShift(f.NumerZmiany),
                    UprawnieniaSkrot = BuildUprawnieniaSkrot(uprawnienia),
                    OdznaczeniaSkrot = user.CanViewSensitiveData && user.CanAccessShift(f.NumerZmiany)
                        ? BuildOdznaczeniaSkrot(f)
                        : string.Empty
                };
            })
            .ToList();
    }

    public async Task<IReadOnlyList<Funkcjonariusz>> GetEditableAsync(
        SessionUser user,
        CancellationToken cancellationToken = default)
    {
        if (!user.CanEditPersonnel || user.ShiftNumber is not int shift)
        {
            return [];
        }

        var all = await repository.GetAllAsync(cancellationToken: cancellationToken);
        return all
            .Where(f => f.NumerZmiany == shift)
            .OrderBy(f => f.NumerPorzadkowy)
            .ThenBy(f => f.Id)
            .ToList();
    }

    public async Task<int> GetNextNumerPorzadkowyAsync(
        SessionUser user,
        CancellationToken cancellationToken = default)
    {
        if (!user.CanEditPersonnel || user.ShiftNumber is not int shift)
        {
            throw new UnauthorizedAccessException("Brak uprawnień do dodawania funkcjonariusza.");
        }

        return await repository.GetNextNumerPorzadkowyAsync(shift, cancellationToken);
    }

    public async Task<Funkcjonariusz?> GetByIdForEditAsync(
        SessionUser user,
        int id,
        CancellationToken cancellationToken = default)
    {
        var entity = await repository.GetByIdAsync(id, cancellationToken);
        if (entity is null || !CanEditEntity(user, entity))
        {
            return null;
        }

        return entity;
    }

    public async Task<Funkcjonariusz?> GetForGeneralViewProfileAsync(
        SessionUser user,
        int id,
        CancellationToken cancellationToken = default)
    {
        if (!user.IsDcaJrgUser)
        {
            throw new UnauthorizedAccessException("Brak uprawnień do podglądu profilu funkcjonariusza.");
        }

        var entity = await repository.GetByIdAsync(id, cancellationToken);
        if (entity is null || !user.CanAccessShift(entity.NumerZmiany))
        {
            return null;
        }

        return entity;
    }

    public async Task<int> SaveAsync(
        SessionUser user,
        Funkcjonariusz entity,
        IReadOnlyList<int> typyUprawnienIds,
        IReadOnlyDictionary<int, DateTime?> datyWaznosci,
        IReadOnlyDictionary<int, DateTime> datyNadaniaOdznaczen,
        CancellationToken cancellationToken = default)
    {
        if (!CanEditEntity(user, entity))
        {
            throw new UnauthorizedAccessException("Brak uprawnień do edycji tego funkcjonariusza.");
        }

        await ValidateNumerPorzadkowyAsync(entity, cancellationToken);

        if (entity.Id == 0)
        {
            entity.Id = await repository.InsertAsync(entity, cancellationToken);
        }
        else
        {
            await repository.UpdateAsync(entity, cancellationToken);
        }

        await repository.ReplaceUprawnieniaAsync(entity.Id, typyUprawnienIds, datyWaznosci, cancellationToken);
        await repository.ReplaceOdznaczeniaAsync(entity.Id, datyNadaniaOdznaczen, cancellationToken);
        return entity.Id;
    }

    public async Task DeleteAsync(SessionUser user, int id, CancellationToken cancellationToken = default)
    {
        var entity = await repository.GetByIdAsync(id, cancellationToken);
        if (entity is null || !CanEditEntity(user, entity))
        {
            throw new UnauthorizedAccessException("Brak uprawnień do usunięcia.");
        }

        await repository.DeleteAsync(id, cancellationToken);
    }

    public async Task SaveGeneralViewNumerZmianyAsync(
        SessionUser user,
        int funkcjonariuszId,
        int numerZmiany,
        CancellationToken cancellationToken = default)
    {
        if (!user.CanEditGeneralViewShift)
        {
            throw new UnauthorizedAccessException("Brak uprawnień do zmiany numeru zmiany.");
        }

        if (numerZmiany is < 1 or > 3)
        {
            throw new InvalidOperationException("Numer zmiany musi być w zakresie 1–3.");
        }

        var entity = await repository.GetByIdAsync(funkcjonariuszId, cancellationToken)
            ?? throw new InvalidOperationException("Nie znaleziono funkcjonariusza.");

        await repository.UpdateNumerZmianyAsync(funkcjonariuszId, numerZmiany, cancellationToken);
    }

    public async Task SaveGeneralViewStopienAsync(
        SessionUser user,
        int funkcjonariuszId,
        int stopienId,
        CancellationToken cancellationToken = default)
    {
        if (!user.CanEditGeneralViewStopien)
        {
            throw new UnauthorizedAccessException("Brak uprawnień do zmiany stopnia.");
        }

        if (stopienId <= 0)
        {
            throw new InvalidOperationException("Wybierz prawidłowy stopień.");
        }

        var entity = await repository.GetByIdAsync(funkcjonariuszId, cancellationToken)
            ?? throw new InvalidOperationException("Nie znaleziono funkcjonariusza.");

        if (!user.CanAccessShift(entity.NumerZmiany))
        {
            throw new UnauthorizedAccessException("Brak uprawnień do edycji tego funkcjonariusza.");
        }

        await repository.UpdateStopienIdAsync(funkcjonariuszId, stopienId, cancellationToken);
    }

    public async Task SaveGeneralViewTerminyMedyczneAsync(
        SessionUser user,
        int funkcjonariuszId,
        DateTime? badaniaOkresoweDo,
        DateTime? komoraDymowaDo,
        DateTime? kppDo,
        CancellationToken cancellationToken = default)
    {
        EnsureCanEditGeneralViewDates(user);
        var entity = await repository.GetByIdAsync(funkcjonariuszId, cancellationToken)
            ?? throw new InvalidOperationException("Nie znaleziono funkcjonariusza.");

        if (!user.CanAccessShift(entity.NumerZmiany))
        {
            throw new UnauthorizedAccessException("Brak uprawnień do edycji tego funkcjonariusza.");
        }

        await repository.UpdateTerminyMedyczneAsync(
            funkcjonariuszId,
            badaniaOkresoweDo,
            komoraDymowaDo,
            kppDo,
            cancellationToken);
    }

    public async Task SaveGeneralViewUprawnienieWazneDoAsync(
        SessionUser user,
        int uprawnieniePrzypisanieId,
        DateTime? wazneDo,
        CancellationToken cancellationToken = default)
    {
        EnsureCanEditGeneralViewDates(user);
        var entity = await FindFunkcjonariuszByUprawnienieAsync(uprawnieniePrzypisanieId, cancellationToken)
            ?? throw new InvalidOperationException("Nie znaleziono uprawnienia.");

        if (!user.CanAccessShift(entity.NumerZmiany))
        {
            throw new UnauthorizedAccessException("Brak uprawnień do edycji tego funkcjonariusza.");
        }

        await repository.UpdateUprawnienieWazneDoAsync(uprawnieniePrzypisanieId, wazneDo, cancellationToken);
    }

    public async Task<PersonnelDictionaries> GetDictionariesAsync(CancellationToken cancellationToken = default)
    {
        if (_dictionariesCache is not null)
        {
            return _dictionariesCache;
        }

        _dictionariesCache = new PersonnelDictionaries
        {
            Stopnie = await slownikRepository.GetStopnieAsync(cancellationToken),
            Stanowiska = await slownikRepository.GetStanowiskaAsync(cancellationToken),
            TypyUprawnien = await slownikRepository.GetTypyUprawnienAsync(cancellationToken),
            TypyOdznaczen = await slownikRepository.GetTypyOdznaczenAsync(cancellationToken)
        };

        return _dictionariesCache;
    }

    public void InvalidateDictionariesCache() => _dictionariesCache = null;

    public async Task<IReadOnlyList<string>> GetPersonnelNamesForExportAsync(
        SessionUser user,
        int? numerZmiany,
        CancellationToken cancellationToken = default)
    {
        int? exportShift;
        if (user.IsShiftScoped)
        {
            if (user.ShiftNumber is not int ownShift)
            {
                throw new InvalidOperationException("Brak przypisanej zmiany użytkownika.");
            }

            exportShift = ownShift;
        }
        else if (user.IsPaUser)
        {
            exportShift = numerZmiany;
        }
        else
        {
            throw new UnauthorizedAccessException("Brak uprawnień do eksportu listy osób.");
        }

        var query = new FunkcjonariuszListQuery
        {
            NumerZmiany = exportShift
        };

        return await repository.GetPersonnelFullNamesAsync(query, cancellationToken);
    }

    private static FunkcjonariuszListQuery BuildListQuery(
        SessionUser user,
        FunkcjonariuszFilter filter)
    {
        int? shift = null;
        if (user.IsShiftScoped)
        {
            shift = user.ShiftNumber;
        }
        else if (filter.NumerZmiany is int filteredShift)
        {
            shift = filteredShift;
        }

        return new FunkcjonariuszListQuery
        {
            NumerZmiany = shift,
            SearchTerm = filter.Szukaj,
            UprawnienieNazwa = filter.UprawnienieNazwa,
            UprawnieniePodtyp = filter.UprawnieniePodtyp
        };
    }

    private async Task ValidateNumerPorzadkowyAsync(
        Funkcjonariusz entity,
        CancellationToken cancellationToken)
    {
        if (entity.NumerPorzadkowy < 1)
        {
            throw new InvalidOperationException("Numer na liście musi być liczbą większą od zera.");
        }

        if (await repository.IsNumerPorzadkowyTakenAsync(
                entity.NumerZmiany,
                entity.NumerPorzadkowy,
                entity.Id,
                cancellationToken))
        {
            throw new InvalidOperationException(
                $"Numer {entity.NumerPorzadkowy} jest już przypisany do innej osoby w tej zmianie.");
        }
    }

    private static bool CanEditEntity(SessionUser user, Funkcjonariusz entity) =>
        user.CanEditPersonnel && user.ShiftNumber == entity.NumerZmiany;

    private static void EnsureCanEditGeneralViewDates(SessionUser user)
    {
        if (!user.CanEditGeneralViewDates)
        {
            throw new UnauthorizedAccessException("Brak uprawnień do edycji dat w widoku ogólnym.");
        }
    }

    private async Task<Funkcjonariusz?> FindFunkcjonariuszByUprawnienieAsync(
        int uprawnieniePrzypisanieId,
        CancellationToken cancellationToken)
    {
        var all = await repository.GetAllAsync(
            FunkcjonariuszLoadOptions.ForGeneralView(includeSensitiveRelations: false),
            cancellationToken);
        return all.FirstOrDefault(f => f.Uprawnienia.Any(u => u.Id == uprawnieniePrzypisanieId));
    }

    private static string BuildUprawnieniaSkrot(IReadOnlyList<UprawnieniePrzypisanie> uprawnienia) =>
        string.Join(", ", uprawnienia.Select(u =>
            string.IsNullOrWhiteSpace(u.Podtyp) ? u.Nazwa : $"{u.Nazwa} {u.Podtyp}"));

    private static string BuildOdznaczeniaSkrot(Funkcjonariusz f) =>
        string.Join(", ", f.Odznaczenia.Select(o => $"{o.Nazwa} ({DateDisplayFormat.Format(o.DataNadania)})"));
}
