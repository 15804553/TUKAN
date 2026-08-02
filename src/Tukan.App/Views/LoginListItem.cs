namespace Tukan.App.Views;

/// <summary>Element listy logowania — konto albo separator wizualny między grupami.</summary>
public sealed class LoginListItem
{
    private LoginListItem(string display, string? login)
    {
        Display = display;
        Login = login;
    }

    public string Display { get; }
    public string? Login { get; }
    public bool IsSeparator => Login is null;

    public static LoginListItem Account(string login) => new(login, login);

    public static LoginListItem Separator() => new("──────────────", null);

    public override string ToString() => Display;
}
