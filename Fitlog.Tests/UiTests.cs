using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Fitlog.Controls;
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
        Named<ChoiceBar>(dialog, "DayStatus").Choose(Attendance.Training); Named<ChoiceBar>(dialog, "DietCompletion").Choose(100);
        Named<TextBox>(dialog, "WorkoutName").Text = "Upper body"; Named<TextBox>(dialog, "DailyNotes").Text = "Feeling strong";
        Capture(dialog, "check-in"); Click(Named<Button>(dialog, "SaveEntry"));
        var saved = Assert.Single(repo.Snapshot().Logs); Assert.True(saved.Gym); Assert.True(saved.Diet); Assert.Equal("Feeling strong", saved.Notes);
        Click(Named<Button>(window, "NavWeight"));
        var add = window.GetVisualDescendants().OfType<Button>().Single(x => x.Content is string s && s.Contains("Add weight")); Click(add);
        var weightDialog = Assert.Single(window.OwnedWindows); Named<NumericUpDown>(weightDialog, "WeightValue").Value = 79.9m; Click(Named<Button>(weightDialog, "SaveEntry"));
        Assert.Equal(79.9, Assert.Single(repo.Snapshot().Weights).Kilograms); window.Close();
    }
    [AvaloniaFact]
    public void HeaderTodayActionSwitchesToEditOnceTodayIsLogged()
    {
        var repo = RepositoryTests.NewRepository(); var window = new MainWindow(repo); window.Show(); Dispatcher.UIThread.RunJobs();
        Assert.Equal("＋  Log Today", Named<Button>(window, "LogToday").Content);
        Assert.DoesNotContain(window.GetVisualDescendants().OfType<Button>(), x => x.Content is string s && s == "Edit today");
        Click(Named<Button>(window, "LogToday")); var dialog = Assert.Single(window.OwnedWindows);
        Named<ChoiceBar>(dialog, "DayStatus").Choose(Attendance.Rest); Click(Named<Button>(dialog, "SaveEntry"));
        Assert.Equal("Edit today", Named<Button>(window, "LogToday").Content);
        Click(Named<Button>(window, "LogToday")); dialog = Assert.Single(window.OwnedWindows); Click(Named<Button>(dialog, "SaveEntry"));
        Assert.Single(repo.Snapshot().Logs); window.Close();
    }
    [AvaloniaFact]
    public void WorkoutFieldsAreShownOnlyOnTrainingDays()
    {
        var repo = RepositoryTests.NewRepository(); var window = new MainWindow(repo); window.Show(); Dispatcher.UIThread.RunJobs();
        Click(Named<Button>(window, "LogToday")); var dialog = Assert.Single(window.OwnedWindows);
        var completion = Named<ChoiceBar>(dialog, "WorkoutCompletion");
        Named<ChoiceBar>(dialog, "DayStatus").Choose(Attendance.Training); Dispatcher.UIThread.RunJobs();
        Assert.True(completion.IsEffectivelyVisible);
        Named<ChoiceBar>(dialog, "DayStatus").Choose(Attendance.Rest); Dispatcher.UIThread.RunJobs();
        Assert.False(completion.IsEffectivelyVisible);
        Named<ChoiceBar>(dialog, "DayStatus").Choose(Attendance.Missed); Dispatcher.UIThread.RunJobs();
        Assert.False(completion.IsEffectivelyVisible);
        Click(Named<Button>(dialog, "SaveEntry")); var log = Assert.Single(repo.Snapshot().Logs);
        Assert.Equal(Attendance.Missed, log.EffectiveStatus); Assert.Equal(0, log.EffectiveWorkoutCompletion); window.Close();
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
        Named<ChoiceBar>(dialog, "DayStatus").Choose(Attendance.Rest);
        Named<ChoiceBar>(dialog, "DietCompletion").Choose(75); Click(Named<Button>(dialog, "SaveEntry"));
        var log = Assert.Single(repo.Snapshot().Logs); Assert.Equal(new DateOnly(2026, 10, 1), log.Date); Assert.Equal(Attendance.Rest, log.EffectiveStatus); Assert.False(log.Trained); Assert.Null(log.EffectiveWorkoutCompletion); Assert.Equal(75, log.EffectiveDietCompletion); window.Close();
    }
    [AvaloniaFact]
    public void TrainingRestAndMissedAreDistinctAndCompletionIsPersisted()
    {
        var repo = RepositoryTests.NewRepository(); var window = new MainWindow(repo); window.Show(); Dispatcher.UIThread.RunJobs();
        foreach (var status in new[] { Attendance.Training, Attendance.Rest, Attendance.Missed })
        {
            Click(Named<Button>(window, "LogToday")); var dialog = Assert.Single(window.OwnedWindows); Named<ChoiceBar>(dialog, "DayStatus").Choose(status);
            Named<ChoiceBar>(dialog, "WorkoutCompletion").Choose(75); Named<ChoiceBar>(dialog, "DietCompletion").Choose(75);
            Click(Named<Button>(dialog, "SaveEntry")); var log = Assert.Single(repo.Snapshot().Logs); Assert.Equal(status, log.EffectiveStatus);
            Assert.Equal(status == Attendance.Training ? 75 : status == Attendance.Missed ? 0 : (int?)null, log.EffectiveWorkoutCompletion); Assert.Equal(75, log.EffectiveDietCompletion);
        }
        var prior = repo.Snapshot().Logs.Single(); repo.Save("log", prior.Date.ToString("yyyy-MM-dd"), prior with { DietCompletion = 85 });
        window.Close(); window = new MainWindow(repo); window.Show(); Dispatcher.UIThread.RunJobs();
        Click(Named<Button>(window, "LogToday")); var legacy = Assert.Single(window.OwnedWindows);
        Assert.Equal(85, Named<ChoiceBar>(legacy, "DietCompletion").SelectedTag); Click(Named<Button>(legacy, "SaveEntry"));
        Assert.Equal(85, Assert.Single(repo.Snapshot().Logs).EffectiveDietCompletion);
        window.Close();
    }
    [AvaloniaFact]
    public void CustomWorkoutScheduleCanBeCreatedAndEditedIndependentlyOfCheckIns()
    {
        var repo = RepositoryTests.NewRepository(); var clock = new TestClock(new DateTimeOffset(2026, 9, 25, 9, 0, 0, TimeSpan.Zero)); var window = new MainWindow(repo, clock); window.Show(); Dispatcher.UIThread.RunJobs();
        Click(Named<Button>(window, "NavWorkouts")); Click(Named<Button>(window, "AddWorkoutRoutine")); var dialog = Assert.Single(window.OwnedWindows);
        Named<TextBox>(dialog, "RoutineProgram").Text = "Upper / Lower"; Named<TextBox>(dialog, "RoutineTitle").Text = "Push day"; Named<TextBox>(dialog, "ExerciseName").Text = "Bench press";
        Named<NumericUpDown>(dialog, "ExerciseWeight").Value = 60;
        Click(Named<Button>(dialog, "AddExercise")); dialog.GetVisualDescendants().OfType<TextBox>().Where(x => x.Name == "ExerciseName").Last().Text = "Shoulder press";
        Capture(dialog, "workout-editor"); Click(Named<Button>(dialog, "SaveEntry"));
        var routine = Assert.Single(repo.Snapshot().Routines); Assert.Equal("Upper / Lower", routine.ProgramName); Assert.Equal(2, routine.Exercises.Count); Assert.Empty(repo.Snapshot().Logs); Capture(window, "workout-schedule");
        clock.Utc = new DateTimeOffset(2026, 10, 5, 9, 0, 0, TimeSpan.Zero);
        Click(Named<Button>(window, "EditRoutine")); dialog = Assert.Single(window.OwnedWindows); Named<TextBox>(dialog, "RoutineTitle").Text = "Upper body";
        Click(dialog.GetVisualDescendants().OfType<Button>().Last(x => x.Name == "RemoveExercise"));
        Named<NumericUpDown>(dialog, "ExerciseWeight").Value = 66; Click(Named<Button>(dialog, "SaveEntry"));
        routine = Assert.Single(repo.Snapshot().Routines); Assert.Equal("Upper body", routine.Title); Assert.Single(routine.Exercises);
        var change = Assert.Single(ExerciseProgress.Changes(routine)); Assert.Equal(10, change.Days); Assert.Equal(10, change.Percent, 3);
        Click(Named<Button>(window, "NavGoalsRecords"));
        Assert.Contains(window.GetVisualDescendants().OfType<TextBlock>(), x => x.Text?.Contains("+10%") == true);
        Capture(window, "goals-strength-progress");
        Click(Named<Button>(window, "LogToday")); dialog = Assert.Single(window.OwnedWindows);
        Named<ChoiceBar>(dialog, "DayStatus").Choose(Attendance.Training); Named<ComboBox>(dialog, "LogRoutine").SelectedIndex = 1;
        Click(Named<Button>(dialog, "SaveEntry")); Assert.Empty(repo.Snapshot().Logs);
        Assert.Single(dialog.GetVisualDescendants().OfType<CheckBox>(), x => x.Name == "PerformedExercise").IsChecked = true;
        Click(Named<Button>(dialog, "SaveEntry")); var log = Assert.Single(repo.Snapshot().Logs);
        Assert.Equal(routine.Id, log.RoutineId); Assert.Equal(routine.Exercises[0].Name, Assert.Single(log.PerformedExercises)); window.Close();
    }
    [AvaloniaFact]
    public void NutritionCourseCanBeCreatedEditedAndDisplayedWithItsCreationDate()
    {
        var repo = RepositoryTests.NewRepository();
        var clock = new TestClock(new DateTimeOffset(2026, 9, 25, 9, 0, 0, TimeSpan.Zero));
        var window = new MainWindow(repo, clock); window.Show(); Dispatcher.UIThread.RunJobs();
        Click(Named<Button>(window, "NavNutrition")); Click(Named<Button>(window, "AddNutritionPlan"));
        var dialog = Assert.Single(window.OwnedWindows);
        Named<TextBox>(dialog, "NutritionCourse").Text = "Daily course";
        Named<TextBox>(dialog, "MealName").Text = "Breakfast";
        Named<TextBox>(dialog, "IngredientName").Text = "Oats";
        Named<TextBox>(dialog, "MealTime").Text = "07:30";
        Named<NumericUpDown>(dialog, "MealAmount").Value = 80;
        Click(Named<Button>(dialog, "AddIngredient"));
        dialog.GetVisualDescendants().OfType<TextBox>().Where(x => x.Name == "IngredientName").Last().Text = "Milk";
        dialog.GetVisualDescendants().OfType<NumericUpDown>().Where(x => x.Name == "MealAmount").Last().Value = 200;
        Named<NumericUpDown>(dialog, "MealCount").Value = 2;
        Assert.Equal(2, dialog.GetVisualDescendants().OfType<TextBox>().Count(x => x.Name == "MealName"));
        Named<NumericUpDown>(dialog, "MealCount").Value = 1;
        Assert.Single(dialog.GetVisualDescendants().OfType<TextBox>(), x => x.Name == "MealName");
        Click(Named<Button>(dialog, "AddMeal"));
        dialog.GetVisualDescendants().OfType<TextBox>().Where(x => x.Name == "MealName").Last().Text = "Snack";
        dialog.GetVisualDescendants().OfType<TextBox>().Where(x => x.Name == "IngredientName").Last().Text = "Honey";
        dialog.GetVisualDescendants().OfType<TextBox>().Where(x => x.Name == "MealTime").Last().Text = "12:00";
        dialog.GetVisualDescendants().OfType<ComboBox>().Where(x => x.Name == "MealUnit").Last().SelectedIndex = 1;
        dialog.GetVisualDescendants().OfType<NumericUpDown>().Where(x => x.Name == "MealAmount").Last().Value = 2;
        Capture(dialog, "nutrition-editor"); Click(Named<Button>(dialog, "SaveEntry"));
        var plan = Assert.Single(repo.Snapshot().NutritionPlans);
        Assert.Equal(new DateOnly(2026, 9, 25), plan.CreatedOn); Assert.Equal(2, plan.Meals.Count);
        Assert.Equal(2, plan.Meals[0].Ingredients.Count); Assert.Equal("Milk", plan.Meals[0].Ingredients[1].Name);
        Assert.Equal(MealUnit.Tablespoons, plan.Meals[1].Unit);
        Assert.Contains(window.GetVisualDescendants().OfType<TextBlock>(), x => x.Text?.Contains("25 Sep 2026") == true);
        Capture(window, "nutrition-schedule");
        Click(Named<Button>(window, "EditNutritionPlan")); dialog = Assert.Single(window.OwnedWindows);
        Named<TextBox>(dialog, "NutritionCourse").Text = "Updated course";
        Click(Named<Button>(dialog, "SaveEntry"));
        Assert.Equal("Updated course", Assert.Single(repo.Snapshot().NutritionPlans).CourseName);
        Assert.Equal(new DateOnly(2026, 9, 25), Assert.Single(repo.Snapshot().NutritionPlans).CreatedOn);
        Click(Named<Button>(window, "NavSettings"));
        Click(window.GetVisualDescendants().OfType<Button>().Single(x => x.Content is string s && s == "العربية"));
        Click(Named<Button>(window, "NavNutrition"));
        Assert.Contains(window.GetVisualDescendants().OfType<TextBlock>(), x => x.Text?.Contains("غرام") == true);
        window.Close(); window = new MainWindow(repo, clock) { Width = 900, Height = 700 }; window.Show(); Dispatcher.UIThread.RunJobs();
        Click(Named<Button>(window, "NavNutrition")); Capture(window, "nutrition-arabic-compact");
        Click(Named<Button>(window, "EditNutritionPlan")); dialog = Assert.Single(window.OwnedWindows);
        Capture(dialog, "nutrition-editor-arabic"); dialog.Close();
        window.Close();
    }
    [AvaloniaFact]
    public void ArabicRtlPersistsAndAllPagesRenderWithTheNewTheme()
    {
        var repo = RepositoryTests.NewRepository(); var window = new MainWindow(repo); window.Show(); Dispatcher.UIThread.RunJobs();
        Click(Named<Button>(window, "NavSettings")); Click(window.GetVisualDescendants().OfType<Button>().Single(x => x.Content is string s && s == "العربية"));
        Assert.Equal("ar", repo.Snapshot().Preferences.Language); Assert.Equal(Avalonia.Media.FlowDirection.RightToLeft, window.FlowDirection); Assert.Equal(SystemDecorations.Full, window.SystemDecorations);
        foreach (var page in new[] { "Settings", "Dashboard", "Calendar", "Workouts", "Weight", "Measurements", "ProgressPhotos", "GoalsRecords", "Journey", "Statistics" })
        {
            Click(Named<Button>(window, "Nav" + page)); Capture(window, "arabic-" + page.ToLowerInvariant()); Assert.Empty(window.OwnedWindows);
        }
        Click(Named<Button>(window, "LogToday")); var dialog = Assert.Single(window.OwnedWindows); Named<ChoiceBar>(dialog, "DayStatus").Choose(Attendance.Training);
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
        Click(Named<Button>(window, "ComparePhotos")); dialog = Assert.Single(window.OwnedWindows); Assert.Equal(2, dialog.GetVisualDescendants().OfType<Image>().Count(x => x.Name == "ComparisonImage")); Named<ComboBox>(dialog, "ComparisonMode").SelectedIndex = 1; Named<Slider>(dialog, "ComparisonSlider").Value = 75; Capture(dialog, "photo-comparison"); dialog.Close(); Dispatcher.UIThread.RunJobs();
        Click(window.GetVisualDescendants().OfType<Button>().First(x => x.Name == "OpenPhoto")); dialog = Assert.Single(window.OwnedWindows);
        var picture = Named<Image>(dialog, "FullPhoto"); var width = picture.Width; Named<Slider>(dialog, "PhotoZoom").Value = 2; Dispatcher.UIThread.RunJobs(); Assert.True(picture.Width > width); Capture(dialog, "photo-zoom"); dialog.Close(); Dispatcher.UIThread.RunJobs();
        Click(Named<Button>(window, "DeleteAlbum")); dialog = Assert.Single(window.OwnedWindows);
        Click(dialog.GetVisualDescendants().OfType<Button>().Single(x => x.Content is string s && s == "حذف"));
        Assert.Empty(repo.Snapshot().Albums); Assert.Equal(2, repo.Snapshot().Photos.Count); Assert.All(repo.Snapshot().Photos, photo => Assert.Null(photo.AlbumId)); window.Close();
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
        window.Width = 900; window.Height = 700;
        foreach (var page in new[] { "Dashboard", "Calendar", "Weight", "Workouts", "Measurements", "ProgressPhotos", "GoalsRecords", "Journey", "Statistics", "Settings" })
        {
            Click(Named<Button>(window, "Nav" + page)); Capture(window, page.ToLowerInvariant() + "-compact"); Assert.Empty(window.OwnedWindows);
        }
        Click(Named<Button>(window, "NavSettings")); var light = window.GetVisualDescendants().OfType<Button>().Single(x => x.Content is string s && s == "Light"); Click(light); Assert.True(repo.Snapshot().Preferences.Light); Capture(window, "settings-light");
        window.Close();
    }
}
