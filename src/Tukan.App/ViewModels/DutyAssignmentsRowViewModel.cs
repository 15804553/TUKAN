using System.Collections.Generic;
using System.Linq;

namespace Tukan.App.ViewModels;

public sealed class DutyAssignmentsRowViewModel
{
    private readonly Dictionary<int, string> _cells = [];

    public int Numer { get; init; }

    public string ImieNazwisko { get; init; } = string.Empty;

    public string this[int day] => _cells.TryGetValue(day, out var value) ? value : string.Empty;

    public void AddAssignment(int day, string code)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return;
        }

        if (!_cells.TryGetValue(day, out var existing) || string.IsNullOrWhiteSpace(existing))
        {
            _cells[day] = code;
            return;
        }

        var parts = existing.Split(" / ", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Contains(code, StringComparer.OrdinalIgnoreCase))
        {
            return;
        }

        _cells[day] = $"{existing} / {code}";
    }
}
