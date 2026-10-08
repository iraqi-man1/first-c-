using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
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
    private Button? _logButton;
    private readonly List<IDisposable> _images = [];
    private DateOnly Today => DateOnly.FromDateTime(_clock.GetLocalNow().DateTime);
    private IBrush BackgroundBrush => Brush(Pref.Light ? "#F3F6F8" : "#101820");
    private IBrush Surface => Brush(Pref.Light ? "#FFFFFF" : "#1B2832");
    private IBrush Raised => Brush(Pref.Light ? "#E6EDF1" : "#24333E");
    private IBrush InputSurface => Brush(Pref.Light ? "#F3F6F8" : "#15232D");
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
        using (var icon = AssetLoader.Open(new Uri("avares://Fitlog/Assets/fitlog.ico"))) Icon = new WindowIcon(icon);
        // Keep the native caption and frame: Windows owns Snap, restore-drag, edge resizing and shortcuts.
        SystemDecorations = SystemDecorations.Full;
        Title = "fitlog — Your fitness, day by day"; Width = 1360; Height = 920; MinWidth = 900; MinHeight = 640;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;
        Reload(); _heatFilter = Pref.Heatmap; BuildShell();
        Opened += (_, _) => { _clockTimer.Start(); WindowsChrome.Apply(this, Pref.Light); };
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
        var root = new Grid { ColumnDefinitions = new($"{UiMetrics.SidebarWidth},*") };
        var sidebar = new Grid { RowDefinitions = new("80,*,56"), Margin = new Thickness(UiMetrics.Lg, 0) };
        var brand = new StackPanel { Orientation = Orientation.Horizontal, Spacing = UiMetrics.Md, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(UiMetrics.Sm, 0) };
        brand.Children.Add(new Border { Width = 44, Height = 44, CornerRadius = new(UiMetrics.ControlRadius), ClipToBounds = true, Background = Surface, Child = new Image { Source = _brandLogo, Stretch = Stretch.Uniform, FlowDirection = FlowDirection.LeftToRight } });
        brand.Children.Add(T("fitlog", 22, true)); sidebar.Children.Add(brand);
        var navigation = Stack(UiMetrics.Xs); navigation.Margin = new Thickness(0, UiMetrics.Lg, 0, 0);
        (string? heading, (string label, string glyph)[] items)[] groups =
        [
            ("Today", [("Dashboard", "\uE9D9"), ("Calendar", "\uE787")]),
            ("Plan", [("Workouts", "\uE7C1"), ("Nutrition", "\uEC09")]),
            ("Body", [("Weight", "\uE9D5"), ("Measurements", "\uE9D2"), ("Progress Photos", "\uE722")]),
            ("Progress", [("Goals & Records", "\uE7C1"), ("Journey", "\uE70B"), ("Statistics", "\uE9D2")]),
            (null, [("Settings", "\uE713")]),
        ];
        foreach (var (heading, items) in groups)
        {
            if (heading != null) { var title = T(heading, 11, true, Muted); title.Margin = new Thickness(UiMetrics.Md, UiMetrics.Sm, 0, UiMetrics.Xs); navigation.Children.Add(title); }
            foreach (var (label, glyph) in items)
            {
                var active = _page == label;
                var icon = Glyph(glyph); var text = T(label, 15, active, active ? Ink : Muted); if (active) icon.Foreground = Accent;
                var content = new StackPanel { Orientation = Orientation.Horizontal, Spacing = UiMetrics.Md, VerticalAlignment = VerticalAlignment.Center };
                content.Children.Add(icon); content.Children.Add(text);
                var button = Button(content, () => Navigate(label)); button.Classes.Add("nav"); button.Name = "Nav" + label.Replace(" ", "").Replace("&", "");
                button.Background = active ? Raised : Brushes.Transparent;
                navigation.Children.Add(button);
            }
        }
        var navScroll = new ScrollViewer { Content = navigation }; Grid.SetRow(navScroll, 1); sidebar.Children.Add(navScroll);
        var local = T("●   Saved on this device", 11, color: Muted); local.Margin = new Thickness(UiMetrics.Md, 0); local.VerticalAlignment = VerticalAlignment.Center; Grid.SetRow(local, 2); sidebar.Children.Add(local);
        root.Children.Add(new Border { Background = Surface, BorderBrush = Line, BorderThickness = new(0, 0, 1, 0), Child = sidebar });
        var right = new Grid { RowDefinitions = new($"{UiMetrics.HeaderHeight},*,28") }; Grid.SetColumn(right, 1); root.Children.Add(right);
        var header = new Grid { ColumnDefinitions = new("*,Auto,Auto,Auto"), ColumnSpacing = UiMetrics.Md, Margin = new Thickness(UiMetrics.PageGutter, 0), Background = Brushes.Transparent };
        var titleGroup = Stack(UiMetrics.Xs); titleGroup.VerticalAlignment = VerticalAlignment.Center; titleGroup.Children.Add(T(_page, 14, true));
        _clockLabel = T(_clock.GetLocalNow().ToString("dd MMM yyyy · HH:mm:ss", UiCulture), 11, color: Muted); _clockLabel.Name = "LocalClock"; titleGroup.Children.Add(_clockLabel); header.Children.Add(titleGroup);
        var date = new CalendarDatePicker { Width = 160, VerticalAlignment = VerticalAlignment.Center, Name = "OpenDate", SelectedDate = Today.ToDateTime(TimeOnly.MinValue), DisplayDateEnd = Today.ToDateTime(TimeOnly.MinValue), Background = Surface, BorderBrush = Line };
        var selectedHeaderDate = Today;
        date.SelectedDateChanged += (_, _) =>
        {
            if (date.SelectedDate is not { } value) return;
            var selected = DateOnly.FromDateTime(value);
            if (selected == selectedHeaderDate) return;
            selectedHeaderDate = selected; _ = EditLog(selected);
        };
        Grid.SetColumn(date, 1); header.Children.Add(date);
        var theme = Button(Glyph(Pref.Light ? "\uE708" : "\uE706", 18), () => { Pref.Light = !Pref.Light; SavePreferences(); }); theme.Classes.Add("icon"); theme.Name = "SwitchAppearance"; theme.Background = Brushes.Transparent; theme.HorizontalContentAlignment = HorizontalAlignment.Center; theme.VerticalContentAlignment = VerticalAlignment.Center; ToolTip.SetTip(theme, Tr("Switch appearance")); Grid.SetColumn(theme, 2); header.Children.Add(theme);
        _logButton = Button(LogLabel, () => EditLog(Today), true); _logButton.Name = "LogToday"; Grid.SetColumn(_logButton, 3); header.Children.Add(_logButton);
        right.Children.Add(new Border { BorderBrush = Line, BorderThickness = new(0, 0, 0, 1), Child = header });
        _pageHost = new ContentControl { Margin = UiMetrics.PageMargin, HorizontalContentAlignment = HorizontalAlignment.Stretch };
        var scroll = new ScrollViewer { Content = _pageHost, HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled }; Grid.SetRow(scroll, 1); right.Children.Add(scroll);
        _status = T("", 11, color: Muted); _status.Margin = new Thickness(UiMetrics.PageGutter, 0); Grid.SetRow(_status, 2); right.Children.Add(_status);
        Content = root; RenderPage(); WindowsChrome.Apply(this, Pref.Light);
    }
    private void Navigate(string page) { _page = page; BuildShell(); }
    private void RenderPage()
    {
        foreach (var image in _images) image.Dispose(); _images.Clear();
        _pageHost.Content = _page switch
        {
            "Calendar" => CalendarPage(), "Weight" => WeightPage(), "Settings" => SettingsPage(), "Workouts" => WorkoutsPage(), "Nutrition" => NutritionPage(),
            "Measurements" => MeasurementsPage(), "Progress Photos" => PhotosPage(), "Goals & Records" => GoalsPage(),
            "Journey" => JourneyPage(), "Statistics" => StatisticsPage(), _ => DashboardPage()
        };
    }
    private void Refresh(string message = "Saved to your device") { Reload(); RenderPage(); UpdateLogButton(); _status.Text = Tr(message); }
    private void SavePreferences() { _repository.Save("preferences", "main", Pref); Reload(); BuildShell(); }
    private TextBlock T(string text, double size = 14, bool bold = false, IBrush? color = null) => new()
    {
        Text = Tr(text), FontSize = size, FontWeight = bold ? FontWeight.SemiBold : FontWeight.Normal,
        Foreground = color ?? Ink, VerticalAlignment = VerticalAlignment.Center, TextWrapping = TextWrapping.Wrap
    };
    private TextBlock Glyph(string glyph, double size = UiMetrics.IconSize) => new() { Text = glyph, FontFamily = new FontFamily("Segoe Fluent Icons, Segoe MDL2 Assets"), FontSize = size, Foreground = Muted, VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Center, Width = UiMetrics.Xl };
    private static StackPanel Stack(double spacing = UiMetrics.Lg) => new() { Spacing = spacing };
    private Border Card(Control child, double padding = UiMetrics.Xl) => new() { Background = Surface, BorderBrush = Line, BorderThickness = new(1), CornerRadius = new(UiMetrics.CardRadius), Padding = new(padding), Child = child };
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
    private string LogLabel => _data.Logs.Any(x => x.Date == Today) ? "Edit today" : "＋  Log Today";
    private void UpdateLogButton() { if (_logButton != null) _logButton.Content = Tr(LogLabel); }
    private Button Segment(string label, bool selected, Action action)
    {
        var b = Button(label, action); b.Background = selected ? Accent : Brushes.Transparent; b.BorderBrush = Brushes.Transparent;
        b.Foreground = selected ? Brush("#101820") : Muted; b.FontWeight = selected ? FontWeight.SemiBold : FontWeight.Normal; return b;
    }
    private Button Destructive(Button button) { button.Foreground = Brush("#E99B92"); button.BorderBrush = Brush("#E99B92"); return button; }
    private Grid Columns(params Control[] children) => ColumnsWithMinimum(children.Length == 2 ? 320 : children.Length == 3 ? 190 : 200, children);
    private Grid ColumnsWithMinimum(double minimum, params Control[] children)
    {
        var g = new Grid { ColumnSpacing = UiMetrics.Md, RowSpacing = UiMetrics.Md };
        foreach (var child in children) g.Children.Add(child);
        var columns = 0;
        void Arrange(double width)
        {
            var next = width <= 0 ? children.Length : Math.Clamp((int)((width + UiMetrics.Md) / (minimum + UiMetrics.Md)), 1, children.Length);
            if (next == columns) return;
            columns = next;
            g.ColumnDefinitions = new(string.Join(",", Enumerable.Repeat("*", columns)));
            g.RowDefinitions = new(string.Join(",", Enumerable.Repeat("Auto", (children.Length + columns - 1) / columns)));
            for (var i = 0; i < children.Length; i++) { Grid.SetColumn(children[i], i % columns); Grid.SetRow(children[i], i / columns); }
        }
        Arrange(0);
        g.SizeChanged += (_, e) => Arrange(e.NewSize.Width);
        return g;
    }
    private Grid Split(Control left, Control right, string sizes = "*,Auto")
    {
        var g = new Grid { ColumnSpacing = UiMetrics.Lg, RowSpacing = UiMetrics.Lg };
        g.Children.Add(left); g.Children.Add(right);
        bool? stacked = null;
        void Arrange(double width)
        {
            var threshold = left is TextBlock && right is Button ? 400 : 680;
            var next = width > 0 && width < threshold && (left is not TextBlock || right is not TextBlock);
            if (next == stacked) return;
            stacked = next;
            g.ColumnDefinitions = new(next ? "*" : sizes);
            g.RowDefinitions = new(next ? "Auto,Auto" : "Auto");
            Grid.SetColumn(right, next ? 0 : 1); Grid.SetRow(right, next ? 1 : 0);
            if (right is Button button) button.HorizontalAlignment = next ? HorizontalAlignment.Left : HorizontalAlignment.Center;
        }
        Arrange(0);
        g.SizeChanged += (_, e) => Arrange(e.NewSize.Width);
        return g;
    }
    private StackPanel Heading(string eyebrow, string title, string subtitle)
    {
        var s = Stack(UiMetrics.Sm); if (eyebrow.Length > 0) s.Children.Add(T(eyebrow.ToUpperInvariant(), 10, true, Muted));
        s.Children.Add(T(title, 28, true)); if (subtitle.Length > 0) s.Children.Add(T(subtitle, 13, color: Muted)); return s;
    }
    private Border Stat(string label, string value, string? note = null)
    {
        var s = Stack(UiMetrics.Md); s.Children.Add(T(label, 12, color: Muted)); s.Children.Add(T(value, 23, true));
        if (note != null) s.Children.Add(T(note, 11, color: Muted)); return Card(s);
    }
    private Control Empty(string title, string description, string? action = null, Func<Task>? run = null)
    {
        var s = Stack(UiMetrics.Md); s.Margin = new Thickness(UiMetrics.Md, UiMetrics.Xl); s.Children.Add(T(title, 20, true)); s.Children.Add(T(description, 14, color: Muted));
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
        var dialog = new Window { Title = Tr(title), Icon = Icon, Width = 580, MaxHeight = 860, SizeToContent = SizeToContent.Height, CanResize = false, WindowStartupLocation = WindowStartupLocation.CenterOwner, Background = BackgroundBrush, Foreground = Ink, Padding = new Thickness(UiMetrics.Xl), RequestedThemeVariant = RequestedThemeVariant, FlowDirection = FlowDirection, FontFamily = FontFamily, SystemDecorations = SystemDecorations.Full };
        dialog.Opened += (_, _) => WindowsChrome.Apply(dialog, Pref.Light);
        return dialog;
    }
    private async Task<bool> Confirm(string title, string text)
    {
        var dialog = Dialog(title); var s = Stack(); s.Children.Add(T(title, 24, true)); s.Children.Add(T(text, color: Muted));
        var actions = new StackPanel { Orientation = Orientation.Horizontal, Spacing = UiMetrics.Md, HorizontalAlignment = HorizontalAlignment.Right };
        var cancel = Button("Cancel", () => dialog.Close(false)); cancel.IsCancel = true; actions.Children.Add(cancel); actions.Children.Add(Destructive(Button("Delete", () => dialog.Close(true))));
        s.Children.Add(actions); dialog.Content = s; return await dialog.ShowDialog<bool>(this);
    }
    private async Task DeleteRecord(string kind, string id)
    {
        if (await Confirm("Delete this entry?", "This removes the selected entry from this device.")) { _repository.Delete(kind, id); Refresh("Entry deleted"); }
    }
}
