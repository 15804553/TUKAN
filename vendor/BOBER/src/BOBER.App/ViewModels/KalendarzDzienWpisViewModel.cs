using BOBER.Core.Models;

namespace BOBER.App.ViewModels;

public sealed class KalendarzDzienWpisViewModel
{
    public required KalendarzWpis Wpis { get; init; }
    public required string Tytul { get; init; }
    public required string Szczegoly { get; init; }
    public bool CanDelete { get; init; }
    public bool CanConfirmRead { get; init; }
    public bool CanReply { get; init; }
    public bool IsUnread { get; init; }

    /// <summary>Prywatna notatka wysłana przez bieżącą zmianę.</summary>
    public bool IsSent { get; init; }

    /// <summary>Notatka odebrana przez bieżącą zmianę (DCA lub prywatna od innej zmiany).</summary>
    public bool IsReceived { get; init; }
}
