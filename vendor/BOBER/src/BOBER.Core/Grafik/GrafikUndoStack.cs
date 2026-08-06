namespace BOBER.Core.Grafik;

/// <summary>Stan komórki przed zmianą — do cofnięcia wpisu w grafiku służb.</summary>
public sealed class GrafikUndoCell
{
    public required int FunkcjonariuszId { get; init; }
    public required int Month { get; init; }
    public required int Day { get; init; }
    public required string PreviousTyp { get; init; }
    public required bool PreviousFromUrlopPlan { get; init; }
}

/// <summary>Jedna akcja użytkownika (np. batch wielu komórek) na stosie Cofnij.</summary>
public sealed class GrafikUndoEntry
{
    public required IReadOnlyList<GrafikUndoCell> Cells { get; init; }
}

/// <summary>Stos cofnięć zmian w grafiku miesięcznym (limit głębokości).</summary>
public sealed class GrafikUndoStack
{
    public const int DefaultMaxDepth = 20;

    private readonly List<GrafikUndoEntry> _entries = [];
    private readonly int _maxDepth;

    public GrafikUndoStack(int maxDepth = DefaultMaxDepth) =>
        _maxDepth = Math.Max(1, maxDepth);

    public bool CanUndo => _entries.Count > 0;

    public int Count => _entries.Count;

    public event EventHandler? Changed;

    public void Push(GrafikUndoEntry entry)
    {
        if (entry.Cells.Count == 0)
            return;

        _entries.Add(entry);
        while (_entries.Count > _maxDepth)
            _entries.RemoveAt(0);

        Changed?.Invoke(this, EventArgs.Empty);
    }

    public bool TryPop(out GrafikUndoEntry entry)
    {
        if (_entries.Count == 0)
        {
            entry = null!;
            return false;
        }

        var last = _entries.Count - 1;
        entry = _entries[last];
        _entries.RemoveAt(last);
        Changed?.Invoke(this, EventArgs.Empty);
        return true;
    }

    public void Clear()
    {
        if (_entries.Count == 0)
            return;

        _entries.Clear();
        Changed?.Invoke(this, EventArgs.Empty);
    }
}
