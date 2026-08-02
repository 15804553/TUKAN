using System.Windows;

namespace Tukan.App.Views;

/// <summary>Element listy logowania — konto z wyróżnieniem pogrubienia (bez Gość).</summary>
public sealed class LoginListItem
{
    private LoginListItem(string display, string login, bool isBold)
    {
        Display = display;
        Login = login;
        FontWeight = isBold ? FontWeights.Bold : FontWeights.Normal;
    }

    public string Display { get; }
    public string Login { get; }
    public FontWeight FontWeight { get; }

    public static LoginListItem Account(string login) =>
        new(login, login, isBold: !IsGuestLogin(login));

    public override string ToString() => Display;

    private static bool IsGuestLogin(string login) =>
        login.StartsWith("Gość ", StringComparison.OrdinalIgnoreCase)
        || login.StartsWith("Gosc ", StringComparison.OrdinalIgnoreCase);
}
