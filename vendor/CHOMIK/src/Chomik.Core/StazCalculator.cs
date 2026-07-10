namespace Chomik.Core;

public static class StazCalculator
{
    public static int? CalculateServiceYears(DateTime? serviceStartDate)
    {
        if (serviceStartDate is not DateTime start)
        {
            return null;
        }

        var today = DateTime.Today;
        var startDate = start.Date;
        if (startDate > today)
        {
            return 0;
        }

        var years = today.Year - startDate.Year;
        if (startDate > today.AddYears(-years))
        {
            years--;
        }

        return Math.Max(0, years);
    }
}
