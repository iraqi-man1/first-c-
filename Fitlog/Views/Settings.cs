using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Platform.Storage;
using Fitlog.Models;

namespace Fitlog;
public sealed partial class MainWindow
{
    private Control Choice(string[] options, int selected, Action<int> change)
    {
        var s = new StackPanel { Orientation = Orientation.Horizontal, Spacing = UiMetrics.Xs };
        for (int i = 0; i < options.Length; i++) { int index = i; var b = Button(options[i], () => change(index)); b.Background = i == selected ? Line : Avalonia.Media.Brushes.Transparent; s.Children.Add(b); }
        return new Border { Background = Raised, CornerRadius = new(12), Padding = new(4), HorizontalAlignment = HorizontalAlignment.Left, Child = s };
    }
    private Control SettingsPage()
    {
        var page = Stack(UiMetrics.Xl); page.Children.Add(Heading("", "Settings", "Make fitlog your own. Your data is stored locally on this device."));
        var appearance = Stack(UiMetrics.Xl); appearance.Children.Add(T("Appearance", 28, true));
        appearance.Children.Add(Field("Appearance", Choice(["Dark", "Light"], Pref.Light ? 1 : 0, i => { Pref.Light = i == 1; SavePreferences(); })));
        appearance.Children.Add(Field("Language", Choice(["English", "العربية"], Arabic ? 1 : 0, i => { Pref.Language = i == 1 ? "ar" : "en"; SavePreferences(); })));
        appearance.Children.Add(Field("Weight units", Choice(["kg", "lb"], Pref.Pounds ? 1 : 0, i => { Pref.Pounds = i == 1; SavePreferences(); })));
        appearance.Children.Add(Field("Measurement units", Choice(["cm", "in"], Pref.Inches ? 1 : 0, i => { Pref.Inches = i == 1; SavePreferences(); })));
        appearance.Children.Add(Field("Week starts on", Choice(["Monday", "Sunday"], Pref.SundayFirst ? 1 : 0, i => { Pref.SundayFirst = i == 1; SavePreferences(); })));
        var tracking = Stack(UiMetrics.Xl); tracking.Children.Add(T("Tracking preferences", 28, true));
        string[] options = ["Overall", "Gym", "Diet", "Weight logging"];
        var heat = SelectOptions(options, Array.IndexOf(options, Pref.Heatmap));
        heat.SelectionChanged += (_, _) => { if (heat.SelectedIndex >= 0 && options[heat.SelectedIndex] != Pref.Heatmap) { Pref.Heatmap = options[heat.SelectedIndex]; _heatFilter = Pref.Heatmap; _repository.Save("preferences", "main", Pref); _status.Text = Tr("Preferences saved"); } };
        tracking.Children.Add(Field("Heatmap default", heat)); tracking.Children.Add(T("Show optional check-in fields", 12, color: Muted));
        void Check(string label, bool selected, Action<bool> setter) { var c = new CheckBox { Content = Tr(label), IsChecked = selected }; c.IsCheckedChanged += (_, _) => { setter(c.IsChecked == true); _repository.Save("preferences", "main", Pref); _status.Text = Tr("Preferences saved"); }; tracking.Children.Add(c); }
        Check("Energy", Pref.ShowEnergy, x => Pref.ShowEnergy = x); Check("Sleep hours", Pref.ShowSleep, x => Pref.ShowSleep = x); Check("Water intake", Pref.ShowWater, x => Pref.ShowWater = x); Check("Steps", Pref.ShowSteps, x => Pref.ShowSteps = x);
        tracking.Children.Add(T("Hidden fields keep their existing values.", 12, color: Muted));
        page.Children.Add(Columns(Card(appearance), Card(tracking)));
        var goals = Stack(UiMetrics.Xl); goals.Children.Add(T("Goals", 28, true));
        var target = Number(DisplayWeight(Pref.TargetWeight), DisplayWeight(20), DisplayWeight(500), .1); var weekly = Number(Pref.WeeklyGoal, 1, 7); var monthly = Number(Pref.MonthlyGoal, 1, 31); var diet = Number(Pref.DietGoal, 1, 100);
        goals.Children.Add(Columns(Field($"Target weight · {(Pref.Pounds ? "lb" : "kg")}", target), Field("Weekly gym goal", weekly), Field("Monthly gym goal", monthly), Field("Diet goal · %", diet)));
        goals.Children.Add(Button("Save goals", () => { var next = Pref with { TargetWeight = ToKg(Value(target)), WeeklyGoal = (int)Value(weekly), MonthlyGoal = (int)Value(monthly), DietGoal = (int)Value(diet) }; _repository.Save("preferences", "main", next); Refresh("Goals saved"); }, true)); page.Children.Add(Card(goals));
        var backup = Stack(UiMetrics.Lg); backup.Children.Add(T("Backup & export", 28, true)); backup.Children.Add(T("Keep a portable copy of your check-ins, weight, goals, photos, and preferences.", color: Muted));
        var actions = new StackPanel { Orientation = Orientation.Horizontal, Spacing = UiMetrics.Md }; actions.Children.Add(Button("↓  Export backup", ExportBackup, true)); actions.Children.Add(Button("↑  Import backup", ImportBackup)); backup.Children.Add(actions);
        backup.Children.Add(T("Import merges entries. Matching dates and IDs are updated; other records are kept.", 12, color: Muted)); page.Children.Add(Card(backup));
        var storage = Stack(UiMetrics.Md); storage.Children.Add(T("Local storage", 22, true)); storage.Children.Add(T(_repository.DatabasePath, 12, color: Muted)); storage.Children.Add(T("No account needed. Photos are included in your local database and backups.", 12, color: Muted)); page.Children.Add(Card(storage)); return page;
    }
    private async Task ExportBackup()
    {
        var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions { Title = Tr("Export fitlog backup"), SuggestedFileName = $"fitlog-backup-{Today:yyyy-MM-dd}.json", DefaultExtension = "json", FileTypeChoices = [new FilePickerFileType(Tr("Fitlog backup")) { Patterns = ["*.json"] }] });
        if (file == null) return;
        await using var stream = await file.OpenWriteAsync(); stream.SetLength(0); await using var writer = new StreamWriter(stream); await writer.WriteAsync(_repository.Export()); _status.Text = Tr("Backup exported");
    }
    private async Task ImportBackup()
    {
        var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions { Title = Tr("Import fitlog backup"), AllowMultiple = false, FileTypeFilter = [new FilePickerFileType(Tr("Fitlog backup")) { Patterns = ["*.json"] }] });
        if (files.Count == 0) return;
        await using var stream = await files[0].OpenReadAsync(); if (stream.Length > 150_000_000) throw new InvalidDataException("This backup is too large (maximum 150 MB)."); using var reader = new StreamReader(stream); var json = await reader.ReadToEndAsync();
        // Keep a recovery copy before merging any imported records.
        var backupDirectory = Path.Combine(Path.GetDirectoryName(_repository.DatabasePath)!, "Backups"); Directory.CreateDirectory(backupDirectory);
        await File.WriteAllTextAsync(Path.Combine(backupDirectory, $"before-import-{DateTime.Now:yyyyMMdd-HHmmss-fff}.json"), _repository.Export());
        _repository.Import(json); Reload(); _heatFilter = Pref.Heatmap; BuildShell(); _status.Text = Tr("Backup imported. A recovery copy was saved in the Backups folder.");
    }
}
