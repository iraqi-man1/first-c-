using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Fitlog.Models;

namespace Fitlog;
public sealed partial class MainWindow
{
    private Control WorkoutsPage()
    {
        var page = Stack(24);
        var add = Button("＋  Add workout day", () => EditRoutine(), true); add.Name = "AddWorkoutRoutine";
        page.Children.Add(Split(Heading("Your plan", "Workout schedule", "Your weekly plan and custom exercises. Record attendance from Log Today."), add));
        if (_data.Routines.Count == 0) page.Children.Add(Empty("Create your training plan", "Add a workout, choose its weekdays, and list the exercises you follow.", "Add workout day", () => EditRoutine()));
        foreach (var routine in _data.Routines.OrderByDescending(x => x.Days.Contains(Today.DayOfWeek)).ThenBy(x => x.Title))
        {
            var content = Stack(18);
            var actions = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
            var edit = Button("Edit", () => EditRoutine(routine)); edit.Name = "EditRoutine"; actions.Children.Add(edit);
            actions.Children.Add(Button("Delete", () => DeleteRecord("routine", routine.Id)));
            var heading = Stack(8); heading.Children.Add(T(routine.Title, 24, true));
            heading.Children.Add(T(string.Join(" · ", routine.Days.OrderBy(x => ((int)x + (Pref.SundayFirst ? 0 : 6)) % 7).Select(x => Tr(x.ToString()))), 13, color: Muted));
            content.Children.Add(Split(heading, actions));
            if (routine.Days.Contains(Today.DayOfWeek)) content.Children.Add(T("Scheduled today", 12, true, Accent));
            var header = Columns(T("Exercise name", 12, true), T("Sets", 12, true), T("Reps", 12, true), T("Load", 12, true), T("Rest · seconds", 12, true)); content.Children.Add(header);
            foreach (var exercise in routine.Exercises)
                content.Children.Add(new Border { Padding = new Thickness(0, 10), BorderBrush = Line, BorderThickness = new(0, 0, 0, 1), Child = Columns(T(exercise.Name), T(exercise.Sets.ToString()), T(exercise.Reps), T(exercise.WeightKg == 0 ? "—" : Weight(exercise.WeightKg)), T($"{exercise.RestSeconds} {L("sec", "ثانية")}")) });
            if (!string.IsNullOrWhiteSpace(routine.Notes)) content.Children.Add(T(routine.Notes, 13, color: Muted));
            page.Children.Add(Card(content));
        }
        return page;
    }
    private async Task EditRoutine(WorkoutRoutine? routine = null)
    {
        var dialog = Dialog(routine == null ? "Add workout day" : "Edit workout"); dialog.Width = 840;
        var form = Stack(18); form.Children.Add(Heading("Your plan", routine == null ? "Add workout day" : "Edit workout", ""));
        var title = new TextBox { Name = "RoutineTitle", Text = routine?.Title ?? "", Watermark = "e.g. Push day", MaxLength = 100 }; form.Children.Add(Field("Workout name", title));
        var days = new WrapPanel { Orientation = Orientation.Horizontal }; var dayChecks = new Dictionary<DayOfWeek, CheckBox>();
        for (int i = 0; i < 7; i++)
        {
            var day = (DayOfWeek)((i + (Pref.SundayFirst ? 0 : 1)) % 7);
            var check = new CheckBox { Content = Tr(day.ToString()), Name = "RoutineDay" + day, IsChecked = routine?.Days.Contains(day) ?? day == Today.DayOfWeek, Margin = new Thickness(0, 0, 16, 0) };
            dayChecks[day] = check; days.Children.Add(check);
        }
        form.Children.Add(Field("Repeat on", days)); form.Children.Add(T("Exercises", 20, true));
        var exercises = Stack(12); var editors = new List<ExerciseEditor>();
        void AddExercise(PlannedExercise? exercise)
        {
            var editor = new ExerciseEditor(this, exercise); editors.Add(editor);
            var block = Stack(10); block.Children.Add(Field("Exercise name", editor.Name));
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
            var selectedDays = dayChecks.Where(x => x.Value.IsChecked == true).Select(x => x.Key).ToList();
            if (selectedDays.Count == 0) throw new InvalidDataException("Choose at least one weekday.");
            if (editors.Count == 0) throw new InvalidDataException("Add at least one exercise.");
            var items = editors.Select(x => x.Read(this)).ToList();
            var id = routine?.Id ?? Guid.NewGuid().ToString("N");
            _repository.Save("routine", id, new WorkoutRoutine { Id = id, Title = title.Text.Trim(), Days = selectedDays, Notes = notes.Text?.Trim() ?? "", Exercises = items });
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
