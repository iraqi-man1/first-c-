using System.Text.Json.Serialization;
namespace Fitlog.Models;

public enum Attendance { Unspecified, Training, Rest, Missed }

public sealed record DailyLog
{
    public DateOnly Date { get; set; } = DateOnly.FromDateTime(DateTime.Today);
    public bool Gym { get; set; }
    public bool Diet { get; set; }
    public Attendance? Status { get; set; }
    public int? WorkoutCompletion { get; set; }
    public int? DietCompletion { get; set; }
    [JsonIgnore] public Attendance EffectiveStatus => Status ?? (Gym ? Attendance.Training : Attendance.Unspecified);
    [JsonIgnore] public bool Trained => EffectiveStatus == Attendance.Training;
    [JsonIgnore] public int EffectiveDietCompletion => DietCompletion ?? (Diet ? 100 : 0);
    [JsonIgnore] public int? EffectiveWorkoutCompletion => Trained ? WorkoutCompletion ?? 100 : EffectiveStatus == Attendance.Missed ? 0 : null;
    public string Workout { get; set; } = "";
    public string? RoutineId { get; set; }
    public List<string> PerformedExercises { get; set; } = [];
    public int Minutes { get; set; }
    public int Steps { get; set; }
    public double Water { get; set; }
    public double Sleep { get; set; }
    public int Energy { get; set; } = 3;
    public string Notes { get; set; } = "";
}
public sealed record WeightEntry(DateOnly Date, double Kilograms);
public sealed record Measurement(DateOnly Date, double Waist, double Chest, double Hips);
public sealed record Goal(string Id, string Title, double Target, double Current, string Unit);
public sealed record ProgressPhoto(string Id, DateOnly Date, string Caption, string Base64)
{
    public string? AlbumId { get; set; }
}
public sealed record PhotoAlbum(string Id, string Title, DateOnly Date);
public sealed record PlannedExercise(string Name, int Sets, string Reps, double WeightKg, int RestSeconds);
public sealed record WorkoutRoutine
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public DateOnly? CreatedOn { get; set; }
    public string ProgramName { get; set; } = "My plan";
    public string Title { get; set; } = "";
    public List<DayOfWeek> Days { get; set; } = [];
    public string Notes { get; set; } = "";
    public List<PlannedExercise> Exercises { get; set; } = [];
}
public enum MealUnit { Grams, Tablespoons }
public sealed record PlannedMeal(string Name, TimeOnly Time, double Amount, MealUnit Unit);
public sealed record NutritionPlan
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string CourseName { get; set; } = "";
    public DateOnly CreatedOn { get; set; }
    public List<PlannedMeal> Meals { get; set; } = [];
}
public sealed record Preferences
{
    public bool Light { get; set; }
    public string Language { get; set; } = "en";
    public bool Pounds { get; set; }
    public bool Inches { get; set; }
    public bool SundayFirst { get; set; }
    public int WeeklyGoal { get; set; } = 4;
    public int MonthlyGoal { get; set; } = 16;
    public double TargetWeight { get; set; } = 76;
    public int DietGoal { get; set; } = 85;
    public string Heatmap { get; set; } = "Overall";
    public bool ShowEnergy { get; set; } = true;
    public bool ShowSleep { get; set; } = true;
    public bool ShowWater { get; set; } = true;
    public bool ShowSteps { get; set; } = true;
}
public sealed record Backup
{
    public int Version { get; set; } = 2;
    public List<DailyLog> Logs { get; set; } = [];
    public List<WeightEntry> Weights { get; set; } = [];
    public List<Measurement> Measurements { get; set; } = [];
    public List<Goal> Goals { get; set; } = [];
    public List<ProgressPhoto> Photos { get; set; } = [];
    public List<PhotoAlbum> Albums { get; set; } = [];
    public List<WorkoutRoutine> Routines { get; set; } = [];
    public List<NutritionPlan> NutritionPlans { get; set; } = [];
    public Preferences Preferences { get; set; } = new();
}
