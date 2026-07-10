using BOBER.Core.Enums;

namespace BOBER.Core.Models;

public sealed class UserAccount
{
    public int Id { get; set; }
    public string Login { get; set; } = string.Empty;
    public UserRole Role { get; set; }
    public int NumerZmiany { get; set; }
    public string HasloHash { get; set; } = string.Empty;
    public string HasloSol { get; set; } = string.Empty;
}
