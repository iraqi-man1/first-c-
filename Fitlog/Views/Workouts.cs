using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Fitlog.Models;

namespace Fitlog;
public sealed partial class MainWindow
{
    private Control WorkoutsPage()
    {
        var page = Stack(UiMetrics.Xl);
        var add = Button("＋  Add workout day", () => EditRoutine(), true); add.Name = "AddWorkoutRoutine";
        page.Children.Add(Split(Heading("Your plan", "Workout schedule", "Create a program with workout days, then choose the day and exercises when you check in."), add));
        if (_data.Routines.Count == 0) page.Children.Add(Empty("Create your training plan", "Name your program and add a workout day with its exercises.", "Add workout day", () => EditRoutine()));
        foreach (var group in _data.Routines.GroupBy(x => x.ProgramName).OrderBy(x => x.Key))
        {
            page.Children.Add(Split(T(group.Key, 23, true), Button("＋  Add day to program", () => EditRoutine(programName: group.Key))));
            foreach (var routine in group.OrderByDescending(x => x.Days.Contains(Today.DayOfWeek)).ThenBy(x => x.Title))
            {
            var content = Stack(UiMetrics.Lg);
            var actions = new StackPanel { Orientation = Orientation.Horizontal, Spacing = UiMetrics.Sm };
            var edit = Button("Edit", () => EditRoutine(routine)); edit.Name = "EditRoutine"; actions.Children.Add(edit);
            actions.Children.Add(Button("Delete", () => DeleteRecord("routine", routine.Id)));
            var heading = Stack(UiMetrics.Sm); heading.Children.Add(T(routine.Title, 24, true));
            heading.Children.Add(T(routine.Days.Count == 0 ? "Choose on check-in" : string.Join(" · ", routine.Days.OrderBy(x => ((int)x + (Pref.SundayFirst ? 0 : 6)) % 7).Select(x => Tr(x.ToString()))), 13, color: Muted));
            heading.Children.Add(T(routine.CreatedOn is { } createdOn ? $"{Tr("Created on")} {DateText(createdOn)}" : "Creation date not recorded", 12, color: Muted));
            content.Children.Add(Split(heading, actions));
            if (routine.Days.Contains(Today.DayOfWeek)) content.Children.Add(T("Scheduled today", 12, true, Accent));
            var header = ColumnsWithMinimum(140, T("Exercise name", 12, true), T("Sets", 12, true), T("Reps", 12, true), T("Load", 12, true), T("Rest · seconds", 12, true)); content.Children.Add(header);
            foreach (var exercise in routine.Exercises)
                content.Children.Add(new Border { Padding = new Thickness(0, 10), BorderBrush = Line, BorderThickness = new(0, 0, 0, 1), Child = ColumnsWithMinimum(140, T(exercise.Name), T(exercise.Sets.ToString()), T(exercise.Reps), T(exercise.WeightKg == 0 ? "—" : Weight(exercise.WeightKg)), T($"{exercise.RestSeconds} {L("sec", "ثانية")}")) });
            if (!string.IsNullOrWhiteSpace(routine.Notes)) content.Children.Add(T(routine.Notes, 13, color: Muted));
            page.Children.Add(Card(content));
            }
        }
        return page;
    }
    private async Task EditRoutine(WorkoutRoutine? routine = null, string? programName = null)
    {
        var dialog = Dialog(routine == null ? "Add workout day" : "Edit workout"); dialog.Width = 840;
        var form = Stack(UiMetrics.Lg); form.Children.Add(Heading("Your plan", routine == null ? "Add workout day" : "Edit workout", ""));
        var program = new TextBox { Name = "RoutineProgram", Text = routine?.ProgramName ?? programName ?? _data.Routines.FirstOrDefault()?.ProgramName ?? "", Watermark = Tr("e.g. Upper / Lower"), MaxLength = 100 };
        form.Children.Add(Field("Program name", program));
        if (_data.Routines.Count > 0) form.Children.Add(T($"{Tr("Existing programs")}: {string.Join(" · ", _data.Routines.Select(x => x.ProgramName).Distinct().OrderBy(x => x))}", 12, color: Muted));
        var title = new TextBox { Name = "RoutineTitle", Text = routine?.Title ?? "", Watermark = "e.g. Push day", MaxLength = 100 }; form.Children.Add(Field("Workout name", title));
        var days = new WrapPanel { Orientation = Orientation.Horizontal }; var dayChecks = new Dictionary<DayOfWeek, CheckBox>();
        for (int i = 0; i < 7; i++)
        {
            var day = (DayOfWeek)((i + (Pref.SundayFirst ? 0 : 1)) % 7);
            var check = new CheckBox { Content = Tr(day.ToString()), Name = "RoutineDay" + day, IsChecked = routine?.Days.Contains(day) ?? false, Margin = new Thickness(0, 0, 16, 0) };
            dayChecks[day] = check; days.Children.Add(check);
        }
        form.Children.Add(Field("Repeat on (optional)", days)); form.Children.Add(T("Exercises", 20, true));
        var exercises = Stack(UiMetrics.Md); var editors = new List<ExerciseEditor>();
        void AddExercise(PlannedExercise? exercise)
        {
            var editor = new ExerciseEditor(this, exercise); editors.Add(editor);
            var block = Stack(UiMetrics.Md); block.Children.Add(Field("Exercise name", editor.Name));
            block.Children.Add(Columns(Field("Sets", editor.Sets), Field("Reps", editor.Reps), Field($"{Tr("Load")} · {Tr(Pref.Pounds ? "lb" : "kg")}", editor.Weight), Field("Rest · seconds", editor.Rest)));
            var border = Card(block, 16); var remove = Button("Remove", () => { editors.Remove(editor); exercises.Children.Remove(border); }); remove.Name = "RemoveExercise"; block.Children.Add(remove); exercises.Children.Add(border);
        }
        foreach (var exercise in routine?.Exercises ?? [new PlannedExercise("", 3, "8–12", 0, 90)]) AddExercise(exercise);
        form.Children.Add(exercises);
        var addExercise = Button("＋  Add exercise", () => AddExercise(null)); addExercise.Name = "AddExercise"; form.Children.Add(addExercise);
        var notes = new TextBox { Text = routine?.Notes ?? "", MaxLength = 2000, AcceptsReturn = true, MinHeight = 65 }; form.Children.Add(Field("Schedule notes", notes));
        EditorContent(dialog, form, () =>
        {
            if (string.IsNullOrWhiteSpace(title.Text)) throw new InvalidDataException("Enter a workout name.");
            if (string.IsNullOrWhiteSpace(program.Text)) throw new InvalidDataException("Enter a program name.");
            if (program.Text.Trim().Length > 100) throw new InvalidDataException("Program name is too long.");
            var selectedDays = dayChecks.Where(x => x.Value.IsChecked == true).Select(x => x.Key).ToList();
            if (editors.Count == 0) throw new InvalidDataException("Add at least one exercise.");
            var items = editors.Select(x => x.Read(this)).ToList();
            var id = routine?.Id ?? Guid.NewGuid().ToString("N");
            _repository.Save("routine", id, new WorkoutRoutine { Id = id, CreatedOn = routine == null ? Today : routine.CreatedOn, ProgramName = program.Text.Trim(), Title = title.Text.Trim(), Days = selectedDays, Notes = notes.Text?.Trim() ?? "", Exercises = items });
        });
        await dialog.ShowDialog(this);
    }
    private sealed class ExerciseEditor
    {
        public TextBox Name { get; }
        public TextBox Reps { get; }
        public NumericUpDown Sets { get; }
        public NumericUpDown Weight { get; }
        public NumericUpDown Rest { get; }
        public ExerciseEditor(MainWindow owner, PlannedExercise? exercise)
        {
            Name = new TextBox { Text = exercise?.Name ?? "", MaxLength = 100, Name = "ExerciseName" };
            Reps = new TextBox { Text = exercise?.Reps ?? "8–12", MaxLength = 30, Name = "ExerciseReps" };
            Sets = Number(exercise?.Sets ?? 3, 1, 50); Weight = Number(owner.DisplayWeight(exercise?.WeightKg ?? 0), 0, owner.DisplayWeight(1000), .5); Rest = Number(exercise?.RestSeconds ?? 90, 0, 3600, 15);
        }
        public PlannedExercise Read(MainWindow owner)
        {
            if (string.IsNullOrWhiteSpace(Name.Text) || string.IsNullOrWhiteSpace(Reps.Text)) throw new InvalidDataException("Enter an exercise name and rep range.");
            return new(Name.Text.Trim(), (int)Value(Sets), Reps.Text.Trim(), owner.ToKg(Value(Weight)), (int)Value(Rest));
        }
    }
}
