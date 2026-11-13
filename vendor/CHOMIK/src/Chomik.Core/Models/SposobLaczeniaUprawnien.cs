namespace Chomik.Core.Models;

/// <summary>Jak łączyć kilka zaznaczonych uprawnień w filtrze widoku ogólnego.</summary>
public enum SposobLaczeniaUprawnien
{
    /// <summary>Wystarczy dowolne z zaznaczonych uprawnień.</summary>
    Lub,

    /// <summary>Osoba musi mieć wszystkie zaznaczone uprawnienia.</summary>
    I
}
