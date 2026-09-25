using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Fitlog.Models;
using Xunit;

[assembly: AvaloniaTestApplication(typeof(Fitlog.Tests.TestAppBuilder))]
[assembly: CollectionBehavior(DisableTestParallelization = true)]
namespace Fitlog.Tests;

public static class TestAppBuilder
{
    public static AppBuilder BuildAvaloniaApp() => AppBuilder.Configure<App>().WithInterFont().UseSkia().UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = false });
}
public sealed class UiTests
{
    private static T Named<T>(Window window, string name) where T : Control => window.GetVisualDescendants().OfType<T>().Single(x => x.Name == name);
    private static void Click(Button button) { button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent)); Dispatcher.UIThread.RunJobs(); }
    private static void Capture(Window window, string name)
    {
        Dispatcher.UIThread.RunJobs();
        var directory = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../artifacts/screenshots")); Directory.CreateDirectory(directory);
        using var frame = window.CaptureRenderedFrame(); Assert.NotNull(frame); frame.Save(Path.Combine(directory, name + ".png"));
    }
    [AvaloniaFact]
    public void DailyCheckInCanBeSavedAndWeightCanBeAddedThroughTheUi()
    {
        var repo = RepositoryTests.NewRepository(); var window = new MainWindow(repo); window.Show(); Dispatcher.UIThread.RunJobs();
        Click(Named<Button>(window, "LogToday")); var dialog = Assert.Single(window.OwnedWindows);
        Named<ComboBox>(dialog, "DayStatus").SelectedIndex = (int)Attendance.Training; Named<NumericUpDown>(dialog, "DietCompletion").Value = 100;
        Named<TextBox>(dialog, "WorkoutName").Text = "Upper body"; Named<TextBox>(dialog, "DailyNotes").Text = "Feeling strong";
        Capture(dialog, "check-in"); Click(Named<Button>(dialog, "SaveEntry"));
        var saved = Assert.Single(repo.Snapshot().Logs); Assert.True(saved.Gym); Assert.True(saved.Diet); Assert.Equal("Feeling strong", saved.Notes);
        Click(Named<Button>(window, "NavWeight"));
        var add = window.GetVisualDescendants().OfType<Button>().Single(x => x.Content is string s && s.Contains("Add weight")); Click(add);
        var weightDialog = Assert.Single(window.OwnedWindows); Named<NumericUpDown>(weightDialog, "WeightValue").Value = 79.9m; Click(Named<Button>(weightDialog, "SaveEntry"));
        Assert.Equal(79.9, Assert.Single(repo.Snapshot().Weights).Kilograms); window.Close();
    }
    [AvaloniaFact]
    public void MeasurementGoalAndUnitPreferencesCanBeEditedAndPersisted()
    {
        var repo = RepositoryTests.NewRepository(); var window = new MainWindow(repo); window.Show(); Dispatcher.UIThread.RunJobs();
        Click(Named<Button>(window, "NavMeasurements"));
        Click(window.GetVisualDescendants().OfType<Button>().Single(x => x.Content is string s && s.Contains("Add measurements")));
        var measurements = Assert.Single(window.OwnedWindows); Click(Named<Button>(measurements, "SaveEntry")); Assert.Single(repo.Snapshot().Measurements);
        Click(Named<Button>(window, "NavGoalsRecords"));
        Click(window.GetVisualDescendants().OfType<Button>().Single(x => x.Content is string s && s == "＋  Add goal"));
        var goal = Assert.Single(window.OwnedWindows); goal.GetVisualDescendants().OfType<TextBox>().Single(x => x.Watermark is string s && s.Contains("Deadlift")).Text = "Deadlift";
        Click(Named<Button>(goal, "SaveEntry")); Assert.Equal("Deadlift", Assert.Single(repo.Snapshot().Goals).Title);
        Click(Named<Button>(window, "NavSettings")); Click(window.GetVisualDescendants().OfType<Button>().Single(x => x.Content is string s && s == "lb")); Assert.True(repo.Snapshot().Preferences.Pounds);
        Click(Named<Button>(window, "NavWeight")); Click(window.GetVisualDescendants().OfType<Button>().Single(x => x.Content is string s && s.Contains("Add weight")));
        var weight = Assert.Single(window.OwnedWindows); Named<NumericUpDown>(weight, "WeightValue").Value = 176.3698m; Click(Named<Button>(weight, "SaveEntry")); Assert.InRange(Assert.Single(repo.Snapshot().Weights).Kilograms, 79.99, 80.01);
        window.Close();
    }
    [AvaloniaFact]
    public void ClockFollowsLocalMidnightAndRefreshesTodayWithoutRestart()
    {
        var clock = new TestClock(new DateTimeOffset(2026, 9, 30, 20, 59, 59, TimeSpan.Zero));
        var repo = RepositoryTests.NewRepository(); var window = new MainWindow(repo, clock); window.Show(); Dispatcher.UIThread.RunJobs();
        Assert.Contains("30 Sep", Named<TextBlock>(window, "LocalClock").Text);
        clock.Utc = clock.Utc.AddSeconds(2); window.UpdateClock(); Dispatcher.UIThread.RunJobs();
        Assert.Contains("01 Oct", Named<TextBlock>(window, "LocalClock").Text);
        Click(Named<Button>(window, "LogToday")); var dialog = Assert.Single(window.OwnedWindows);
        Named<ComboBox>(dialog, "DayStatus").SelectedIndex = (int)Attendance.Rest;
        Named<NumericUpDown>(dialog, "DietCompletion").Value = 65; Click(Named<Button>(dialog, "SaveEntry"));
        var log = Assert.Single(repo.Snapshot().Logs); Assert.Equal(new DateOnly(2026, 10, 1), log.Date); Assert.Equal(Attendance.Rest, log.EffectiveStatus); Assert.False(log.Trained); Assert.Null(log.EffectiveWorkoutCompletion); Assert.Equal(65, log.EffectiveDietCompletion); window.Close();
    }
    [AvaloniaFact]
    public void TrainingRestAndMissedAreDistinctAndCompletionIsPersisted()
    {
        var repo = RepositoryTests.NewRepository(); var window = new MainWindow(repo); window.Show(); Dispatcher.UIThread.RunJobs();
        foreach (var status in new[] { Attendance.Training, Attendance.Rest, Attendance.Missed })
        {
            Click(Named<Button>(window, "LogToday")); var dialog = Assert.Single(window.OwnedWindows); Named<ComboBox>(dialog, "DayStatus").SelectedIndex = (int)status;
            Named<NumericUpDown>(dialog, "WorkoutCompletion").Value = 75; Named<NumericUpDown>(dialog, "DietCompletion").Value = 80;
            Click(Named<Button>(dialog, "SaveEntry")); var log = Assert.Single(repo.Snapshot().Logs); Assert.Equal(status, log.EffectiveStatus);
            Assert.Equal(status == Attendance.Training ? 75 : status == Attendance.Missed ? 0 : (int?)null, log.EffectiveWorkoutCompletion); Assert.Equal(80, log.EffectiveDietCompletion);
        }
        window.Close();
    }
    [AvaloniaFact]
    public void CustomWorkoutScheduleCanBeCreatedAndEditedIndependentlyOfCheckIns()
    {
        var repo = RepositoryTests.NewRepository(); var window = new MainWindow(repo); window.Show(); Dispatcher.UIThread.RunJobs();
        Click(Named<Button>(window, "NavWorkouts")); Click(Named<Button>(window, "AddWorkoutRoutine")); var dialog = Assert.Single(window.OwnedWindows);
        Named<TextBox>(dialog, "RoutineTitle").Text = "Push day"; Named<TextBox>(dialog, "ExerciseName").Text = "Bench press";
        Click(Named<Button>(dialog, "AddExercise")); dialog.GetVisualDescendants().OfType<TextBox>().Where(x => x.Name == "ExerciseName").Last().Text = "Shoulder press";
        Capture(dialog, "workout-editor"); Click(Named<Button>(dialog, "SaveEntry"));
        var routine = Assert.Single(repo.Snapshot().Routines); Assert.Equal(2, routine.Exercises.Count); Assert.Empty(repo.Snapshot().Logs); Capture(window, "workout-schedule");
        Click(Named<Button>(window, "EditRoutine")); dialog = Assert.Single(window.OwnedWindows); Named<TextBox>(dialog, "RoutineTitle").Text = "Upper body";
        Click(dialog.GetVisualDescendants().OfType<Button>().First(x => x.Name == "RemoveExercise")); Click(Named<Button>(dialog, "SaveEntry"));
        routine = Assert.Single(repo.Snapshot().Routines); Assert.Equal("Upper body", routine.Title); Assert.Single(routine.Exercises); window.Close();
    }
    [AvaloniaFact]
    public void ArabicRtlPersistsAndAllPagesRenderWithTheNewTheme()
    {
        var repo = RepositoryTests.NewRepository(); var window = new MainWindow(repo); window.Show(); Dispatcher.UIThread.RunJobs();
        Click(Named<Button>(window, "NavSettings")); Click(window.GetVisualDescendants().OfType<Button>().Single(x => x.Content is string s && s == "العربية"));
        Assert.Equal("ar", repo.Snapshot().Preferences.Language); Assert.Equal(Avalonia.Media.FlowDirection.RightToLeft, window.FlowDirection); Assert.Equal(SystemDecorations.None, window.SystemDecorations);
        foreach (var page in new[] { "Settings", "Dashboard", "Calendar", "Workouts", "Weight", "Measurements", "ProgressPhotos", "GoalsRecords", "Journey", "Statistics" })
        {
            Click(Named<Button>(window, "Nav" + page)); Capture(window, "arabic-" + page.ToLowerInvariant()); Assert.Empty(window.OwnedWindows);
        }
        Click(Named<Button>(window, "LogToday")); var dialog = Assert.Single(window.OwnedWindows); Named<ComboBox>(dialog, "DayStatus").SelectedIndex = 1;
        Named<TextBox>(dialog, "WorkoutName").Focus(); Capture(dialog, "arabic-check-in-focused"); dialog.Close(); window.Close();
    }
    [AvaloniaFact]
    public async Task AlbumsBatchPhotosComparisonAndZoomWork()
    {
        var repo = RepositoryTests.NewRepository(); var day = DateOnly.FromDateTime(DateTime.Today);
        repo.Save("album", "album", new PhotoAlbum("album", "September check-in", day));
        repo.Save("preferences", "main", new Preferences { Language = "ar" });
        var window = new MainWindow(repo); window.Show(); Dispatcher.UIThread.RunJobs(); Click(Named<Button>(window, "NavProgressPhotos"));
        Click(Named<Button>(window, "OpenAlbum"));
        using var asset = Avalonia.Platform.AssetLoader.Open(new Uri("avares://Fitlog/Assets/fitlog-logo.png")); using var memory = new MemoryStream(); asset.CopyTo(memory);
        var pending = window.SavePhotoBatch([memory.ToArray(), memory.ToArray()]); Dispatcher.UIThread.RunJobs(); var dialog = Assert.Single(window.OwnedWindows); Click(Named<Button>(dialog, "SaveEntry")); await pending;
        Assert.Equal(2, repo.Snapshot().Photos.Count); Assert.All(repo.Snapshot().Photos, photo => Assert.Equal("album", photo.AlbumId));
        Assert.All(window.GetVisualDescendants().OfType<Image>(), image => Assert.Equal(Avalonia.Media.FlowDirection.LeftToRight, image.FlowDirection)); Capture(window, "photo-album-arabic");
        foreach (var check in window.GetVisualDescendants().OfType<CheckBox>().Where(x => x.Name == "SelectPhoto").ToList()) check.IsChecked = true;
        Click(Named<Button>(window, "ComparePhotos")); dialog = Assert.Single(window.OwnedWindows); Assert.Equal(2, dialog.GetVisualDescendants().OfType<Image>().Count(x => x.Name == "ComparisonImage")); Capture(dialog, "photo-comparison"); dialog.Close(); Dispatcher.UIThread.RunJobs();
        Click(window.GetVisualDescendants().OfType<Button>().First(x => x.Name == "OpenPhoto")); dialog = Assert.Single(window.OwnedWindows);
        var picture = Named<Image>(dialog, "FullPhoto"); var width = picture.Width; Named<Slider>(dialog, "PhotoZoom").Value = 2; Dispatcher.UIThread.RunJobs(); Assert.True(picture.Width > width); Capture(dialog, "photo-zoom"); dialog.Close(); window.Close();
    }
    private sealed class TestClock(DateTimeOffset utc) : TimeProvider
    {
        public DateTimeOffset Utc { get; set; } = utc;
        public override DateTimeOffset GetUtcNow() => Utc;
        public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.CreateCustomTimeZone("Test Baghdad", TimeSpan.FromHours(3), "Baghdad", "Baghdad");
    }
    [AvaloniaFact]
    public void AllPagesRenderWithRealStoredDataAtDesktopAndCompactSizes()
    {
        var repo = RepositoryTests.NewRepository(); var today = DateOnly.FromDateTime(DateTime.Today);
        for (var day = new DateOnly(today.Year, 1, 1); day <= today; day = day.AddDays(1))
        {
            if (day.Day % 9 != 0) repo.Save("log", day.ToString("yyyy-MM-dd"), new DailyLog { Date = day, Gym = day.DayOfWeek is DayOfWeek.Monday or DayOfWeek.Wednesday or DayOfWeek.Friday, Diet = day.Day % 7 != 0, Steps = 7600, Water = 2.2, Sleep = 7.5, Workout = "Strength training", Minutes = 45 });
        }
        for (int i = 24; i >= 0; i--) { var day = today.AddDays(-i * 8); repo.Save("weight", day.ToString("yyyy-MM-dd"), new WeightEntry(day, 79.9 + i * .27)); }
        var window = new MainWindow(repo); window.Show(); Capture(window, "dashboard");
        foreach (var page in new[] { "Calendar", "Weight", "Workouts", "Measurements", "ProgressPhotos", "GoalsRecords", "Journey", "Statistics", "Settings" })
        {
            Click(Named<Button>(window, "Nav" + page)); Capture(window, page.ToLowerInvariant()); Assert.Empty(window.OwnedWindows);
        }
        window.Width = 1000; window.Height = 720; Click(Named<Button>(window, "NavDashboard")); Capture(window, "dashboard-compact");
        Click(Named<Button>(window, "NavSettings")); var light = window.GetVisualDescendants().OfType<Button>().Single(x => x.Content is string s && s == "Light"); Click(light); Assert.True(repo.Snapshot().Preferences.Light); Capture(window, "settings-light");
        window.Close();
    }
}
