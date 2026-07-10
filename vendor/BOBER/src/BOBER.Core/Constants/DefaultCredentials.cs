using BOBER.Core.Enums;

namespace BOBER.Core.Constants;

public static class DefaultCredentials
{
    public const string DatabasePassword = "5359";

    public static readonly IReadOnlyDictionary<UserRole, string> DefaultPasswords =
        new Dictionary<UserRole, string>
        {
            { UserRole.Zmiana1, "zmiana1" },
            { UserRole.Zmiana2, "zmiana2" },
            { UserRole.Zmiana3, "zmiana3" }
        };
}
