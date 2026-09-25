using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Fitlog.Models;
using System.Globalization;

namespace Fitlog.Controls;
public sealed class WeightChart : Control
{
    public IReadOnlyList<WeightEntry> Entries { get; set; } = [];
    public bool Pounds { get; set; }
    public bool Light { get; set; }
    public bool Arabic { get; set; }
    public override void Render(DrawingContext context)
    {
        base.Render(context);
        var muted = Brush.Parse(Light ? "#526F80" : "#A1BDCF");
        var line = Brush.Parse(Light ? "#D5E0E6" : "#304552");
        var accent = Brush.Parse(Light ? "#558CA5" : "#A3C5D5");
        void Text(string value, double x, double y) => context.DrawText(new FormattedText(value, CultureInfo.InvariantCulture, Arabic ? FlowDirection.RightToLeft : FlowDirection.LeftToRight, new Typeface("Segoe UI"), 11, muted), new Point(x, y));
        if (Entries.Count == 0) { Text(Arabic ? "سيظهر مخطط الوزن بعد تسجيل أول وزن." : "Your weight trend will appear after your first weigh-in.", 20, Bounds.Height / 2); return; }
        double left = 44, top = 20, width = Math.Max(1, Bounds.Width - 66), height = Math.Max(1, Bounds.Height - 60);
        var factor = Pounds ? 2.2046226218 : 1;
        double min = Math.Floor(Entries.Min(x => x.Kilograms * factor)) - 1, max = Math.Ceiling(Entries.Max(x => x.Kilograms * factor)) + 1;
        int first = Entries[0].Date.DayNumber, span = Math.Max(1, Entries[^1].Date.DayNumber - first);
        for (int i = 0; i < 5; i++)
        {
            double y = top + height * i / 4; context.DrawLine(new Pen(line, 1, new DashStyle(new double[] { 2, 5 }, 0)), new Point(left, y), new Point(left + width, y));
            Text((max - (max - min) * i / 4).ToString("0.#"), 5, y - 6);
        }
        Point Position(WeightEntry e) => new(left + (Entries.Count == 1 ? width / 2 : (e.Date.DayNumber - first) * width / span), top + (max - e.Kilograms * factor) / (max - min) * height);
        if (Entries.Count > 1)
        {
            var fill = new StreamGeometry(); using (var drawing = fill.Open()) { drawing.BeginFigure(new Point(Position(Entries[0]).X, top + height), true); foreach (var e in Entries) drawing.LineTo(Position(e)); drawing.LineTo(new Point(Position(Entries[^1]).X, top + height)); drawing.EndFigure(true); }
            context.DrawGeometry(new SolidColorBrush(Color.Parse("#A3C5D5"), .055), null, fill);
            for (int i = 1; i < Entries.Count; i++) context.DrawLine(new Pen(accent, 2.5), Position(Entries[i - 1]), Position(Entries[i]));
        }
        for (int i = 0; i < Entries.Count; i++) { var p = Position(Entries[i]); context.DrawEllipse(accent, null, p, i == Entries.Count - 1 ? 4 : 3, i == Entries.Count - 1 ? 4 : 3); }
        int labels = Math.Min(6, Entries.Count);
        for (int i = 0; i < labels; i++) { int index = labels == 1 ? 0 : (int)Math.Round(i * (Entries.Count - 1.0) / (labels - 1)); var p = Position(Entries[index]); Text(Entries[index].Date.ToString(Arabic ? "dd/MM" : "MMM d", CultureInfo.InvariantCulture), Math.Clamp(p.X - 18, 0, Bounds.Width - 46), top + height + 14); }
    }
}
