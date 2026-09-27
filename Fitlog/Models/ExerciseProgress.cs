namespace Fitlog.Models;

public sealed record ExerciseLoadChange(string ExerciseName, double PreviousKg, double CurrentKg, DateOnly? Date, int? Days)
{
    public double Percent => (CurrentKg - PreviousKg) / PreviousKg * 100;
}

public static class ExerciseProgress
{
    public static List<ExerciseLoadEntry> RecordChanges(WorkoutRoutine? previous, IReadOnlyList<PlannedExercise> exercises, DateOnly today)
    {
        var history = previous?.LoadHistory.ToList() ?? [];
        var oldExercises = previous?.Exercises.ToDictionary(e => e.Id) ?? new Dictionary<string, PlannedExercise>();
        foreach (var exercise in exercises)
        {
            if (exercise.WeightKg <= 0) continue;
            oldExercises.TryGetValue(exercise.Id, out var old);
            if (old != null && Math.Abs(old.WeightKg - exercise.WeightKg) < .0001) continue;
            if (old is { WeightKg: > 0 } && !history.Any(h => h.ExerciseId == exercise.Id))
                history.Add(new(exercise.Id, old.Name, previous?.CreatedOn, old.WeightKg));
            history.Add(new(exercise.Id, exercise.Name, today, exercise.WeightKg));
        }
        return history;
    }

    public static IReadOnlyList<ExerciseLoadChange> Changes(WorkoutRoutine routine)
    {
        var changes = new List<ExerciseLoadChange>();
        foreach (var entries in routine.LoadHistory.GroupBy(x => x.ExerciseId))
        {
            ExerciseLoadEntry? prior = null;
            foreach (var entry in entries)
            {
                if (prior != null && Math.Abs(prior.WeightKg - entry.WeightKg) >= .0001)
                {
                    var days = prior.Date is { } from && entry.Date is { } to ? (int?)(to.DayNumber - from.DayNumber) : null;
                    changes.Add(new(entry.ExerciseName, prior.WeightKg, entry.WeightKg, entry.Date, days));
                }
                prior = entry;
            }
        }
        return changes.OrderByDescending(x => x.Date).ToList();
    }
}
