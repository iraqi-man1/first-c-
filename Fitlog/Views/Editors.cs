using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Fitlog.Models;

namespace Fitlog;
public sealed partial class MainWindow
{
    private Control Field(string label, Control input)
    {
        if (input is Avalonia.Controls.Primitives.TemplatedControl field) { field.Background = Raised; field.BorderBrush = Line; field.Foreground = Ink; }
        if (input is TextBox textBox && textBox.Watermark != null) textBox.Watermark = Tr(textBox.Watermark);
        var s = Stack(7); s.Children.Add(T(label, 12, color: Muted)); s.Children.Add(input); return s;
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
        var footer = Stack(10); footer.Children.Add(error); footer.Children.Add(Columns(Button("Cancel", () => dialog.Close()), saveButton));
        var layout = new Grid { RowDefinitions = new("*,Auto"), RowSpacing = 16 };
        layout.Children.Add(new ScrollViewer { Content = form, MaxHeight = 640 }); Grid.SetRow(footer, 1); layout.Children.Add(footer); dialog.Content = layout;
    }
    private async Task EditLog(DateOnly date)
    {
        if (date > Today) { await Message("A day at a time", "Check-ins can be recorded for today or any previous day."); return; }
        var existing = _data.Logs.FirstOrDefault(x => x.Date == date); var entry = existing ?? new DailyLog { Date = date };
        var dialog = Dialog("Daily check-in"); var form = Stack(16); form.Children.Add(Heading("Daily check-in", DateText(date, "dddd, MMMM d"), "Small actions add up. Record what matters to you."));
        var attendance = SelectOptions(["Choose a day status", "Training day", "Rest day", "Missed gym"], (int)entry.EffectiveStatus); attendance.Name = "DayStatus";
        form.Children.Add(Field("Day status", attendance));
        var completion = Number(entry.EffectiveWorkoutCompletion ?? 100, 0, 100, 5); completion.Name = "WorkoutCompletion";
        var diet = Number(entry.EffectiveDietCompletion, 0, 100, 5); diet.Name = "DietCompletion";
        var completionField = Field("Completion · %", completion); form.Children.Add(Columns(completionField, Field("Diet adherence · %", diet)));
        var workout = new TextBox { Text = entry.Workout, Watermark = "e.g. Upper body · strength", MaxLength = 120, Name = "WorkoutName" }; var minutes = Number(entry.Minutes, 0, 1440); form.Children.Add(Field("Workout", workout)); form.Children.Add(Field("Duration · minutes", minutes));
        void UpdateAttendance() { var trained = attendance.SelectedIndex == (int)Attendance.Training; completionField.IsVisible = trained; workout.IsEnabled = trained; minutes.IsEnabled = trained; }
        attendance.SelectionChanged += (_, _) => UpdateAttendance(); UpdateAttendance();
        var steps = Number(entry.Steps, 0, 200000, 100); var water = Number(entry.Water, 0, 30, .1); var sleep = Number(entry.Sleep, 0, 24, .5); var energy = Number(entry.Energy, 1, 5);
        var optional = new List<Control>();
        if (Pref.ShowSteps) optional.Add(Field("Steps", steps)); if (Pref.ShowWater) optional.Add(Field("Water · liters", water)); if (Pref.ShowSleep) optional.Add(Field("Sleep · hours", sleep)); if (Pref.ShowEnergy) optional.Add(Field("Energy · 1 to 5", energy));
        for (int i = 0; i < optional.Count; i += 2) form.Children.Add(i + 1 < optional.Count ? Columns(optional[i], optional[i + 1]) : optional[i]);
        var notes = new TextBox { Text = entry.Notes, Watermark = "How did today feel?", AcceptsReturn = true, TextWrapping = Avalonia.Media.TextWrapping.Wrap, MinHeight = 85, MaxLength = 5000, Name = "DailyNotes" }; form.Children.Add(Field("Notes", notes));
        form.Children.Add(Button("＋  Add a weigh-in for this day", () => EditWeight(_data.Weights.FirstOrDefault(x => x.Date == date), date, dialog)));
        if (existing != null) form.Children.Add(Button("Delete check-in", async () => { if (await Confirm("Delete check-in?", "The check-in will be removed. Weigh-ins and photos are kept separately.")) { _repository.Delete("log", date.ToString("yyyy-MM-dd")); dialog.Close(); Refresh("Check-in deleted"); } }));
        EditorContent(dialog, form, () =>
        {
            if (attendance.SelectedIndex <= 0) throw new InvalidDataException("Choose a day status");
            var status = (Attendance)attendance.SelectedIndex; var trained = status == Attendance.Training;
            _repository.Save("log", date.ToString("yyyy-MM-dd"), new DailyLog { Date = date, Status = status, Gym = trained, Diet = Value(diet) == 100, WorkoutCompletion = trained ? (int)Value(completion) : status == Attendance.Missed ? 0 : null, DietCompletion = (int)Value(diet), Workout = workout.Text?.Trim() ?? "", Minutes = trained ? (int)Value(minutes) : 0, Steps = (int)Value(steps), Water = Value(water), Sleep = Value(sleep), Energy = (int)Value(energy), Notes = notes.Text?.Trim() ?? "" });
        });
        await dialog.ShowDialog(this);
    }
    private async Task EditWeight(WeightEntry? entry = null, DateOnly? initialDate = null, Window? owner = null)
    {
        var dialog = Dialog(entry == null ? "Add weight" : "Edit weight"); var form = Stack(18); form.Children.Add(Heading("Weigh-in", "Add weight", "One weigh-in per day. Saving the same date updates its value."));
        var date = DatePicker(entry?.Date ?? initialDate ?? Today); date.IsEnabled = entry == null;
        var number = Number(DisplayWeight(entry?.Kilograms ?? Weights.LastOrDefault()?.Kilograms ?? 80), DisplayWeight(20), DisplayWeight(500), .1); number.Name = "WeightValue";
        form.Children.Add(Field("Date", date)); form.Children.Add(Field($"Weight · {(Pref.Pounds ? "lb" : "kg")}", number));
        EditorContent(dialog, form, () => { var d = Selected(date); _repository.Save("weight", d.ToString("yyyy-MM-dd"), new WeightEntry(d, ToKg(Value(number)))); });
        await dialog.ShowDialog(owner ?? this);
    }
    private async Task EditMeasurement(Measurement? entry = null)
    {
        var dialog = Dialog("Measurements"); var form = Stack(18); form.Children.Add(Heading("Body measurements", "Record measurements", "Use a consistent measuring point each time."));
        var date = DatePicker(entry?.Date ?? Today); date.IsEnabled = entry == null; var unit = Pref.Inches ? "in" : "cm"; double factor = Pref.Inches ? 2.54 : 1;
        var waist = Number((entry?.Waist ?? 80) / factor, 1 / factor, 500 / factor, .1); var chest = Number((entry?.Chest ?? 95) / factor, 1 / factor, 500 / factor, .1); var hips = Number((entry?.Hips ?? 95) / factor, 1 / factor, 500 / factor, .1);
        form.Children.Add(Field("Date", date)); form.Children.Add(Field($"Waist · {unit}", waist)); form.Children.Add(Field($"Chest · {unit}", chest)); form.Children.Add(Field($"Hips · {unit}", hips));
        EditorContent(dialog, form, () => { var d = Selected(date); _repository.Save("measurement", d.ToString("yyyy-MM-dd"), new Measurement(d, Value(waist) * factor, Value(chest) * factor, Value(hips) * factor)); }); await dialog.ShowDialog(this);
    }
}
