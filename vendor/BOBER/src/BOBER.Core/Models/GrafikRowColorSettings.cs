namespace BOBER.Core.Models;

/// <summary>
/// Preferencje kolorowania wierszy grafiku służb (tylko UI).
/// </summary>
public sealed class GrafikRowColorSettings
{
    public const string DefaultColorA = "#FFFFFF";
    public const string DefaultColorB = "#D9E2F3";

    public GrafikRowColorMode Mode { get; init; } = GrafikRowColorMode.Role;

    public string ColorA { get; init; } = DefaultColorA;

    public string ColorB { get; init; } = DefaultColorB;
}
