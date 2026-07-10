namespace Chomik.Core;

public enum DateValidityStatus
{
    None,
    Valid,
    Warning,
    Expired
}

public static class DateValidityEvaluator
{
    /// <summary>
    /// Zielony: więcej niż 2 miesiące do końca ważności.
    /// Pomarańczowy: 2 miesiące lub mniej, termin jeszcze nie minął.
    /// Czerwony: termin ważności upłynął.
    /// </summary>
    public static DateValidityStatus Evaluate(DateTime? validUntil)
    {
        if (validUntil is null)
        {
            return DateValidityStatus.None;
        }

        var expiry = validUntil.Value.Date;
        var today = DateTime.Today;

        if (expiry < today)
        {
            return DateValidityStatus.Expired;
        }

        if (expiry <= today.AddMonths(2))
        {
            return DateValidityStatus.Warning;
        }

        return DateValidityStatus.Valid;
    }
}
