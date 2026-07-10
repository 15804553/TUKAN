namespace BOBER.App.ViewModels;

public sealed class DayHeaderViewModel
{
    public int Day { get; init; }
    public string DayName { get; init; } = string.Empty;
    public bool IsSaturday { get; init; }
    public bool IsSunday { get; init; }

    public static DayHeaderViewModel Create(int year, int month, int day)
    {
        var date = new DateTime(year, month, day);
        return new DayHeaderViewModel
        {
            Day = day,
            DayName = date.ToString("ddd"),
            IsSaturday = date.DayOfWeek == DayOfWeek.Saturday,
            IsSunday = date.DayOfWeek == DayOfWeek.Sunday
        };
    }
}
