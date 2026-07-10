using Chomik.Core.Models;

namespace Chomik.App.ViewModels;

public static class PersonnelProfileMapper
{
    public static PersonnelProfileViewModel ToViewModel(Funkcjonariusz entity) =>
        new()
        {
            Stopien = entity.Stopien,
            PelneImieNazwisko = entity.PelneImieNazwisko,
            Stanowisko = entity.Stanowisko,
            NumerZmianyLabel = $"Zmiana {entity.NumerZmiany}",
            Terminy =
            [
                new PersonnelProfileDateItem("Wstąpienie do służby", entity.DataWstepieniaDoSluzby),
                new PersonnelProfileDateItem("Badania okresowe do", entity.BadaniaOkresoweDo),
                new PersonnelProfileDateItem("Komora dymowa do", entity.KomoraDymowaDo),
                new PersonnelProfileDateItem("KPP do", entity.KppDo)
            ],
            Uprawnienia = entity.Uprawnienia
                .OrderBy(u => u.Nazwa, StringComparer.OrdinalIgnoreCase)
                .ThenBy(u => u.Podtyp, StringComparer.OrdinalIgnoreCase)
                .Select(u => new PersonnelProfileUprawnienieItem
                {
                    Etykieta = string.IsNullOrWhiteSpace(u.Podtyp) ? u.Nazwa : $"{u.Nazwa} ({u.Podtyp})",
                    WazneDo = u.WazneDo
                })
                .ToList()
        };
}
