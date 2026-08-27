using System.Collections.ObjectModel;
using SKRYBEK.App.ViewModels;
using SKRYBEK.Core.Enums;
using SKRYBEK.Core.Models;

namespace SKRYBEK.App.Helpers;

/// <summary>
/// Osoby z dyżuru muszą być też na liście wolnej służby.
/// Każdy wiersz dyżuru ma co najwyżej jedną kopię — bez dokładania nowego pola przy każdej literze.
/// </summary>
internal sealed class DyzurWolnaSluzbaSynchronizer
{
    private readonly Dictionary<NieobecnyViewModel, NieobecnyViewModel> _kopiaDlaDyzuru = [];
    private readonly HashSet<NieobecnyViewModel> _autoKopie = [];
    private bool _wToku;

    public void Reset()
    {
        _kopiaDlaDyzuru.Clear();
        _autoKopie.Clear();
    }

    /// <param name="wymusNoweWpisy">
    /// True przy zapisie: dopisz też ręcznie wpisane nazwiska spoza personelu.
    /// False przy pisaniu: nowy wiersz tylko dla kompletnej osoby (wybór z listy / dokładne dopasowanie).
    /// </param>
    public void Synchronizuj(
        ObservableCollection<NieobecnyViewModel> dyzurItems,
        ObservableCollection<NieobecnyViewModel> wolnaItems,
        IEnumerable<Funkcjonariusz>? personel,
        bool wymusNoweWpisy)
    {
        if (_wToku) return;
        _wToku = true;
        try
        {
            UsunKopieUsunietychDyzurow(dyzurItems, wolnaItems);

            foreach (var dyzur in dyzurItems.ToList())
            {
                var model = dyzur.ToModel();
                if (string.IsNullOrWhiteSpace(model.Nazwisko) && model.FunkcjonariuszId is null)
                {
                    UsunAutoKopie(dyzur, wolnaItems);
                    continue;
                }

                if (_kopiaDlaDyzuru.TryGetValue(dyzur, out var kopia) && wolnaItems.Contains(kopia))
                {
                    if (Pasuje(kopia, model))
                        continue;

                    if (_autoKopie.Contains(kopia))
                    {
                        var inna = ZnajdzNiepowiazana(wolnaItems, model, kopia);
                        if (inna is not null)
                        {
                            UsunAutoKopie(dyzur, wolnaItems);
                            _kopiaDlaDyzuru[dyzur] = inna;
                            continue;
                        }

                        kopia.ZastosujDane(model.Nazwisko.Trim(), dyzur.WybranaOsoba, model.FunkcjonariuszId);
                        continue;
                    }

                    _kopiaDlaDyzuru.Remove(dyzur);
                }

                var istniejaca = ZnajdzNiepowiazana(wolnaItems, model, wylacz: null);
                if (istniejaca is not null)
                {
                    _kopiaDlaDyzuru[dyzur] = istniejaca;
                    continue;
                }

                if (!wymusNoweWpisy && !JestKompletnaOsoba(model, personel))
                    continue;

                var nowa = new NieobecnyViewModel(new NieobecnyWSluzbie
                {
                    FunkcjonariuszId = model.FunkcjonariuszId,
                    Nazwisko = model.Nazwisko.Trim(),
                    TypNieobecnosci = TypNieobecnosci.CzasWolny
                }, personel);

                wolnaItems.Add(nowa);
                _kopiaDlaDyzuru[dyzur] = nowa;
                _autoKopie.Add(nowa);
            }
        }
        finally
        {
            _wToku = false;
        }
    }

    private void UsunKopieUsunietychDyzurow(
        ObservableCollection<NieobecnyViewModel> dyzurItems,
        ObservableCollection<NieobecnyViewModel> wolnaItems)
    {
        foreach (var dyzur in _kopiaDlaDyzuru.Keys.Where(k => !dyzurItems.Contains(k)).ToList())
            UsunAutoKopie(dyzur, wolnaItems);
    }

    private void UsunAutoKopie(
        NieobecnyViewModel dyzur,
        ObservableCollection<NieobecnyViewModel> wolnaItems)
    {
        if (!_kopiaDlaDyzuru.Remove(dyzur, out var kopia))
            return;

        if (_autoKopie.Remove(kopia) && wolnaItems.Contains(kopia))
            wolnaItems.Remove(kopia);
    }

    private NieobecnyViewModel? ZnajdzNiepowiazana(
        ObservableCollection<NieobecnyViewModel> wolnaItems,
        NieobecnyWSluzbie model,
        NieobecnyViewModel? wylacz)
    {
        var zajete = _kopiaDlaDyzuru.Values.ToHashSet();
        if (wylacz is not null)
            zajete.Remove(wylacz);

        foreach (var item in wolnaItems)
        {
            if (ReferenceEquals(item, wylacz) || zajete.Contains(item))
                continue;

            if (Pasuje(item, model))
                return item;
        }

        return null;
    }

    private static bool Pasuje(NieobecnyViewModel item, NieobecnyWSluzbie model)
    {
        var wm = item.ToModel();
        if (model.FunkcjonariuszId is int fid && wm.FunkcjonariuszId == fid)
            return true;

        var nazwisko = model.Nazwisko?.Trim() ?? string.Empty;
        return !string.IsNullOrWhiteSpace(nazwisko)
            && string.Equals(wm.Nazwisko?.Trim(), nazwisko, StringComparison.OrdinalIgnoreCase);
    }

    private static bool JestKompletnaOsoba(NieobecnyWSluzbie model, IEnumerable<Funkcjonariusz>? personel)
    {
        if (model.FunkcjonariuszId.HasValue)
            return true;

        return personel is not null
            && PersonelSuggestFilter.ZnajdzDokladnie(personel, model.Nazwisko) is not null;
    }
}
