using System.Windows.Media;
using Chomik.Core.Models;

namespace Chomik.App.Helpers;

/// <summary>
/// Opcjonalne kolorowanie wierszy listy Edycji personelu — wstrzykiwane z TUKAN.
/// </summary>
public interface IPersonnelListColoring
{
    Task RefreshAsync(CancellationToken cancellationToken = default);

    bool TryGetAppearance(Funkcjonariusz funkcjonariusz, out Brush background, out Brush nameBorder);
}
