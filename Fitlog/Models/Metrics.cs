namespace Fitlog.Models;
public static class Metrics
{
    public static DateOnly WeekStart(DateOnly day, bool sunday) => day.AddDays(-(((int)day.DayOfWeek - (sunday ? 0 : 1) + 7) % 7));
    public static int Streak(IEnumerable<DailyLog> logs, DateOnly today)
    {
        var days = logs.Where(x => x.Trained).Select(x => x.Date).ToHashSet();
        var day = days.Contains(today) ? today : today.AddDays(-1); var count = 0;
        while (days.Contains(day)) { count++; day = day.AddDays(-1); }
        return count;
    }
    public static double? WeightChange(IEnumerable<WeightEntry> weights, int days, DateOnly today)
    {
        var list = weights.Where(x => x.Date <= today).OrderBy(x => x.Date).ToList();
        var prior = list.LastOrDefault(x => x.Date <= today.AddDays(-days));
        return prior == null || list.Count == 0 ? null : list[^1].Kilograms - prior.Kilograms;
    }
}
