using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Input;
using Avalonia.VisualTree;
using Fitlog.Data;
using Fitlog.Models;

namespace Fitlog;

public sealed partial class MainWindow : Window
{
    private readonly FitnessRepository _repository;
    private readonly TimeProvider _clock;
    private readonly DispatcherTimer _clockTimer;
    private DateOnly _displayedDay;
    private TextBlock _clockLabel = new();
    private readonly Bitmap _brandLogo;
    private Backup _data = new();
    private Preferences Pref => _data.Preferences;
    private string _page = "Dashboard";
    private DateOnly _month;
    private int _year;
    private string _heatFilter = "Overall";
    private string _weightRange = "3 months";
    private ContentControl _pageHost = new();
    private TextBlock _status = new();
    private readonly List<IDisposable> _images = [];
    private DateOnly Today => DateOnly.FromDateTime(_clock.GetLocalNow().DateTime);
    private IBrush BackgroundBrush => Brush(Pref.Light ? "#F3F6F8" : "#101820");
    private IBrush Surface => Brush(Pref.Light ? "#FFFFFF" : "#1B2832");
    private IBrush Raised => Brush(Pref.Light ? "#E6EDF1" : "#24333E");
    private IBrush Line => Brush(Pref.Light ? "#D5E0E6" : "#304552");
    private IBrush Ink => Brush(Pref.Light ? "#1B303D" : "#F0F4F7");
    private IBrush Muted => Brush(Pref.Light ? "#526F80" : "#A1BDCF");
    private static IBrush Accent => Brush("#A3C5D5");
    private static IBrush Brush(string color) => Avalonia.Media.Brush.Parse(color);
    public MainWindow(FitnessRepository repository, TimeProvider? clock = null)
    {
        _repository = repository;
        _clock = clock ?? TimeProvider.System;
        _displayedDay = Today; _month = new(Today.Year, Today.Month, 1); _year = Today.Year;
        _clockTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _clockTimer.Tick += (_, _) => UpdateClock();
        using (var logo = AssetLoader.Open(new Uri("avares://Fitlog/Assets/fitlog-logo.png"))) _brandLogo = new Bitmap(logo);
        using (var logo = AssetLoader.Open(new Uri("avares://Fitlog/Assets/fitlog-logo.png"))) Icon = new WindowIcon(logo);
        SystemDecorations = SystemDecorations.None;
        PointerPressed += (_, e) => ResizeFromEdge(e);
        Title = "fitlog — Your fitness, day by day"; Width = 1440; Height = 1040; MinWidth = 940; MinHeight = 650;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;
        Reload(); _heatFilter = Pref.Heatmap; BuildShell();
        Opened += (_, _) => _clockTimer.Start();
        Activated += (_, _) => UpdateClock();
        Closed += (_, _) => { _clockTimer.Stop(); foreach (var image in _images) image.Dispose(); _brandLogo.Dispose(); };
    }
    internal void UpdateClock()
    {
        if (Today != _displayedDay)
        {
            if (_month.Year == _displayedDay.Year && _month.Month == _displayedDay.Month) _month = new(Today.Year, Today.Month, 1);
            if (_year == _displayedDay.Year) _year = Today.Year;
            _displayedDay = Today; Reload(); BuildShell();
        }
        _clockLabel.Text = _clock.GetLocalNow().ToString("dd MMM yyyy · HH:mm:ss", UiCulture);
    }
    private void Reload() => _data = _repository.Snapshot();
    private void BuildShell()
    {
        RequestedThemeVariant = Pref.Light ? ThemeVariant.Light : ThemeVariant.Dark;
        System.Globalization.CultureInfo.CurrentCulture = UiCulture;
        System.Globalization.CultureInfo.CurrentUICulture = UiCulture;
        FlowDirection = Arabic ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;
        FontFamily = new FontFamily(Arabic ? "Segoe UI" : "avares://Avalonia.Fonts.Inter/Assets#Inter");
        Background = BackgroundBrush; Foreground = Ink;
        var root = new Grid { ColumnDefinitions = new("235,*") };
        var sidebar = new Grid { RowDefinitions = new("88,*,64"), Margin = new Thickness(16, 0) };
        var brand = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 12, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(8, 0) };
        brand.Children.Add(new Border { Width = 48, Height = 48, CornerRadius = new(14), ClipToBounds = true, Background = Surface, Child = new Image { Source = _brandLogo, Stretch = Stretch.Uniform, FlowDirection = FlowDirection.LeftToRight } });
        brand.Children.Add(T("fitlog", 22, true)); sidebar.Children.Add(brand);
        var navigation = Stack(3); navigation.Margin = new Thickness(0, 16, 0, 0);
        (string label, string glyph)[] items = [("Dashboard", "\uE9D9"), ("Calendar", "\uE787"), ("Workouts", "\uE7C1"), ("Weight", "\uE9D5"), ("Measurements", "\uE9D2"), ("Progress Photos", "\uE722"), ("Goals & Records", "\uE7C1"), ("Journey", "\uE70B"), ("Statistics", "\uE9D2"), ("Settings", "\uE713")];
        foreach (var (label, glyph) in items)
        {
            var content = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 13 };
            content.Children.Add(Glyph(glyph)); content.Children.Add(T(label, 15, color: Muted));
            var button = Button(content, () => Navigate(label)); button.Classes.Add("nav"); button.Name = "Nav" + label.Replace(" ", "").Replace("&", "");
            button.Background = _page == label ? Raised : Brushes.Transparent;
            navigation.Children.Add(button);
        }
        var navScroll = new ScrollViewer { Content = navigation }; Grid.SetRow(navScroll, 1); sidebar.Children.Add(navScroll);
        var local = T("●   Saved on this device", 11, color: Muted); local.Margin = new Thickness(14, 0); local.VerticalAlignment = VerticalAlignment.Center; Grid.SetRow(local, 2); sidebar.Children.Add(local);
        root.Children.Add(new Border { Background = Surface, BorderBrush = Line, BorderThickness = new(0, 0, 1, 0), Child = sidebar });
        var right = new Grid { RowDefinitions = new("72,*,28") }; Grid.SetColumn(right, 1); root.Children.Add(right);
        var header = new Grid { ColumnDefinitions = new("*,Auto,Auto,Auto,Auto"), Margin = new Thickness(24, 0, 10, 0), Background = Brushes.Transparent };
        header.PointerPressed += (_, e) => DragChrome(this, e);
        brand.PointerPressed += (_, e) => DragChrome(this, e);
        var titleGroup = Stack(4); titleGroup.VerticalAlignment = VerticalAlignment.Center; titleGroup.Children.Add(T(_page, 14, true));
        _clockLabel = T(_clock.GetLocalNow().ToString("dd MMM yyyy · HH:mm:ss", UiCulture), 11, color: Muted); _clockLabel.Name = "LocalClock"; titleGroup.Children.Add(_clockLabel); header.Children.Add(titleGroup);
        var date = new CalendarDatePicker { Width = 165, VerticalAlignment = VerticalAlignment.Center, Name = "OpenDate", Watermark = Tr("Open a date"), DisplayDateEnd = Today.ToDateTime(TimeOnly.MinValue), Background = Surface, BorderBrush = Line, CornerRadius = new(12) };
        date.SelectedDateChanged += (_, _) => { if (date.SelectedDate is { } d) _ = EditLog(DateOnly.FromDateTime(d)); };
        Grid.SetColumn(date, 1); header.Children.Add(date);
        var theme = Button(Glyph(Pref.Light ? "\uE708" : "\uE706"), () => { Pref.Light = !Pref.Light; SavePreferences(); }); theme.Margin = new Thickness(12, 0); theme.Background = Brushes.Transparent; ToolTip.SetTip(theme, Tr("Switch appearance")); Grid.SetColumn(theme, 2); header.Children.Add(theme);
        var log = Button("＋  Log Today", () => EditLog(Today), true); log.Name = "LogToday"; Grid.SetColumn(log, 3); header.Children.Add(log);
        var windowButtons = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 2, Margin = new Thickness(12, 0, 0, 0), FlowDirection = FlowDirection.LeftToRight };
        foreach (var (symbol, label, action) in new (string, string, Action)[] { ("−", "Minimize", () => WindowState = WindowState.Minimized), ("□", "Maximize", ToggleMaximize), ("×", "Close", Close) })
        {
            var button = Button(symbol, action); button.Name = "Window" + label; button.Width = 32; button.Height = 32; button.Padding = new(0); button.Background = Brushes.Transparent; button.BorderThickness = new(0); ToolTip.SetTip(button, L(label, label == "Minimize" ? "تصغير" : label == "Maximize" ? "تكبير / استعادة" : "إغلاق")); windowButtons.Children.Add(button);
        }
        Grid.SetColumn(windowButtons, 4); header.Children.Add(windowButtons);
        right.Children.Add(new Border { BorderBrush = Line, BorderThickness = new(0, 0, 0, 1), Child = header });
        _pageHost = new ContentControl { Margin = new Thickness(32, 30, 32, 26), HorizontalContentAlignment = HorizontalAlignment.Stretch };
        var scroll = new ScrollViewer { Content = _pageHost, HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled }; Grid.SetRow(scroll, 1); right.Children.Add(scroll);
        _status = T("", 11, color: Muted); _status.Margin = new Thickness(32, 0); Grid.SetRow(_status, 2); right.Children.Add(_status);
        Content = root; RenderPage();
    }
    private void Navigate(string page) { _page = page; BuildShell(); }
    private void RenderPage()
    {
        foreach (var image in _images) image.Dispose(); _images.Clear();
        _pageHost.Content = _page switch
        {
            "Calendar" => CalendarPage(), "Weight" => WeightPage(), "Settings" => SettingsPage(), "Workouts" => WorkoutsPage(),
            "Measurements" => MeasurementsPage(), "Progress Photos" => PhotosPage(), "Goals & Records" => GoalsPage(),
            "Journey" => JourneyPage(), "Statistics" => StatisticsPage(), _ => DashboardPage()
        };
    }
    private void Refresh(string message = "Saved to your device") { Reload(); RenderPage(); _status.Text = Tr(message); }
    private void SavePreferences() { _repository.Save("preferences", "main", Pref); Reload(); BuildShell(); }
    private TextBlock T(string text, double size = 14, bool bold = false, IBrush? color = null) => new()
    {
        Text = Tr(text), FontSize = size, FontWeight = bold ? FontWeight.SemiBold : FontWeight.Normal,
        Foreground = color ?? Ink, VerticalAlignment = VerticalAlignment.Center, TextWrapping = TextWrapping.Wrap
    };
    private TextBlock Glyph(string glyph, double size = 18) => new() { Text = glyph, FontFamily = new FontFamily("Segoe Fluent Icons, Segoe MDL2 Assets"), FontSize = size, Foreground = Muted, VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Center, Width = size + 3 };
    private static StackPanel Stack(double spacing = 16) => new() { Spacing = spacing };
    private Border Card(Control child, double padding = 24) => new() { Background = Surface, BorderBrush = Line, BorderThickness = new(1), CornerRadius = new(22), Padding = new(padding), Child = child };
    private Button Button(object content, Action action, bool primary = false)
    {
        var b = new Button { Content = content is string text ? Tr(text) : content, VerticalAlignment = VerticalAlignment.Center };
        if (primary) b.Classes.Add("primary"); else { b.Background = Raised; b.Foreground = Ink; b.BorderBrush = Line; }
        b.Click += async (_, _) => { try { action(); } catch (Exception ex) { await Error(ex.Message); } }; return b;
    }
    private Button Button(object content, Func<Task> action, bool primary = false)
    {
        var b = new Button { Content = content is string text ? Tr(text) : content, VerticalAlignment = VerticalAlignment.Center };
        if (primary) b.Classes.Add("primary"); else { b.Background = Raised; b.Foreground = Ink; b.BorderBrush = Line; }
        b.Click += async (_, _) => { try { await action(); } catch (Exception ex) { await Error(ex.Message); } }; return b;
    }
    private Grid Columns(params Control[] children)
    {
        var g = new Grid { ColumnDefinitions = new(string.Join(",", children.Select(_ => "*"))), ColumnSpacing = 12 };
        for (int i = 0; i < children.Length; i++) { Grid.SetColumn(children[i], i); g.Children.Add(children[i]); } return g;
    }
    private Grid Split(Control left, Control right, string sizes = "*,Auto")
    {
        var g = new Grid { ColumnDefinitions = new(sizes), ColumnSpacing = 20 };
        g.Children.Add(left); Grid.SetColumn(right, 1); g.Children.Add(right); return g;
    }
    private StackPanel Heading(string eyebrow, string title, string subtitle)
    {
        var s = Stack(9); if (eyebrow.Length > 0) s.Children.Add(T(eyebrow.ToUpperInvariant(), 10, true, Muted));
        s.Children.Add(T(title, 28, true)); if (subtitle.Length > 0) s.Children.Add(T(subtitle, 13, color: Muted)); return s;
    }
    private Border Stat(string label, string value, string? note = null)
    {
        var s = Stack(15); s.Children.Add(T(label, 12, color: Muted)); s.Children.Add(T(value, 23, true));
        if (note != null) s.Children.Add(T(note, 11, color: Muted)); return Card(s, 22);
    }
    private Control Empty(string title, string description, string? action = null, Func<Task>? run = null)
    {
        var s = Stack(12); s.Margin = new Thickness(12, 25); s.Children.Add(T(title, 20, true)); s.Children.Add(T(description, 14, color: Muted));
        if (action != null && run != null) s.Children.Add(Button(action, run, true)); return Card(s);
    }
    private string Weight(double kg) => $"{(Pref.Pounds ? kg * 2.2046226218 : kg):0.0} {Tr(Pref.Pounds ? "lb" : "kg")}";
    private string Length(double cm) => $"{(Pref.Inches ? cm / 2.54 : cm):0.0} {Tr(Pref.Inches ? "in" : "cm")}";
    private double DisplayWeight(double kg) => Pref.Pounds ? kg * 2.2046226218 : kg;
    private double ToKg(double value) => Pref.Pounds ? value / 2.2046226218 : value;
    private List<WeightEntry> Weights => _data.Weights.OrderBy(x => x.Date).ToList();
    private Task Error(string message) => Message("Unable to complete", message);
    private async Task Message(string title, string message)
    {
        var dialog = Dialog(title); var s = Stack(); s.Children.Add(T(message, 14)); s.Children.Add(Button("Close", () => dialog.Close())); dialog.Content = s; await dialog.ShowDialog(this);
    }
    private Window Dialog(string title)
    {
        var dialog = new Window { Title = Tr(title), Width = 580, MaxHeight = 860, SizeToContent = SizeToContent.Height, CanResize = false, WindowStartupLocation = WindowStartupLocation.CenterOwner, Background = BackgroundBrush, Foreground = Ink, Padding = new Thickness(28), RequestedThemeVariant = RequestedThemeVariant, FlowDirection = FlowDirection, FontFamily = FontFamily, SystemDecorations = SystemDecorations.None, BorderBrush = Line, BorderThickness = new(1) };
        dialog.PointerPressed += (_, e) => { if (e.GetPosition(dialog).Y < 28) DragChrome(dialog, e); };
        return dialog;
    }
    private void ToggleMaximize() => WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
    private static void DragChrome(Window window, PointerPressedEventArgs e)
    {
        if (!e.GetCurrentPoint(window).Properties.IsLeftButtonPressed || e.Source is not Visual visual) return;
        if (visual.GetSelfAndVisualAncestors().OfType<Control>().Any(x => x is Avalonia.Controls.Button or TextBox or CalendarDatePicker)) return;
        if (e.ClickCount == 2 && window.CanResize) window.WindowState = window.WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
        else window.BeginMoveDrag(e);
    }
    private void ResizeFromEdge(PointerPressedEventArgs e)
    {
        if (WindowState != WindowState.Normal || !e.GetCurrentPoint(this).Properties.IsLeftButtonPressed) return;
        var p = e.GetPosition(this); bool left = p.X < 5, right = p.X > Bounds.Width - 5, top = p.Y < 5, bottom = p.Y > Bounds.Height - 5;
        WindowEdge? edge = (left, right, top, bottom) switch { (true, _, true, _) => WindowEdge.NorthWest, (_, true, true, _) => WindowEdge.NorthEast, (true, _, _, true) => WindowEdge.SouthWest, (_, true, _, true) => WindowEdge.SouthEast, (true, _, _, _) => WindowEdge.West, (_, true, _, _) => WindowEdge.East, (_, _, true, _) => WindowEdge.North, (_, _, _, true) => WindowEdge.South, _ => null };
        if (edge is { } resize) { BeginResizeDrag(resize, e); e.Handled = true; }
    }
    private async Task<bool> Confirm(string title, string text)
    {
        var dialog = Dialog(title); var s = Stack(); s.Children.Add(T(title, 24, true)); s.Children.Add(T(text, color: Muted));
        s.Children.Add(Columns(Button("Cancel", () => dialog.Close(false)), Button("Delete", () => dialog.Close(true), true))); dialog.Content = s; return await dialog.ShowDialog<bool>(this);
    }
    private async Task DeleteRecord(string kind, string id)
    {
        if (await Confirm("Delete this entry?", "This removes the selected entry from this device.")) { _repository.Delete(kind, id); Refresh("Entry deleted"); }
    }
}
