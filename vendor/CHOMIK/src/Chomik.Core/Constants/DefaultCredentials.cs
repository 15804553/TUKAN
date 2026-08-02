using Chomik.Core.Enums;

namespace Chomik.Core.Constants;

public static class DefaultCredentials
{
    public const string DatabasePassword = "5359";
    public const string LegacyDatabasePassword = "5393";

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
