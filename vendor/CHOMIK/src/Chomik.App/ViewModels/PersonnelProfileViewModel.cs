using Chomik.Core;

namespace Chomik.App.ViewModels;

public sealed class PersonnelProfileViewModel
{
    public required string Stopien { get; init; }

    public required string PelneImieNazwisko { get; init; }

    public required string Stanowisko { get; init; }

    public required string NumerZmianyLabel { get; init; }

    public IReadOnlyList<PersonnelProfileDateItem> Terminy { get; init; } = [];

    public IReadOnlyList<PersonnelProfileUprawnienieItem> Uprawnienia { get; init; } = [];
}

public sealed class PersonnelProfileDateItem
{
    public PersonnelProfileDateItem(string etykieta, DateTime? data)
    {
        Etykieta = etykieta;
        Data = data;
    }

    public string Etykieta { get; }

    public DateTime? Data { get; }

    public string WyswietlanaData => Data.HasValue ? DateDisplayFormat.Format(Data) : "—";
}

public sealed class PersonnelProfileUprawnienieItem
{
    public required string Etykieta { get; init; }

    public DateTime? WazneDo { get; init; }

    public string WyswietlanaData => WazneDo.HasValue ? DateDisplayFormat.Format(WazneDo) : "—";
}
