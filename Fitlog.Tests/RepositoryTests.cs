using System.Text.Json;
using Fitlog.Data;
using Fitlog.Models;
using Xunit;

namespace Fitlog.Tests;
public sealed class RepositoryTests
{
    internal static FitnessRepository NewRepository() => new(Path.Combine(Path.GetTempPath(), "FitlogTests", Guid.NewGuid().ToString("N"), "fitlog.db"));
    [Fact]
    public void CheckInAndWeightSurviveReopeningAndSameDateUpdates()
    {
        var repo = NewRepository(); var day = new DateOnly(2026, 9, 25);
        repo.Save("log", day.ToString("yyyy-MM-dd"), new DailyLog { Date = day, Gym = true, Diet = true, Notes = "ممتاز اليوم", Steps = 6000 });
        repo.Save("weight", day.ToString("yyyy-MM-dd"), new WeightEntry(day, 80));
        repo.Save("weight", day.ToString("yyyy-MM-dd"), new WeightEntry(day, 79.9));
        var reopened = new FitnessRepository(repo.DatabasePath).Snapshot();
        Assert.Single(reopened.Logs); Assert.Equal("ممتاز اليوم", reopened.Logs[0].Notes); Assert.True(reopened.Logs[0].Gym);
        Assert.Single(reopened.Weights); Assert.Equal(79.9, reopened.Weights[0].Kilograms);
    }
    [Fact]
    public void BackupRoundTripsEveryRecordTypeAndMergesUnrelatedRecords()
    {
        var repo = NewRepository(); var day = new DateOnly(2026, 9, 25);
        repo.Save("log", day.ToString("yyyy-MM-dd"), new DailyLog { Date = day, Gym = true });
        repo.Save("weight", day.ToString("yyyy-MM-dd"), new WeightEntry(day, 79.9));
        repo.Save("measurement", day.ToString("yyyy-MM-dd"), new Measurement(day, 80, 95, 90));
        repo.Save("goal", "goal", new Goal("goal", "Run", 10, 5, "km"));
        repo.Save("photo", "photo", new ProgressPhoto("photo", day, "Progress", "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+aXioAAAAASUVORK5CYII="));
        repo.Save("preferences", "main", new Preferences { Pounds = true, Light = true, WeeklyGoal = 3 });
        var target = NewRepository(); var other = day.AddDays(-1); target.Save("log", other.ToString("yyyy-MM-dd"), new DailyLog { Date = other });
        target.Import(repo.Export()); var result = target.Snapshot();
        Assert.Equal(2, result.Logs.Count); Assert.Single(result.Weights); Assert.Single(result.Measurements); Assert.Single(result.Goals); Assert.Single(result.Photos); Assert.True(result.Preferences.Pounds); Assert.True(result.Preferences.Light);
        target.Import(repo.Export()); Assert.Equal(2, target.Snapshot().Logs.Count);
    }
    [Fact]
    public void InvalidImportDoesNotPartiallyWriteOrChangePreferences()
    {
        var repo = NewRepository(); var before = repo.Export();
        var data = new Backup { Logs = [new DailyLog { Gym = true }], Weights = [new WeightEntry(new DateOnly(2026, 9, 25), -2)] };
        Assert.Throws<InvalidDataException>(() => repo.Import(JsonSerializer.Serialize(data))); Assert.Equal(before, repo.Export());
        Assert.Throws<InvalidDataException>(() => repo.Import("{}")); Assert.Equal(before, repo.Export());
    }
    [Fact]
    public void DeleteOnlyRemovesSelectedRecord()
    {
        var repo = NewRepository(); var day = new DateOnly(2026, 9, 25);
        repo.Save("log", "2026-09-25", new DailyLog { Date = day }); repo.Save("weight", "2026-09-25", new WeightEntry(day, 80));
        repo.Delete("log", "2026-09-25"); Assert.Empty(repo.Snapshot().Logs); Assert.Single(repo.Snapshot().Weights);
    }
    [Fact]
    public void InvalidNumericValuesAreRejected()
    {
        var repo = NewRepository(); Assert.Throws<InvalidDataException>(() => repo.Save("weight", "bad", new WeightEntry(DateOnly.FromDateTime(DateTime.Today), double.NaN)));
        Assert.Throws<InvalidDataException>(() => repo.Save("preferences", "main", new Preferences { WeeklyGoal = 0 }));
        Assert.Throws<InvalidDataException>(() => repo.Save("log", "bad", new DailyLog { Sleep = 25 }));
    }
    [Fact]
    public void VersionOneBackupRemainsReadableAndDoesNotInventRestDays()
    {
        var repo = NewRepository();
        repo.Import("""{"Version":1,"Logs":[{"Date":"2026-09-25","Gym":false,"Diet":true,"Energy":3}],"Weights":[],"Measurements":[],"Goals":[],"Photos":[],"Preferences":{}}""");
        var data = repo.Snapshot(); var log = Assert.Single(data.Logs);
        Assert.Equal(Attendance.Unspecified, log.EffectiveStatus); Assert.Equal(100, log.EffectiveDietCompletion); Assert.Null(log.EffectiveWorkoutCompletion); Assert.Empty(data.Albums); Assert.Empty(data.Routines);
    }
    [Fact]
    public void PlansAlbumsAndPartialComplianceRoundTripWithoutMixingWithAttendance()
    {
        var repo = NewRepository(); var day = new DateOnly(2026, 9, 25);
        repo.Save("routine", "plan", new WorkoutRoutine { Id = "plan", Title = "Push", Days = [DayOfWeek.Monday, DayOfWeek.Thursday], Exercises = [new("Bench", 3, "8–12", 60, 90)] });
        repo.Save("album", "album", new PhotoAlbum("album", "September", day));
        repo.Save("log", "2026-09-25", new DailyLog { Date = day, Status = Attendance.Training, Gym = true, WorkoutCompletion = 70, DietCompletion = 85 });
        repo.Save("preferences", "main", new Preferences { Language = "ar" });
        var target = NewRepository(); target.Import(repo.Export()); var snapshot = target.Snapshot();
        Assert.Single(snapshot.Routines); Assert.Single(snapshot.Albums); Assert.Single(snapshot.Logs); Assert.Equal(70, snapshot.Logs[0].EffectiveWorkoutCompletion); Assert.Equal(85, snapshot.Logs[0].EffectiveDietCompletion); Assert.Equal("ar", snapshot.Preferences.Language);
        target.Delete("routine", "plan"); Assert.Single(target.Snapshot().Logs);
    }
    [Fact]
    public void InvalidPhotoBatchAndInvalidCompletionDoNotPartiallyWrite()
    {
        var repo = NewRepository(); var day = new DateOnly(2026, 9, 25);
        Assert.Throws<InvalidDataException>(() => repo.SavePhotos([new ProgressPhoto("one", day, "", "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+aXioAAAAASUVORK5CYII="), new ProgressPhoto("two", day, "", "invalid")]));
        Assert.Empty(repo.Snapshot().Photos);
        Assert.Throws<InvalidDataException>(() => repo.Save("log", "2026-09-25", new DailyLog { Date = day, WorkoutCompletion = 101 }));
        Assert.Throws<InvalidDataException>(() => repo.Save("routine", "empty", new WorkoutRoutine { Title = "Empty", Days = [DayOfWeek.Monday] }));
    }
    [Fact]
    public void WeekBoundariesStreaksAndSparseWeightHistoryAreCorrect()
    {
        var today = new DateOnly(2026, 9, 25);
        Assert.Equal(new DateOnly(2026, 9, 21), Metrics.WeekStart(today, false)); Assert.Equal(new DateOnly(2026, 9, 20), Metrics.WeekStart(today, true));
        List<DailyLog> logs = [new() { Date = today.AddDays(-1), Gym = true }, new() { Date = today.AddDays(-2), Gym = true }, new() { Date = today.AddDays(-4), Gym = true }];
        Assert.Equal(2, Metrics.Streak(logs, today)); logs.Add(new DailyLog { Date = today, Gym = true }); Assert.Equal(3, Metrics.Streak(logs, today));
        List<WeightEntry> weights = [new(today.AddDays(-10), 81), new(today.AddDays(-2), 80)]; Assert.Equal(-1d, Metrics.WeightChange(weights, 7, today)); Assert.Null(Metrics.WeightChange(weights, 30, today));
    }
}
