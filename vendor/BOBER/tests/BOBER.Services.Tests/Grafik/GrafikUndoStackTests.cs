using BOBER.Core.Grafik;

namespace BOBER.Services.Tests.Grafik;

public sealed class GrafikUndoStackTests
{
    private static GrafikUndoEntry Entry(params (int Fid, int Day)[] cells) =>
        new()
        {
            Cells = cells.Select(c => new GrafikUndoCell
            {
                FunkcjonariuszId = c.Fid,
                Month = 7,
                Day = c.Day,
                PreviousTyp = "U",
                PreviousFromUrlopPlan = false
            }).ToList()
        };

    [Fact]
    public void Push_PustyEntry_Ignoruje()
    {
        var stack = new GrafikUndoStack();
        stack.Push(new GrafikUndoEntry { Cells = [] });
        Assert.False(stack.CanUndo);
        Assert.Equal(0, stack.Count);
    }

    [Fact]
    public void TryPop_LIFO_ILimitGlebokosci()
    {
        var stack = new GrafikUndoStack(maxDepth: 2);
        stack.Push(Entry((1, 1)));
        stack.Push(Entry((1, 2)));
        stack.Push(Entry((1, 3)));

        Assert.Equal(2, stack.Count);
        Assert.True(stack.TryPop(out var newest));
        Assert.Equal(3, newest.Cells[0].Day);
        Assert.True(stack.TryPop(out var older));
        Assert.Equal(2, older.Cells[0].Day);
        Assert.False(stack.TryPop(out _));
    }

    [Fact]
    public void Clear_UsuwaWszystkie()
    {
        var stack = new GrafikUndoStack();
        stack.Push(Entry((1, 1)));
        stack.Clear();
        Assert.False(stack.CanUndo);
    }

    [Fact]
    public void Changed_PoPushIPop()
    {
        var stack = new GrafikUndoStack();
        var count = 0;
        stack.Changed += (_, _) => count++;

        stack.Push(Entry((1, 1)));
        Assert.Equal(1, count);
        Assert.True(stack.TryPop(out _));
        Assert.Equal(2, count);
        stack.Clear();
        Assert.Equal(2, count); // już pusty — bez eventu
    }
}
