using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Fitlog.Models;

namespace Fitlog;
public sealed partial class MainWindow
{
    private Control DashboardPage()
    {
        var page = Stack(UiMetrics.Xl);
        var today = _data.Logs.FirstOrDefault(x => x.Date == Today);
        var hero = Stack(UiMetrics.Lg); hero.Children.Add(T(DateText(Today, "dddd, MMMM dd").ToUpperInvariant(), 10, true, Muted));
        hero.Children.Add(T("Today", 42, true));
        hero.Children.Add(T(today == null ? "A little progress, every day. Start with a check-in." : LogSummary(today), color: Muted));
        var log = Button("＋  Log Today", () => EditLog(Today), true); log.Margin = new Thickness(0, UiMetrics.Sm, 0, 0); log.HorizontalAlignment = HorizontalAlignment.Left; hero.Children.Add(log);
        var weekStart = Metrics.WeekStart(Today, Pref.SundayFirst);
        var weekLogs = _data.Logs.Where(x => x.Date >= weekStart && x.Date <= Today).ToList();
        var count = weekLogs.Count(x => x.Trained);
        var goal = Stack(UiMetrics.Lg); goal.Children.Add(T("THIS WEEK", 10, true, Muted)); goal.Children.Add(T("Weekly gym goal", 18, true));
        var weeklyValue = new StackPanel { Orientation = Orientation.Horizontal, Spacing = UiMetrics.Sm }; weeklyValue.Children.Add(T(count.ToString(), 36, true)); weeklyValue.Children.Add(T(L($"/ {Pref.WeeklyGoal} gym sessions", $"/ {Pref.WeeklyGoal} جلسات تمرين"), 13, color: Muted)); goal.Children.Add(weeklyValue);
        goal.Children.Add(new ProgressBar { Value = count, Maximum = Pref.WeeklyGoal, Height = 6, Foreground = Accent, Background = Raised });
        goal.Children.Add(new Border { Height = 1, Background = Line, Margin = new Thickness(0, 4) });
        goal.Children.Add(Split(T("Diet adherence", 12, color: Muted), T(weekLogs.Count == 0 ? "—" : $"{weekLogs.Average(x => x.EffectiveDietCompletion):0}%", 12, true)));
        page.Children.Add(Split(Card(hero), Card(goal), "1.75*,*"));
        var activity = Stack(UiMetrics.Xl); activity.Children.Add(Heading("", "Yearly activity", "Every square is a day. Select one to see the full story."));
        var filters = new StackPanel { Orientation = Orientation.Horizontal, Spacing = UiMetrics.Xs };
        foreach (var filter in new[] { "Overall", "Gym", "Diet", "Weight logging" })
        {
            var b = Button(filter, () => { _heatFilter = filter; RenderPage(); }); b.Background = _heatFilter == filter ? Line : Brushes.Transparent; filters.Children.Add(b);
        }
        var year = new StackPanel { Orientation = Orientation.Horizontal, Spacing = UiMetrics.Lg };
        year.Children.Add(Button("‹", () => { _year--; RenderPage(); })); year.Children.Add(T(_year.ToString(), 14, true)); year.Children.Add(Button("›", () => { if (_year < 9998) _year++; RenderPage(); }));
        activity.Children.Add(Split(new Border { CornerRadius = new(UiMetrics.ControlRadius), Background = Raised, Padding = new(UiMetrics.Xs), Child = filters, HorizontalAlignment = HorizontalAlignment.Left }, year));
        activity.Children.Add(BuildHeatmap());
        var legend = new StackPanel { Orientation = Orientation.Horizontal, Spacing = UiMetrics.Sm, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, UiMetrics.Lg, 0, 0) };
        legend.Children.Add(T("Less", 10, color: Muted)); foreach (var color in new[] { "#24333E", "#415B6B", "#7699AD", "#ADCDDB" }) legend.Children.Add(new Border { Width = 10, Height = 10, CornerRadius = new(2), Background = Brush(color), VerticalAlignment = VerticalAlignment.Center }); legend.Children.Add(T("More", 10, color: Muted)); activity.Children.Add(legend);
        page.Children.Add(Card(activity));
        var monthLogs = _data.Logs.Where(x => x.Date.Year == Today.Year && x.Date.Month == Today.Month && x.Date <= Today).ToList();
        var dietPercent = monthLogs.Count == 0 ? "—" : $"{monthLogs.Average(x => x.EffectiveDietCompletion):0}%";
        page.Children.Add(Columns(Stat("Current weight", Weights.Count > 0 ? Weight(Weights[^1].Kilograms) : "—"), Stat("Gym this month", monthLogs.Count(x => x.Trained).ToString()), Stat("Current gym streak", L($"{Metrics.Streak(_data.Logs, Today)} days", $"{Metrics.Streak(_data.Logs, Today)} أيام")), Stat("Diet adherence", dietPercent, "of logged days this month")));
        var trend = Stack(UiMetrics.Lg); trend.Children.Add(T("Weight trend", 25, true)); trend.Children.Add(Chart(Weights.TakeLast(12).ToList(), 210));
        var goals = Stack(UiMetrics.Lg); goals.Children.Add(T("Your goals", 25, true)); goals.Children.Add(T($"{Tr("Target weight")}   {Weight(Pref.TargetWeight)}", color: Muted)); goals.Children.Add(T($"{Tr("Monthly gym goal")}   {monthLogs.Count(x => x.Trained)} / {Pref.MonthlyGoal}", color: Muted)); goals.Children.Add(Button("View goals  →", () => Navigate("Goals & Records")));
        page.Children.Add(Split(Card(trend), Card(goals), "1.4*,*")); return page;
    }
    private Control BuildHeatmap()
    {
        var start = new DateOnly(_year, 1, 1); var end = new DateOnly(_year, 12, 31); var origin = Metrics.WeekStart(start, Pref.SundayFirst);
        var weeks = (end.DayNumber - origin.DayNumber) / 7 + 1;
        var grid = new Grid { FlowDirection = FlowDirection.LeftToRight, ColumnDefinitions = new("64," + string.Join(",", Enumerable.Repeat("*", weeks))), RowDefinitions = new("26,20,20,20,20,20,20,20"), ColumnSpacing = 3, RowSpacing = 3, MinWidth = 720 };
        foreach (int row in new[] { 1, 3, 5 }) { var day = origin.AddDays(row - 1); var label = T(DateText(day, "ddd"), 11, color: Muted); Grid.SetRow(label, row); grid.Children.Add(label); }
        for (var month = 1; month <= 12; month++) { var d = new DateOnly(_year, month, 1); var label = T(Arabic ? month.ToString() : DateText(d, "MMM"), 11, color: Muted); Grid.SetColumn(label, (d.DayNumber - origin.DayNumber) / 7 + 1); Grid.SetColumnSpan(label, Math.Min(3, weeks - (d.DayNumber - origin.DayNumber) / 7)); grid.Children.Add(label); }
        var logs = _data.Logs.ToDictionary(x => x.Date); var weights = _data.Weights.Select(x => x.Date).ToHashSet();
        for (var day = start; day <= end; day = day.AddDays(1))
        {
            logs.TryGetValue(day, out var log); int value = _heatFilter switch { "Gym" => log?.Trained == true ? Math.Max(1, (int)Math.Ceiling((log.EffectiveWorkoutCompletion ?? 100) * 3.0 / 100)) : 0, "Diet" => (int)Math.Ceiling((log?.EffectiveDietCompletion ?? 0) * 3.0 / 100), "Weight logging" => weights.Contains(day) ? 3 : 0, _ => (log?.Trained == true ? 1 : 0) + (log?.EffectiveDietCompletion > 0 ? 1 : 0) + (weights.Contains(day) ? 1 : 0) };
            var captured = day;
            var b = Button("", () => EditLog(captured)); b.Classes.Add("heatcell"); b.HorizontalAlignment = HorizontalAlignment.Stretch; b.Height = 20; b.Background = value switch { 1 => Brush("#415B6B"), 2 => Brush("#7699AD"), 3 => Brush("#ADCDDB"), _ => Raised }; b.IsHitTestVisible = day <= Today; b.Focusable = day <= Today;
            ToolTip.SetTip(b, $"{DateText(day)} · {(log == null ? L("No activity", "لا يوجد نشاط") : LogSummary(log))}");
            int offset = day.DayNumber - origin.DayNumber; Grid.SetColumn(b, offset / 7 + 1); Grid.SetRow(b, offset % 7 + 1); grid.Children.Add(b);
        }
        return new ScrollViewer { Content = grid, HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto, VerticalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled };
    }
    private Control CalendarPage()
    {
        var page = Stack(UiMetrics.Xl); page.Children.Add(Heading("Day by day", "Calendar", "A clear view of every check-in, rest day, and weigh-in."));
        var body = Stack(UiMetrics.Xl); var title = T(DateText(_month, "MMMM yyyy"), 20, true); title.HorizontalAlignment = HorizontalAlignment.Center;
        var header = new Grid { ColumnDefinitions = new("Auto,*,Auto") }; header.Children.Add(Button("‹", () => { _month = _month.AddMonths(-1); RenderPage(); })); Grid.SetColumn(title, 1); header.Children.Add(title); var next = Button("›", () => { _month = _month.AddMonths(1); RenderPage(); }); Grid.SetColumn(next, 2); header.Children.Add(next); body.Children.Add(header);
        var origin = Metrics.WeekStart(_month, Pref.SundayFirst); int offset = _month.DayNumber - origin.DayNumber; int days = DateTime.DaysInMonth(_month.Year, _month.Month); int rows = (offset + days + 6) / 7;
        var grid = new Grid { ColumnDefinitions = new("*,*,*,*,*,*,*"), RowDefinitions = new("28," + string.Join(",", Enumerable.Repeat("104", rows))), ColumnSpacing = 5, RowSpacing = 5 };
        for (int i = 0; i < 7; i++) { var label = T(DateText(origin.AddDays(i), "ddd").ToUpperInvariant(), 10, color: Muted); label.HorizontalAlignment = HorizontalAlignment.Center; Grid.SetColumn(label, i); grid.Children.Add(label); }
        for (int d = 1; d <= days; d++)
        {
            var date = new DateOnly(_month.Year, _month.Month, d); var entry = _data.Logs.FirstOrDefault(x => x.Date == date); var weight = _data.Weights.Any(x => x.Date == date); var photo = _data.Photos.Any(x => x.Date == date);
            var cell = new Grid { RowDefinitions = new("*,Auto") }; var num = T(d.ToString(), 12, true, date == Today ? Accent : Ink); num.VerticalAlignment = VerticalAlignment.Top; cell.Children.Add(num);
            var indicators = new StackPanel { Orientation = Orientation.Horizontal, Spacing = UiMetrics.Xs };
            if (entry != null) { indicators.Children.Add(T(entry.EffectiveStatus switch { Attendance.Training => "✓", Attendance.Rest => "☾", Attendance.Missed => "×", _ => "?" }, 13, true, entry.EffectiveStatus == Attendance.Missed ? Brush("#E99B92") : Accent)); indicators.Children.Add(T($"{entry.EffectiveDietCompletion}%", 11, color: Muted)); }
            if (weight) indicators.Children.Add(T("W", 11, color: Brush("#9AC9C1")));
            if (photo) indicators.Children.Add(T("P", 11, color: Brush("#BDAFD1")));
            Grid.SetRow(indicators, 1); cell.Children.Add(indicators);
            var b = Button(cell, () => EditLog(date)); b.HorizontalAlignment = HorizontalAlignment.Stretch; b.VerticalAlignment = VerticalAlignment.Stretch; b.HorizontalContentAlignment = HorizontalAlignment.Stretch; b.VerticalContentAlignment = VerticalAlignment.Stretch; b.BorderThickness = new(1); b.BorderBrush = date == Today ? Accent : Line; b.Background = date == Today ? Raised : Surface; b.Padding = new(12); b.CornerRadius = new(12); ToolTip.SetTip(b, $"{DateText(date)}{(entry == null ? "" : " · " + LogSummary(entry))}");
            Grid.SetColumn(b, (offset + d - 1) % 7); Grid.SetRow(b, (offset + d - 1) / 7 + 1); grid.Children.Add(b);
        }
        body.Children.Add(grid); page.Children.Add(Card(body)); page.Children.Add(T(L("✓ Training   ☾ Rest   × Missed gym   ? Not specified   % Diet adherence   W Weight   P Photo", "✓ تمرين   ☾ راحة   × ما رحت للجم   ? غير محدد   % الالتزام بالأكل   W وزن   P صورة"), 12, color: Muted)); return page;
    }
}
