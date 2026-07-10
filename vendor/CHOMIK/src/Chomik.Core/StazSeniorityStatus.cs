namespace Chomik.Core;

public enum StazSeniorityStatus
{
    None,
    UnderTenYears,
    TenToNineteenYears,
    TwentyYearsOrMore
}

public static class StazSeniorityEvaluator
{
    /// <summary>
    /// Zielony: poniżej 10 lat stażu.
    /// Żółty: od 10 do 19 lat włącznie.
    /// Czerwony: 20 lat i więcej.
    /// </summary>
    public static StazSeniorityStatus Evaluate(int? serviceYears) =>
        serviceYears switch
        {
            null => StazSeniorityStatus.None,
            < 10 => StazSeniorityStatus.UnderTenYears,
            < 20 => StazSeniorityStatus.TenToNineteenYears,
            _ => StazSeniorityStatus.TwentyYearsOrMore
        };

    public static StazSeniorityStatus EvaluateFromServiceStart(DateTime? serviceStartDate) =>
        Evaluate(StazCalculator.CalculateServiceYears(serviceStartDate));
}
