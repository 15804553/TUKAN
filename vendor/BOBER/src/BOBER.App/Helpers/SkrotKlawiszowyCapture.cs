using System.Windows.Input;

namespace BOBER.App.Helpers;

/// <summary>Mapuje naciśnięty klawisz WPF na czytelną wartość skrótu (np. / zamiast OemQuestion).</summary>
public static class SkrotKlawiszowyCapture
{
    public static string FromKey(Key key) =>
        key switch
        {
            Key.Back or Key.Delete => string.Empty,
            // Ten sam fizyczny klawisz co / i ? — zapisujemy znak, nie nazwę OEM.
            Key.Oem2 or Key.OemQuestion or Key.Divide => "/",
            Key.OemPeriod or Key.Decimal => ".",
            _ => key.ToString()
        };
}
