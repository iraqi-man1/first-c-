using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Fitlog.Models;

namespace Fitlog;
public sealed partial class MainWindow
{
    private Control Field(string label, Control input)
    {
        if (input is Avalonia.Controls.Primitives.TemplatedControl field) { field.Background = InputSurface; field.BorderBrush = Line; field.Foreground = Ink; }
        if (input is TextBox textBox && textBox.Watermark != null) textBox.Watermark = Tr(textBox.Watermark);
        var s = Stack(UiMetrics.Sm); s.Children.Add(T(label, 12, true, Muted)); s.Children.Add(input); return s;
    }
    private static NumericUpDown Number(double value, double min, double max, double increment = 1) => new()
    {
        Value = (decimal)value, Minimum = (decimal)min, Maximum = (decimal)max, Increment = (decimal)increment,
        FormatString = increment < 1 ? "0.0" : "0", NumberFormat = System.Globalization.CultureInfo.InvariantCulture.NumberFormat, HorizontalAlignment = HorizontalAlignment.Stretch
    };
    private static double Value(NumericUpDown input) => (double)(input.Value ?? throw new InvalidDataException("Enter all numeric values before saving."));
    private CalendarDatePicker DatePicker(DateOnly date) => new() { SelectedDate = date.ToDateTime(TimeOnly.MinValue), DisplayDateEnd = Today.ToDateTime(TimeOnly.MinValue), HorizontalAlignment = HorizontalAlignment.Stretch };
    private DateOnly Selected(CalendarDatePicker date) => date.SelectedDate is { } value && DateOnly.FromDateTime(value) <= Today ? DateOnly.FromDateTime(value) : throw new InvalidDataException("Choose a date on or before today.");
    private void EditorContent(Window dialog, StackPanel form, Action save)
    {
        var error = T("", 12, color: Brush("#E99B92"));
        var saveButton = Button("Save entry", () => { try { save(); dialog.Close(); Refresh(); } catch (Exception ex) { error.Text = Tr(ex.Message); } }, true); saveButton.Name = "SaveEntry";
        var footer = Stack(UiMetrics.Md); footer.Children.Add(error);
        var actions = new StackPanel { Orientation = Orientation.Horizontal, Spacing = UiMetrics.Md, HorizontalAlignment = HorizontalAlignment.Right };
        actions.Children.Add(Button("Cancel", () => dialog.Close())); actions.Children.Add(saveButton); footer.Children.Add(actions);
        var layout = new Grid { RowDefinitions = new("*,Auto"), RowSpacing = UiMetrics.Lg };
        layout.Children.Add(new ScrollViewer { Content = form, MaxHeight = 640 }); Grid.SetRow(footer, 1); layout.Children.Add(footer); dialog.Content = layout;
    }
    private async Task EditLog(DateOnly date)
    {
        if (date > Today) { await Message("A day at a time", "Check-ins can be recorded for today or any previous day."); return; }
        var existing = _data.Logs.FirstOrDefault(x => x.Date == date); var entry = existing ?? new DailyLog { Date = date };
        var dialog = Dialog("Daily check-in"); var form = Stack(UiMetrics.Lg); form.Children.Add(Heading("Daily check-in", DateText(date, "dddd, MMMM d"), "Small actions add up. Record what matters to you."));
        var attendance = SelectOptions(["Choose a day status", "Training day", "Rest day", "Missed gym"], (int)entry.EffectiveStatus); attendance.Name = "DayStatus";
        form.Children.Add(Field("Day status", attendance));
        var completion = Number(entry.EffectiveWorkoutCompletion ?? 100, 0, 100, 5); completion.Name = "WorkoutCompletion";
        var dietOptions = new List<int> { 0, 25, 50, 75, 100 };
        if (existing != null && !dietOptions.Contains(entry.EffectiveDietCompletion)) { dietOptions.Add(entry.EffectiveDietCompletion); dietOptions.Sort(); }
        var diet = SelectOptions(dietOptions.Select(x => $"{x}%").ToArray(), dietOptions.IndexOf(entry.EffectiveDietCompletion)); diet.Name = "DietCompletion";
        var completionField = Field("Completion · %", completion); form.Children.Add(Columns(completionField, Field("Diet adherence · %", diet)));
        var routineOptions = new List<ComboBoxItem> { new() { Content = Tr("Custom workout"), Tag = null } };
        routineOptions.AddRange(_data.Routines.OrderBy(x => x.ProgramName).ThenBy(x => x.Title).Select(x => new ComboBoxItem { Content = $"{x.ProgramName} · {x.Title}", Tag = x.Id }));
        var routinePicker = new ComboBox { Name = "LogRoutine", ItemsSource = routineOptions, SelectedIndex = Math.Max(0, routineOptions.FindIndex(x => Equals(x.Tag, entry.RoutineId))), HorizontalAlignment = HorizontalAlignment.Stretch };
        var routineField = Field("Workout day from your plan", routinePicker); form.Children.Add(routineField);
        var exerciseList = Stack(UiMetrics.Xs); var exerciseChecks = new List<(string Name, CheckBox Check)>();
        var exerciseField = Field("Exercises completed", exerciseList); form.Children.Add(exerciseField);
        var workout = new TextBox { Text = entry.Workout, Watermark = "e.g. Upper body · strength", MaxLength = 120, Name = "WorkoutName" }; var minutes = Number(entry.Minutes, 0, 1440); var workoutField = Field("Workout", workout); var minutesField = Field("Duration · minutes", minutes); form.Children.Add(workoutField); form.Children.Add(minutesField);
        WorkoutRoutine? SelectedRoutine() => _data.Routines.FirstOrDefault(x => x.Id == (routinePicker.SelectedItem as ComboBoxItem)?.Tag as string);
        void UpdateRoutine()
        {
            exerciseChecks.Clear(); exerciseList.Children.Clear();
            var routine = SelectedRoutine(); exerciseField.IsVisible = routine != null;
            if (routine == null) return;
            foreach (var exercise in routine.Exercises)
            {
                var check = new CheckBox { Name = "PerformedExercise", Content = $"{exercise.Name} · {exercise.Sets} × {exercise.Reps}", IsChecked = existing?.RoutineId == routine.Id && entry.PerformedExercises.Contains(exercise.Name) };
                exerciseChecks.Add((exercise.Name, check)); exerciseList.Children.Add(check);
            }
        }
        routinePicker.SelectionChanged += (_, _) => { var routine = SelectedRoutine(); if (routine != null) workout.Text = routine.Title; UpdateRoutine(); };
        UpdateRoutine();
        void UpdateAttendance() { var trained = attendance.SelectedIndex == (int)Attendance.Training; completionField.IsVisible = trained; routineField.IsVisible = trained; exerciseField.IsVisible = trained && SelectedRoutine() != null; workoutField.IsVisible = trained; minutesField.IsVisible = trained; }
        attendance.SelectionChanged += (_, _) => UpdateAttendance(); UpdateAttendance();
        var steps = Number(entry.Steps, 0, 200000, 100); var water = Number(entry.Water, 0, 30, .1); var sleep = Number(entry.Sleep, 0, 24, .5); var energy = Number(entry.Energy, 1, 5);
        var optional = new List<Control>();
        if (Pref.ShowSteps) optional.Add(Field("Steps", steps)); if (Pref.ShowWater) optional.Add(Field("Water · liters", water)); if (Pref.ShowSleep) optional.Add(Field("Sleep · hours", sleep)); if (Pref.ShowEnergy) optional.Add(Field("Energy · 1 to 5", energy));
        for (int i = 0; i < optional.Count; i += 2) form.Children.Add(i + 1 < optional.Count ? Columns(optional[i], optional[i + 1]) : optional[i]);
        var notes = new TextBox { Text = entry.Notes, Watermark = "How did today feel?", AcceptsReturn = true, TextWrapping = Avalonia.Media.TextWrapping.Wrap, MinHeight = 85, MaxLength = 5000, Name = "DailyNotes" }; form.Children.Add(Field("Notes", notes));
        form.Children.Add(Button("＋  Add a weigh-in for this day", () => EditWeight(_data.Weights.FirstOrDefault(x => x.Date == date), date, dialog)));
        if (existing != null) form.Children.Add(Destructive(Button("Delete check-in", async () => { if (await Confirm("Delete check-in?", "The check-in will be removed. Weigh-ins and photos are kept separately.")) { _repository.Delete("log", date.ToString("yyyy-MM-dd")); dialog.Close(); Refresh("Check-in deleted"); } })));
        EditorContent(dialog, form, () =>
        {
            if (attendance.SelectedIndex <= 0) throw new InvalidDataException("Choose a day status");
            var status = (Attendance)attendance.SelectedIndex; var trained = status == Attendance.Training;
            var dietPercent = dietOptions[diet.SelectedIndex]; var selectedRoutine = trained ? SelectedRoutine() : null;
            if (selectedRoutine != null && !exerciseChecks.Any(x => x.Check.IsChecked == true)) throw new InvalidDataException("Choose at least one completed exercise.");
            _repository.Save("log", date.ToString("yyyy-MM-dd"), new DailyLog { Date = date, Status = status, Gym = trained, Diet = dietPercent == 100, WorkoutCompletion = trained ? (int)Value(completion) : status == Attendance.Missed ? 0 : null, DietCompletion = dietPercent, RoutineId = selectedRoutine?.Id, PerformedExercises = selectedRoutine == null ? [] : exerciseChecks.Where(x => x.Check.IsChecked == true).Select(x => x.Name).ToList(), Workout = trained ? workout.Text?.Trim() ?? "" : "", Minutes = trained ? (int)Value(minutes) : 0, Steps = (int)Value(steps), Water = Value(water), Sleep = Value(sleep), Energy = (int)Value(energy), Notes = notes.Text?.Trim() ?? "" });
        });
        await dialog.ShowDialog(this);
    }
    private async Task EditWeight(WeightEntry? entry = null, DateOnly? initialDate = null, Window? owner = null)
    {
        var dialog = Dialog(entry == null ? "Add weight" : "Edit weight"); var form = Stack(UiMetrics.Lg); form.Children.Add(Heading("Weigh-in", "Add weight", "One weigh-in per day. Saving the same date updates its value."));
        var date = DatePicker(entry?.Date ?? initialDate ?? Today); date.IsEnabled = entry == null;
        var number = Number(DisplayWeight(entry?.Kilograms ?? Weights.LastOrDefault()?.Kilograms ?? 80), DisplayWeight(20), DisplayWeight(500), .1); number.Name = "WeightValue";
        form.Children.Add(Field("Date", date)); form.Children.Add(Field($"Weight · {(Pref.Pounds ? "lb" : "kg")}", number));
        EditorContent(dialog, form, () => { var d = Selected(date); _repository.Save("weight", d.ToString("yyyy-MM-dd"), new WeightEntry(d, ToKg(Value(number)))); });
        await dialog.ShowDialog(owner ?? this);
    }
    private async Task EditMeasurement(Measurement? entry = null)
    {
        var dialog = Dialog("Measurements"); var form = Stack(UiMetrics.Lg); form.Children.Add(Heading("Body measurements", "Record measurements", "Use a consistent measuring point each time."));
        var date = DatePicker(entry?.Date ?? Today); date.IsEnabled = entry == null; var unit = Pref.Inches ? "in" : "cm"; double factor = Pref.Inches ? 2.54 : 1;
        var waist = Number((entry?.Waist ?? 80) / factor, 1 / factor, 500 / factor, .1); var chest = Number((entry?.Chest ?? 95) / factor, 1 / factor, 500 / factor, .1); var hips = Number((entry?.Hips ?? 95) / factor, 1 / factor, 500 / factor, .1);
        form.Children.Add(Field("Date", date)); form.Children.Add(Field($"Waist · {unit}", waist)); form.Children.Add(Field($"Chest · {unit}", chest)); form.Children.Add(Field($"Hips · {unit}", hips));
        EditorContent(dialog, form, () => { var d = Selected(date); _repository.Save("measurement", d.ToString("yyyy-MM-dd"), new Measurement(d, Value(waist) * factor, Value(chest) * factor, Value(hips) * factor)); }); await dialog.ShowDialog(this);
    }
}
