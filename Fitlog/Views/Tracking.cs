using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Fitlog.Controls;
using Fitlog.Models;

namespace Fitlog;
public sealed partial class MainWindow
{
    private Control Chart(List<WeightEntry> entries, double height = 330) => new WeightChart { Entries = entries, Height = height, Pounds = Pref.Pounds, Light = Pref.Light, Arabic = Arabic, FlowDirection = FlowDirection.LeftToRight, HorizontalAlignment = HorizontalAlignment.Stretch };
    private Control WeightPage()
    {
        var page = Stack(UiMetrics.Xl); page.Children.Add(Split(Heading("Body / trend", "Weight progress", "Track change at your own pace. Weigh-ins are always optional."), Button("＋  Add weight", () => EditWeight(), true)));
        var list = Weights; var values = list.Select(x => x.Kilograms).ToList();
        page.Children.Add(Columns(Stat("Current weight", values.Count > 0 ? Weight(values[^1]) : "—"), Stat("Starting weight", values.Count > 0 ? Weight(values[0]) : "—"), Stat("Lowest", values.Count > 0 ? Weight(values.Min()) : "—"), Stat("Highest", values.Count > 0 ? Weight(values.Max()) : "—")));
        var graph = Stack(UiMetrics.Xl); var filters = new WrapPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
        foreach (var range in new[] { "Week", "Month", "3 months", "6 months", "Year", "All" }) filters.Children.Add(Segment(range, range == _weightRange, () => { _weightRange = range; RenderPage(); }));
        graph.Children.Add(Split(Heading("History", "Weight trend", ""), new Border { Background = Raised, CornerRadius = new(12), Child = filters, Padding = new(4) }, "*,1.5*"));
        int days = _weightRange switch { "Week" => 7, "Month" => 30, "3 months" => 90, "6 months" => 180, "Year" => 365, _ => 3650000 };
        var filtered = list.Where(x => Today.DayNumber - x.Date.DayNumber <= days && x.Date <= Today).ToList(); graph.Children.Add(Chart(filtered)); page.Children.Add(Card(graph));
        string Change(double? value) => value is { } v ? $"{(v > 0 ? "+" : "")}{Weight(v)}" : "—";
        page.Children.Add(Columns(Stat("Total change", Change(values.Count >= 2 ? values[^1] - values[0] : null)), Stat("Last 7 days", Change(Metrics.WeightChange(list, 7, Today))), Stat("Last 30 days", Change(Metrics.WeightChange(list, 30, Today))), Stat("Last 90 days", Change(Metrics.WeightChange(list, 90, Today)))));
        var history = Stack(UiMetrics.Lg); history.Children.Add(T("Weight history", 26, true));
        if (list.Count == 0) history.Children.Add(T("No weigh-ins yet. Add your first weight to start your trend.", color: Muted));
        foreach (var entry in list.AsEnumerable().Reverse())
        {
            var actions = new StackPanel { Orientation = Orientation.Horizontal, Spacing = UiMetrics.Sm }; actions.Children.Add(Button("Edit", () => EditWeight(entry))); actions.Children.Add(Destructive(Button("Delete", () => DeleteRecord("weight", entry.Date.ToString("yyyy-MM-dd")))));
            history.Children.Add(Row(DateText(entry.Date, "ddd, dd MMM yyyy"), Weight(entry.Kilograms), actions));
        }
        page.Children.Add(Card(history)); return page;
    }
    private Control Row(string label, string value, Control? action = null)
    {
        var g = new Grid { ColumnDefinitions = new("*,*,Auto"), ColumnSpacing = 14, MinHeight = 54 }; g.Children.Add(T(label, 13)); var v = T(value, 13, color: Muted); Grid.SetColumn(v, 1); g.Children.Add(v); if (action != null) { Grid.SetColumn(action, 2); g.Children.Add(action); }
        return new Border { BorderBrush = Line, BorderThickness = new(0, 0, 0, 1), Padding = new Thickness(0, 5), Child = g };
    }
    private Control MeasurementsPage()
    {
        var page = Stack(UiMetrics.Xl); page.Children.Add(Split(Heading("Beyond the scale", "Measurements", "Notice the changes that a number on the scale can miss."), Button("＋  Add measurements", () => EditMeasurement(), true)));
        var items = _data.Measurements.OrderByDescending(x => x.Date).ToList(); var latest = items.FirstOrDefault();
        page.Children.Add(Columns(Stat("Waist", latest == null ? "—" : Length(latest.Waist)), Stat("Chest", latest == null ? "—" : Length(latest.Chest)), Stat("Hips", latest == null ? "—" : Length(latest.Hips))));
        var rows = Stack(UiMetrics.Md); rows.Children.Add(T("Measurement history", 24, true));
        if (items.Count == 0) rows.Children.Add(T("No measurements yet. All measurements are optional.", color: Muted));
        foreach (var x in items)
        {
            var actions = new StackPanel { Orientation = Orientation.Horizontal, Spacing = UiMetrics.Sm }; actions.Children.Add(Button("Edit", () => EditMeasurement(x))); actions.Children.Add(Destructive(Button("Delete", () => DeleteRecord("measurement", x.Date.ToString("yyyy-MM-dd")))));
            rows.Children.Add(Row(DateText(x.Date, "dd MMM yyyy"), $"{Tr("Waist")} {Length(x.Waist)} · {Tr("Chest")} {Length(x.Chest)} · {Tr("Hips")} {Length(x.Hips)}", actions));
        }
        page.Children.Add(Card(rows)); return page;
    }
    private Control JourneyPage()
    {
        var page = Stack(UiMetrics.Xl); page.Children.Add(Split(Heading("Your story", "Journey", "The effort, the rest days, and everything in between."), Button("＋  Add check-in", () => EditLog(Today), true)));
        if (_data.Logs.Count == 0) page.Children.Add(Empty("Every journey has a day one", "Use your daily check-in to record how you feel, your training, and your notes.", "Log Today", () => EditLog(Today)));
        foreach (var x in _data.Logs.OrderByDescending(x => x.Date))
        {
            var content = Stack(UiMetrics.Lg); content.Children.Add(Split(T(DateText(x.Date, "dddd, dd MMMM yyyy"), 18, true), Button("Edit", () => EditLog(x.Date))));
            content.Children.Add(T(LogSummary(x), color: Muted));
            if (x.Trained && !string.IsNullOrWhiteSpace(x.Workout)) content.Children.Add(T($"{x.Workout}{(x.PerformedExercises.Count > 0 ? " · " + string.Join("، ", x.PerformedExercises) : "")}", 13, color: Muted));
            content.Children.Add(T(L($"{x.Steps:N0} steps · {x.Water:0.#} L water · {x.Sleep:0.#} h sleep · Energy {x.Energy}/5", $"{x.Steps:N0} خطوة · {x.Water:0.#} لتر ماء · {x.Sleep:0.#} ساعة نوم · الطاقة {x.Energy}/5"), 12, color: Muted));
            if (!string.IsNullOrWhiteSpace(x.Notes)) content.Children.Add(T(x.Notes)); page.Children.Add(Card(content));
        }
        return page;
    }
    private Control StatisticsPage()
    {
        var page = Stack(UiMetrics.Xl); page.Children.Add(Heading("The bigger picture", "Statistics", "Your consistency, measured over time. Totals use your recorded check-ins."));
        var logs = _data.Logs; page.Children.Add(Columns(Stat("Logged days", logs.Count.ToString()), Stat("Gym sessions", logs.Count(x => x.Trained).ToString()), Stat("Diet adherence", logs.Count == 0 ? "—" : $"{logs.Average(x => x.EffectiveDietCompletion):0}%")));
        page.Children.Add(Columns(Stat("Total steps", logs.Sum(x => (long)x.Steps).ToString("N0")), Stat("Average sleep", logs.Count == 0 ? "—" : $"{logs.Average(x => x.Sleep):0.0} {L("h", "ساعة")}", "per logged day"), Stat("Average water", logs.Count == 0 ? "—" : $"{logs.Average(x => x.Water):0.0} {L("L", "لتر")}", "per logged day")));
        var monthly = Stack(UiMetrics.Xl); monthly.Children.Add(T("Monthly activity", 25, true));
        for (int i = 5; i >= 0; i--) { var month = Today.AddMonths(-i); var entries = logs.Where(x => x.Date.Year == month.Year && x.Date.Month == month.Month).ToList(); var s = Stack(UiMetrics.Sm); s.Children.Add(Split(T(DateText(month, "MMMM yyyy")), T($"{entries.Count(x => x.Trained)} {Tr("Gym")} · {Tr("Diet")} {(entries.Count == 0 ? 0 : entries.Average(x => x.EffectiveDietCompletion)):0}%", 12, color: Muted))); s.Children.Add(new ProgressBar { Value = entries.Count(x => x.Trained), Maximum = DateTime.DaysInMonth(month.Year, month.Month), Height = 8, Foreground = Accent, Background = Raised }); monthly.Children.Add(s); }
        page.Children.Add(Card(monthly)); return page;
    }
}
