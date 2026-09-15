using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using BOBER.App.ViewModels;
using BOBER.Core.Enums;
using BOBER.Core.Models;
using BOBER.Core.Rules;

namespace BOBER.App.Views.Chrome;

public partial class GrafikZliczanieEditorDialog : Window
{
    private readonly HashSet<string> _zajeteNazwy;
    private readonly IReadOnlyList<GrafikZliczanieSlownikPozycja> _uprawnienia;
    private readonly IReadOnlyList<GrafikZliczanieSlownikPozycja> _stanowiska;
    private readonly List<GrafikZliczanieGrupaEdit> _grupy = [];
    private readonly List<GrafikZliczaniePoziomEdit> _poziomy = [];

    public GrafikZliczanieWiersz? Result { get; private set; }

    public GrafikZliczanieEditorDialog(
        GrafikZliczanieWiersz? existing,
        IReadOnlyList<GrafikZliczanieSlownikPozycja> uprawnienia,
        IReadOnlyList<GrafikZliczanieSlownikPozycja> stanowiska,
        IEnumerable<string> zajeteNazwy)
    {
        InitializeComponent();
        ChromeWindowConfigurator.Apply(this, canResize: true);
        _uprawnienia = uprawnienia;
        _stanowiska = stanowiska;
        _zajeteNazwy = zajeteNazwy
            .Where(n => !string.Equals(n, existing?.Nazwa, StringComparison.OrdinalIgnoreCase))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (existing is null)
        {
            TitleTextBlock.Text = "Nowy wiersz zliczania";
            TypCombo.SelectedIndex = 0;
            ZrodloCombo.SelectedIndex = 0;
            _grupy.Add(CreateGrupa(GrafikZliczanieZrodlo.Uprawnienia, []));
        }
        else
        {
            TitleTextBlock.Text = "Edycja wiersza zliczania";
            NazwaTextBox.Text = existing.Nazwa;
            TypCombo.SelectedIndex = existing.Typ == GrafikZliczanieTyp.Poziom ? 1 : 0;
            ZrodloCombo.SelectedIndex = existing.Zrodlo == GrafikZliczanieZrodlo.Stanowiska ? 1 : 0;
            LoadExisting(existing);
        }

        RefreshTypPanels();
        RebuildGrupyZliczania();
        RebuildPoziomy();
    }

    private void LoadExisting(GrafikZliczanieWiersz existing)
    {
        if (existing.Typ == GrafikZliczanieTyp.Poziom)
        {
            foreach (var poziom in existing.Poziomy.OrderBy(p => p.Kolejnosc))
            {
                var edit = new GrafikZliczaniePoziomEdit { Kod = poziom.Kod, Kolejnosc = poziom.Kolejnosc };
                foreach (var slot in poziom.Sloty.OrderBy(s => s.Kolejnosc))
                {
                    var slotEdit = new GrafikZliczanieSlotEdit
                    {
                        Nazwa = slot.Nazwa,
                        Zrodlo = slot.Zrodlo,
                        Liczba = slot.Liczba,
                        Kolejnosc = slot.Kolejnosc,
                        WspoldzielSlotKolejnosc = slot.WspoldzielSlotKolejnosc
                    };
                    foreach (var grupa in slot.Grupy.OrderBy(g => g.Kolejnosc))
                        slotEdit.Grupy.Add(CreateGrupa(slot.Zrodlo, grupa.RefIds));
                    if (slotEdit.Grupy.Count == 0)
                        slotEdit.Grupy.Add(CreateGrupa(slot.Zrodlo, []));
                    edit.Sloty.Add(slotEdit);
                }

                _poziomy.Add(edit);
            }

            return;
        }

        foreach (var grupa in existing.Grupy.OrderBy(g => g.Kolejnosc))
            _grupy.Add(CreateGrupa(existing.Zrodlo, grupa.RefIds));
        if (_grupy.Count == 0)
            _grupy.Add(CreateGrupa(existing.Zrodlo, []));
    }

    private GrafikZliczanieGrupaEdit CreateGrupa(GrafikZliczanieZrodlo zrodlo, IReadOnlyCollection<int> selected)
    {
        var grupa = new GrafikZliczanieGrupaEdit();
        var zrodloLista = zrodlo == GrafikZliczanieZrodlo.Stanowiska ? _stanowiska : _uprawnienia;
        foreach (var pozycja in zrodloLista)
        {
            grupa.Pozycje.Add(new GrafikZliczaniePozycjaWyboru
            {
                Id = pozycja.Id,
                Etykieta = pozycja.Etykieta,
                IsSelected = selected.Contains(pozycja.Id)
            });
        }

        return grupa;
    }

