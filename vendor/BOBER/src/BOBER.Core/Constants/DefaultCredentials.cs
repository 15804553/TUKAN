using BOBER.Core.Enums;

namespace BOBER.Core.Constants;

public static class DefaultCredentials
{
    /// <summary>Kandydaci hasła Access — tylko migracja / domyślny fallback opcji.</summary>
    public static IReadOnlyList<string> DatabasePasswordMigrationCandidates { get; } =
        ["5359"];

    public static string DatabasePassword => DatabasePasswordMigrationCandidates[0];

    public static readonly IReadOnlyDictionary<UserRole, string> DefaultPasswords =
        new Dictionary<UserRole, string>
        {
            { UserRole.Zmiana1, "zmiana1" },
            { UserRole.Zmiana2, "zmiana2" },
            { UserRole.Zmiana3, "zmiana3" }
        };
}
