using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;

namespace Fitlog.Controls;
public sealed class ChoiceBar : Border
{
    private readonly List<(object Tag, Button Button)> _items = [];
    private readonly IBrush _surface, _line, _muted, _accent, _selectedInk;
    public object? SelectedTag { get; private set; }
    public event EventHandler? SelectionChanged;
    public ChoiceBar(IReadOnlyList<(string Label, object Tag)> options, object? selected, bool light)
    {
        _surface = Brush.Parse(light ? "#F3F6F8" : "#15232D");
        _line = Brush.Parse(light ? "#D5E0E6" : "#304552");
        _muted = Brush.Parse(light ? "#526F80" : "#A1BDCF");
        _accent = Brush.Parse("#A3C5D5");
        _selectedInk = Brush.Parse("#101820");
        var grid = new Grid { ColumnSpacing = UiMetrics.Xs, ColumnDefinitions = new ColumnDefinitions(string.Join(",", Enumerable.Repeat("*", options.Count))) };
        for (var i = 0; i < options.Count; i++)
        {
            var (label, tag) = options[i];
            var button = new Button
            {
                Content = label, HorizontalAlignment = HorizontalAlignment.Stretch, HorizontalContentAlignment = HorizontalAlignment.Center,
                MinHeight = 36, Padding = new Thickness(6, 6), BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(10), FontSize = 13
            };
            button.Click += (_, _) => Choose(tag);
            Grid.SetColumn(button, i); grid.Children.Add(button); _items.Add((tag, button));
        }
        Child = grid;
        Apply(selected);
    }
    public void Choose(object tag)
    {
        if (Equals(SelectedTag, tag)) return;
        Apply(tag); SelectionChanged?.Invoke(this, EventArgs.Empty);
    }
    private void Apply(object? tag)
    {
        SelectedTag = tag;
        foreach (var (itemTag, button) in _items)
        {
            var selected = Equals(itemTag, tag);
            button.Background = selected ? _accent : _surface;
            button.BorderBrush = selected ? _accent : _line;
            button.Foreground = selected ? _selectedInk : _muted;
            button.FontWeight = selected ? FontWeight.SemiBold : FontWeight.Normal;
        }
    }
}