    private GrafikZliczanieZrodlo CurrentZrodlo() =>
        (ZrodloCombo.SelectedItem as ComboBoxItem)?.Tag as string == "Stanowiska"
            ? GrafikZliczanieZrodlo.Stanowiska
            : GrafikZliczanieZrodlo.Uprawnienia;

    private bool IsPoziom() =>
        (TypCombo.SelectedItem as ComboBoxItem)?.Tag as string == "Poziom";

    private void RefreshTypPanels()
    {
        var poziom = IsPoziom();
        ZliczaniePanel.Visibility = poziom ? Visibility.Collapsed : Visibility.Visible;
        PoziomPanel.Visibility = poziom ? Visibility.Visible : Visibility.Collapsed;
    }

    private void OnTypChanged(object sender, SelectionChangedEventArgs e) => RefreshTypPanels();

    private void OnZrodloChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!IsLoaded)
            return;
        var zrodlo = CurrentZrodlo();
        _grupy.Clear();
        _grupy.Add(CreateGrupa(zrodlo, []));
        RebuildGrupyZliczania();
    }

    private void OnAddGrupaZliczaniaClick(object sender, RoutedEventArgs e)
    {
        _grupy.Add(CreateGrupa(CurrentZrodlo(), []));
        RebuildGrupyZliczania();
    }

    private void RebuildGrupyZliczania()
    {
        GrupyZliczaniaHost.Items.Clear();
        for (var i = 0; i < _grupy.Count; i++)
            GrupyZliczaniaHost.Items.Add(CreateGrupaBorder(_grupy, i, RebuildGrupyZliczania));
    }

    private Border CreateGrupaBorder(
        IList<GrafikZliczanieGrupaEdit> grupy,
        int index,
        Action rebuild)
    {
        var grupa = grupy[index];
        var panel = new StackPanel { Margin = new Thickness(0, 0, 0, 10) };
        var header = new DockPanel { Margin = new Thickness(0, 0, 0, 4) };
        header.Children.Add(new TextBlock
        {
            Text = index == 0 ? "Grupa (LUB)" : "ORAZ grupa (LUB)",
            VerticalAlignment = VerticalAlignment.Center,
            Foreground = TryFindResource("ForegroundBrush") as System.Windows.Media.Brush
        });
        if (grupy.Count > 1)
        {
            var usun = new Button { Content = "Usuń grupę", Margin = new Thickness(8, 0, 0, 0), Padding = new Thickness(8, 2, 8, 2) };
            DockPanel.SetDock(usun, Dock.Right);
            usun.Click += (_, _) =>
            {
                grupy.RemoveAt(index);
                rebuild();
            };
            header.Children.Add(usun);
        }

        panel.Children.Add(header);
        var wrap = new WrapPanel();
        foreach (var pozycja in grupa.Pozycje)
        {
            var box = new CheckBox
            {
                Content = pozycja.Etykieta,
                IsChecked = pozycja.IsSelected,
                Margin = new Thickness(0, 0, 12, 6),
                Tag = pozycja
            };
            box.Checked += (_, _) => pozycja.IsSelected = true;
            box.Unchecked += (_, _) => pozycja.IsSelected = false;
            wrap.Children.Add(box);
        }

        panel.Children.Add(wrap);
        return new Border
        {
            BorderBrush = TryFindResource("BorderBrush") as System.Windows.Media.Brush,
            BorderThickness = new Thickness(1),
            Padding = new Thickness(8),
            Child = panel
        };
    }

    private void OnAddPoziomClick(object sender, RoutedEventArgs e)
    {
        var poziom = new GrafikZliczaniePoziomEdit { Kod = _poziomy.Count == 0 ? "AB" : "A" };
        var slot = new GrafikZliczanieSlotEdit { Nazwa = "Slot", Kolejnosc = 0, Liczba = 1 };
        slot.Grupy.Add(CreateGrupa(slot.Zrodlo, []));
        poziom.Sloty.Add(slot);
        _poziomy.Add(poziom);
        RebuildPoziomy();
    }

    private void RebuildPoziomy()
    {
        PoziomyHost.Items.Clear();
        for (var i = 0; i < _poziomy.Count; i++)
            PoziomyHost.Items.Add(CreatePoziomBorder(i));
    }

    private Border CreatePoziomBorder(int index)
    {
        var poziom = _poziomy[index];
        var root = new StackPanel { Margin = new Thickness(0, 0, 0, 12) };
        var header = new DockPanel { Margin = new Thickness(0, 0, 0, 6) };
        header.Children.Add(new TextBlock
        {
            Text = $"Poziom {index + 1}",
            VerticalAlignment = VerticalAlignment.Center,
            FontWeight = FontWeights.SemiBold,
            Foreground = TryFindResource("ForegroundBrush") as System.Windows.Media.Brush
        });
        var usun = new Button { Content = "Usuń", Padding = new Thickness(8, 2, 8, 2) };
        DockPanel.SetDock(usun, Dock.Right);
        usun.Click += (_, _) =>
        {
            _poziomy.RemoveAt(index);
            RebuildPoziomy();
        };
        header.Children.Add(usun);
        root.Children.Add(header);

        var kodPanel = new DockPanel { Margin = new Thickness(0, 0, 0, 6) };
        kodPanel.Children.Add(new TextBlock { Text = "Kod", Width = 80, VerticalAlignment = VerticalAlignment.Center });
        var kodBox = new TextBox { Text = poziom.Kod };
        kodBox.TextChanged += (_, _) => poziom.Kod = kodBox.Text;
        kodPanel.Children.Add(kodBox);
        root.Children.Add(kodPanel);

        var addSlot = new Button { Content = "Dodaj slot", Padding = new Thickness(8, 4, 8, 4), HorizontalAlignment = HorizontalAlignment.Left, Margin = new Thickness(0, 0, 0, 6) };
        addSlot.Click += (_, _) =>
        {
            var slot = new GrafikZliczanieSlotEdit
            {
                Nazwa = "Slot",
                Kolejnosc = (short)poziom.Sloty.Count,
                Liczba = 1
            };
            slot.Grupy.Add(CreateGrupa(slot.Zrodlo, []));
            poziom.Sloty.Add(slot);
            RebuildPoziomy();
        };
        root.Children.Add(addSlot);

        for (var s = 0; s < poziom.Sloty.Count; s++)
            root.Children.Add(CreateSlotPanel(poziom, s));

        return new Border
        {
            BorderBrush = TryFindResource("BorderBrush") as System.Windows.Media.Brush,
            BorderThickness = new Thickness(1),
            Padding = new Thickness(8),
            Child = root
        };
    }

    private UIElement CreateSlotPanel(GrafikZliczaniePoziomEdit poziom, int slotIndex)
    {
        var slot = poziom.Sloty[slotIndex];
        var panel = new StackPanel { Margin = new Thickness(8, 0, 0, 8) };
        var line = new Grid();
        line.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        line.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(70) });
        line.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(90) });
        line.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(80) });
        var nazwa = new TextBox { Text = slot.Nazwa, Margin = new Thickness(0, 0, 6, 0) };
        nazwa.TextChanged += (_, _) => slot.Nazwa = nazwa.Text;
        Grid.SetColumn(nazwa, 0);
        var liczba = new TextBox { Text = slot.Liczba.ToString(), Margin = new Thickness(0, 0, 6, 0) };
        liczba.TextChanged += (_, _) =>
        {
            if (short.TryParse(liczba.Text, out var n) && n > 0)
                slot.Liczba = n;
        };
        Grid.SetColumn(liczba, 1);
        var zrodlo = new ComboBox { Margin = new Thickness(0, 0, 6, 0) };
        zrodlo.Items.Add("Uprawnienia");
        zrodlo.Items.Add("Stanowiska");
        zrodlo.SelectedIndex = slot.Zrodlo == GrafikZliczanieZrodlo.Stanowiska ? 1 : 0;
        zrodlo.SelectionChanged += (_, _) =>
        {
            var next = zrodlo.SelectedIndex == 1 ? GrafikZliczanieZrodlo.Stanowiska : GrafikZliczanieZrodlo.Uprawnienia;
            if (slot.Zrodlo == next)
                return;
            slot.Zrodlo = next;
            slot.Grupy.Clear();
            slot.Grupy.Add(CreateGrupa(next, []));
            RebuildPoziomy();
        };
        Grid.SetColumn(zrodlo, 2);
        var usun = new Button { Content = "Usuń" };
        usun.Click += (_, _) =>
        {
            if (poziom.Sloty.Count <= 1)
                return;
            poziom.Sloty.RemoveAt(slotIndex);
            RebuildPoziomy();
        };
        Grid.SetColumn(usun, 3);
        line.Children.Add(nazwa);
        line.Children.Add(liczba);
        line.Children.Add(zrodlo);
        line.Children.Add(usun);
        panel.Children.Add(line);

        var share = new DockPanel { Margin = new Thickness(0, 4, 0, 4) };
        share.Children.Add(new TextBlock
        {
            Text = "Współdziel z kolejn. slotu",
            Width = 170,
            VerticalAlignment = VerticalAlignment.Center
        });
        var shareBox = new TextBox { Text = slot.WspoldzielTekst };
        shareBox.TextChanged += (_, _) => slot.WspoldzielTekst = shareBox.Text;
        share.Children.Add(shareBox);
        panel.Children.Add(share);

        var addGrupa = new Button { Content = "Dodaj grupę ORAZ", HorizontalAlignment = HorizontalAlignment.Left, Margin = new Thickness(0, 0, 0, 4), Padding = new Thickness(8, 2, 8, 2) };
        addGrupa.Click += (_, _) =>
        {
            slot.Grupy.Add(CreateGrupa(slot.Zrodlo, []));
            RebuildPoziomy();
        };
        panel.Children.Add(addGrupa);

        for (var g = 0; g < slot.Grupy.Count; g++)
            panel.Children.Add(CreateGrupaBorder(slot.Grupy, g, RebuildPoziomy));

        return panel;
    }

    private void OnTitleBarMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed)
            DragMove();
    }

    private void OnCancelClick(object sender, RoutedEventArgs e) => DialogResult = false;

    private void OnAcceptClick(object sender, RoutedEventArgs e)
    {
        var nazwa = NazwaTextBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(nazwa))
        {
            Message("Podaj nazwę wiersza.");
            return;
        }

        if (GrafikZliczanieEvaluator.JestZarezerwowanaNazwa(nazwa))
        {
            Message("Nazwa „Wolne miejsca” jest zarezerwowana.");
            return;
        }

        if (_zajeteNazwy.Contains(nazwa))
        {
            Message("Wiersz o takiej nazwie już istnieje.");
            return;
        }

        GrafikZliczanieWiersz wiersz;
        if (IsPoziom())
        {
            if (!TryBuildPoziom(nazwa, out wiersz))
                return;
        }
        else if (!TryBuildZliczanie(nazwa, out wiersz))
        {
            return;
        }

        Result = wiersz;
        DialogResult = true;
    }

    private bool TryBuildZliczanie(string nazwa, out GrafikZliczanieWiersz wiersz)
    {
        wiersz = new GrafikZliczanieWiersz
        {
            Nazwa = nazwa,
            Typ = GrafikZliczanieTyp.Zliczanie,
            Zrodlo = CurrentZrodlo()
        };
        short i = 0;
        foreach (var grupa in _grupy)
        {
            var ids = grupa.Pozycje.Where(p => p.IsSelected).Select(p => p.Id).ToList();
            if (ids.Count == 0)
            {
                Message("Każda grupa musi mieć co najmniej jedno uprawnienie lub stanowisko.");
                return false;
            }

            wiersz.Grupy.Add(new GrafikZliczanieGrupa { Kolejnosc = i++, RefIds = ids });
        }

        if (wiersz.Grupy.Count == 0)
        {
            Message("Dodaj co najmniej jedną grupę kryteriów.");
            return false;
        }

        return true;
    }

    private bool TryBuildPoziom(string nazwa, out GrafikZliczanieWiersz wiersz)
    {
        wiersz = new GrafikZliczanieWiersz
        {
            Nazwa = nazwa,
            Typ = GrafikZliczanieTyp.Poziom
        };
        if (_poziomy.Count == 0)
        {
            Message("Dodaj co najmniej jeden poziom.");
            return false;
        }

        short pOrd = 0;
        foreach (var poziom in _poziomy)
        {
            if (string.IsNullOrWhiteSpace(poziom.Kod))
            {
                Message("Każdy poziom musi mieć kod (np. A lub AB).");
                return false;
            }

            if (poziom.Sloty.Count == 0)
            {
                Message($"Poziom {poziom.Kod} nie ma slotów.");
                return false;
            }

            var p = new GrafikZliczaniePoziom { Kod = poziom.Kod.Trim(), Kolejnosc = pOrd++ };
            short sOrd = 0;
            foreach (var slot in poziom.Sloty)
            {
                var s = new GrafikZliczanieSlot
                {
                    Nazwa = string.IsNullOrWhiteSpace(slot.Nazwa) ? $"Slot {sOrd + 1}" : slot.Nazwa.Trim(),
                    Zrodlo = slot.Zrodlo,
                    Liczba = slot.Liczba < 1 ? (short)1 : slot.Liczba,
                    Kolejnosc = sOrd,
                    WspoldzielSlotKolejnosc = slot.WspoldzielSlotKolejnosc
                };
                short gOrd = 0;
                foreach (var grupa in slot.Grupy)
                {
                    var ids = grupa.Pozycje.Where(p => p.IsSelected).Select(x => x.Id).ToList();
                    if (ids.Count == 0)
                    {
                        Message($"Slot „{s.Nazwa}” ma pustą grupę.");
                        return false;
                    }

                    s.Grupy.Add(new GrafikZliczanieGrupa { Kolejnosc = gOrd++, RefIds = ids });
                }

                sOrd++;
                p.Sloty.Add(s);
            }

            wiersz.Poziomy.Add(p);
        }

        return true;
    }

    private void Message(string text) =>
        BoberMessageBox.Show(this, text, "Zliczanie grafiku");
}
