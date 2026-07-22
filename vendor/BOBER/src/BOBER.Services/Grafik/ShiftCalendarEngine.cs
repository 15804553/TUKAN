using System.Text.Json;
using BOBER.Data.Repositories;

namespace BOBER.Services.Grafik;

/// <summary>
/// Silnik kalendarza zmian PSP (cykl 3 dni: 1 praca, 2 wolne).
/// Offsety w Ustawieniach (DataReferencyjna, OffsetyZmian). Pusta komórka w UI = w pracy.
/// </summary>
public sealed class ShiftCalendarEngine(IUstawieniaRepository ustawienia)
{
    private DateOnly? _referenceDate;
    private Dictionary<int, int>? _offsets;

    private async Task EnsureLoadedAsync(CancellationToken cancellationToken)
    {
        if (_referenceDate.HasValue) return;

        var refStr = await ustawienia.GetAsync("DataReferencyjna", cancellationToken) ?? "2025-01-01";
        _referenceDate = DateOnly.ParseExact(refStr, "yyyy-MM-dd");

        var offsetsJson = await ustawienia.GetAsync("OffsetyZmian", cancellationToken)
                          ?? "{\"1\":1,\"2\":2,\"3\":0}";
        _offsets = JsonSerializer.Deserialize<Dictionary<string, int>>(offsetsJson)!
            .ToDictionary(kv => int.Parse(kv.Key), kv => kv.Value);
    }

    /// <summary>Sprawdza czy dana zmiana pracuje w podanym dniu.</summary>
    public async Task<bool> IsWorkDayAsync(int zmianaId, DateOnly date, CancellationToken cancellationToken = default)
    {
        await EnsureLoadedAsync(cancellationToken);
        return ComputeIsWorkDay(zmianaId, date);
    }

    /// <summary>Zwraca listę dni pracy danej zmiany w podanym roku.</summary>
    public async Task<IEnumerable<DateOnly>> GetWorkDaysAsync(
        int zmianaId,
        int year,
        CancellationToken cancellationToken = default)
    {
        await EnsureLoadedAsync(cancellationToken);
        var days = new List<DateOnly>();
        var start = new DateOnly(year, 1, 1);
        var end = new DateOnly(year, 12, 31);

        for (var d = start; d <= end; d = d.AddDays(1))
        {
            if (ComputeIsWorkDay(zmianaId, d))
                days.Add(d);
        }

        return days;
    }

    /// <summary>
    /// Zapamiętuje offset na następny rok (wywoływać na koniec roku lub przy zmianie roku).
    /// </summary>
    public async Task PersistYearTransitionAsync(int currentYear, CancellationToken cancellationToken = default)
    {
        await EnsureLoadedAsync(cancellationToken);

        var jan1Next = new DateOnly(currentYear + 1, 1, 1);
        var dayOffset = jan1Next.DayNumber - _referenceDate!.Value.DayNumber;

        var newOffsets = new Dictionary<int, int>();
        foreach (var (zmiana, baseOffset) in _offsets!)
        {
            newOffsets[zmiana] = ((baseOffset - dayOffset) % 3 + 3) % 3;
        }

        var newRefStr = jan1Next.ToString("yyyy-MM-dd");
        var newOffsetsJson = JsonSerializer.Serialize(
            newOffsets.ToDictionary(kv => kv.Key.ToString(), kv => kv.Value));

        await ustawienia.SetAsync("DataReferencyjna", newRefStr, cancellationToken);
        await ustawienia.SetAsync("OffsetyZmian", newOffsetsJson, cancellationToken);
        await ustawienia.SetAsync($"DataReferencyjna_{currentYear}", _referenceDate.Value.ToString("yyyy-MM-dd"), cancellationToken);

        _referenceDate = jan1Next;
        _offsets = newOffsets;
    }

    private bool ComputeIsWorkDay(int zmianaId, DateOnly date)
    {
        var dayOffset = date.DayNumber - _referenceDate!.Value.DayNumber;
        var slot = ((dayOffset % 3) + 3) % 3;
        return _offsets!.TryGetValue(zmianaId, out var zmianaOffset) && slot == zmianaOffset;
    }

    /// <summary>Zwraca identyfikator zmiany która pracuje w danym dniu (1, 2 lub 3).</summary>
    public async Task<int> GetWorkingShiftAsync(DateOnly date, CancellationToken cancellationToken = default)
    {
        await EnsureLoadedAsync(cancellationToken);
        return ComputeWorkingShift(date);
    }

    /// <summary>Mapa dzień miesiąca → zmiana pełniąca służbę.</summary>
    public async Task<IReadOnlyDictionary<int, int>> GetWorkingShiftsForMonthAsync(
        int rok,
        int miesiac,
        CancellationToken cancellationToken = default)
    {
        await EnsureLoadedAsync(cancellationToken);
        var daysInMonth = DateTime.DaysInMonth(rok, miesiac);
        var result = new Dictionary<int, int>(daysInMonth);
        for (var day = 1; day <= daysInMonth; day++)
            result[day] = ComputeWorkingShift(new DateOnly(rok, miesiac, day));
        return result;
    }

    private int ComputeWorkingShift(DateOnly date)
    {
        foreach (var zmiana in _offsets!.Keys)
        {
            if (ComputeIsWorkDay(zmiana, date))
                return zmiana;
        }

        return 0;
    }
}
