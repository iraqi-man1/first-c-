using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using Fitlog.Models;

namespace Fitlog;
public sealed partial class MainWindow
{
    private Control GoalsPage()
    {
        var page = Stack(UiMetrics.Xl); page.Children.Add(Split(Heading("Keep moving forward", "Goals & records", "Set your own milestones and celebrate your progress."), Button("＋  Add goal", () => EditGoal(), true)));
        var start = Metrics.WeekStart(Today, Pref.SundayFirst); var weekly = _data.Logs.Count(x => x.Trained && x.Date >= start && x.Date <= Today);
        page.Children.Add(Columns(Stat("Target weight", Weight(Pref.TargetWeight)), Stat("Weekly gym goal", $"{weekly} / {Pref.WeeklyGoal}"), Stat("Current streak", L($"{Metrics.Streak(_data.Logs, Today)} days", $"{Metrics.Streak(_data.Logs, Today)} أيام"))));
        if (_data.Goals.Count == 0) page.Children.Add(Empty("Make it personal", "Set a strength, distance, or consistency goal and update your progress as you go.", "Add your first goal", () => EditGoal()));
        foreach (var goal in _data.Goals)
        {
            var s = Stack(UiMetrics.Lg); var actions = new StackPanel { Orientation = Orientation.Horizontal, Spacing = UiMetrics.Sm }; actions.Children.Add(Button("Update", () => EditGoal(goal))); actions.Children.Add(Button("Delete", () => DeleteRecord("goal", goal.Id)));
            s.Children.Add(Split(T(goal.Title, 23, true), actions)); s.Children.Add(T($"{goal.Current:0.#} / {goal.Target:0.#} {goal.Unit}   ·   {Math.Min(100, goal.Current / goal.Target * 100):0}%", color: Muted)); s.Children.Add(new ProgressBar { Value = goal.Current, Maximum = goal.Target, Height = 8, Foreground = Accent, Background = Raised }); if (goal.Current >= goal.Target) s.Children.Add(T("✓  Goal achieved", 13, true, Accent)); page.Children.Add(Card(s));
        }
        return page;
    }
    private async Task EditGoal(Goal? goal = null)
    {
        var dialog = Dialog("Your goal"); var form = Stack(UiMetrics.Lg); form.Children.Add(Heading("Personal milestone", goal == null ? "Add a goal" : "Update your goal", "Choose a measurable goal that matters to you."));
        var name = new TextBox { Text = goal?.Title ?? "", Watermark = "e.g. Deadlift personal record", MaxLength = 100 }; var target = Number(goal?.Target ?? 100, .1, 1000000, .1); var current = Number(goal?.Current ?? 0, 0, 1000000, .1); var unit = new TextBox { Text = goal?.Unit ?? "kg", MaxLength = 30 };
        form.Children.Add(Field("Goal name", name)); form.Children.Add(Columns(Field("Target", target), Field("Current progress", current))); form.Children.Add(Field("Unit · kg, km, sessions…", unit));
        EditorContent(dialog, form, () => { var id = goal?.Id ?? Guid.NewGuid().ToString("N"); _repository.Save("goal", id, new Goal(id, name.Text?.Trim() ?? "", Value(target), Value(current), unit.Text?.Trim() ?? "")); }); await dialog.ShowDialog(this);
    }
}
