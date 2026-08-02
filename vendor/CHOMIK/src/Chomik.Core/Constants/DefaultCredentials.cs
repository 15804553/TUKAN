using Chomik.Core.Enums;

namespace Chomik.Core.Constants;

public static class DefaultCredentials
{
    /// <summary>
    /// Kandydaci hasła pliku Access używani wyłącznie przy migracji / pierwszym otwarciu.
    /// Preferuj przechowywanie aktywnego hasła poza kodem (DPAPI / opcje runtime).
    /// </summary>
    public static IReadOnlyList<string> DatabasePasswordMigrationCandidates { get; } =
        ["5359", "5393"];

    /// <summary>Hasło startowe kont — używane przy seed/reset; nie ujawniać w UI. PA = null (bez hasła).</summary>
    public static readonly IReadOnlyDictionary<UserRole, string?> DefaultPasswords =
        new Dictionary<UserRole, string?>
        {
            [UserRole.Pa] = null,
            [UserRole.Zmiana1] = "1111",
            [UserRole.Zmiana2] = "2222",
            [UserRole.Zmiana3] = "3333",
            [UserRole.DcaJrg] = "0000",
            [UserRole.Administrator] = "5359",
            [UserRole.Gosc1] = "0001",
            [UserRole.Gosc2] = "0002",
            [UserRole.Gosc3] = "0003"
        };
}
