using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Fitlog.Models;

namespace Fitlog;

public sealed partial class MainWindow
{
    private Control NutritionPage()
    {
        var page = Stack(UiMetrics.Xl);
        var add = Button("＋  Add food course", () => EditNutritionPlan(), true);
        add.Name = "AddNutritionPlan";
        page.Children.Add(Split(Heading("Food schedule", "Your nutrition courses", "Plan your meals, times and portions in one place."), add));

        if (_data.NutritionPlans.Count == 0)
        {
            page.Children.Add(Empty("Start a food course", "Name your course, then add each meal with its time and portion.", "Add food course", () => EditNutritionPlan()));
            return page;
        }

        foreach (var plan in _data.NutritionPlans.OrderByDescending(x => x.CreatedOn).ThenBy(x => x.CourseName))
        {
            var content = Stack(UiMetrics.Lg);
            var title = Stack(UiMetrics.Sm);
            title.Children.Add(T(plan.CourseName, 24, true));
            title.Children.Add(T($"{Tr("Created on")} {DateText(plan.CreatedOn)}   ·   {plan.Meals.Count} {Tr(plan.Meals.Count == 1 ? "Meal" : "Meals")}", 12, color: Muted));
            var actions = new StackPanel { Orientation = Orientation.Horizontal, Spacing = UiMetrics.Sm };
            var edit = Button("Edit", () => EditNutritionPlan(plan)); edit.Name = "EditNutritionPlan"; actions.Children.Add(edit);
            actions.Children.Add(Button("Delete", () => DeleteRecord("nutrition", plan.Id)));
            content.Children.Add(Split(title, actions));

            var meals = Stack(UiMetrics.Sm);
            var number = 0;
            foreach (var meal in plan.Meals.OrderBy(x => x.Time))
            {
                number++;
                var row = new Grid { ColumnDefinitions = new("44,84,*"), ColumnSpacing = UiMetrics.Md, VerticalAlignment = VerticalAlignment.Center };
                var sequence = T(number.ToString("00", CultureInfo.InvariantCulture), 12, true, Accent);
                var time = T(meal.Time.ToString("HH:mm", CultureInfo.InvariantCulture), 14, true);
                var details = Stack(UiMetrics.Sm);
                details.Children.Add(T(meal.Name, 15, true));
                var ingredients = new WrapPanel();
                foreach (var item in meal.EffectiveIngredients)
                    ingredients.Children.Add(new Border { Background = Raised, CornerRadius = new(UiMetrics.ControlRadius), Padding = new Thickness(10, 5), Margin = new Thickness(0, 0, 6, 6), Child = T($"{item.Name}  ·  {item.Amount:0.#} {Tr(item.Unit == MealUnit.Grams ? "g" : "tbsp")}", 12) });
                details.Children.Add(ingredients);
                Grid.SetColumn(time, 1); Grid.SetColumn(details, 2);
                row.Children.Add(sequence); row.Children.Add(time); row.Children.Add(details);
                meals.Children.Add(new Border { Background = InputSurface, CornerRadius = new(UiMetrics.ControlRadius), Padding = new Thickness(UiMetrics.Lg, UiMetrics.Md), Child = row });
            }
            content.Children.Add(meals);
            page.Children.Add(Card(content));
        }
        return page;
    }

    private async Task EditNutritionPlan(NutritionPlan? plan = null)
    {
        var dialog = Dialog(plan == null ? "Add food course" : "Edit food course");
        dialog.Width = 780;
        var form = Stack(UiMetrics.Xl);
        form.Children.Add(Heading("Food schedule", plan == null ? "Create a food course" : "Edit food course", "Add meals in the order that works for your day."));
        var course = new TextBox { Name = "NutritionCourse", Text = plan?.CourseName ?? "", Watermark = "e.g. My daily course", MaxLength = 100 };
        form.Children.Add(Field("Course name", course));
        var count = Number(plan?.Meals.Count ?? 1, 1, 30);
        count.Name = "MealCount";
        form.Children.Add(Field("Number of meals", count));
        form.Children.Add(T("Meals", 20, true));
        var mealList = Stack(UiMetrics.Md);
        var editors = new List<MealEditor>();
        var removeButtons = new List<Button>();
        var labels = new List<TextBlock>();
        void UpdateRemoveButtons()
        {
            foreach (var button in removeButtons) button.IsEnabled = editors.Count > 1;
            for (var i = 0; i < labels.Count; i++) labels[i].Text = $"{Tr("Meal")} {i + 1:00}";
        }
        void AddMeal(PlannedMeal? meal)
        {
            if (editors.Count >= 30) return;
            var editor = new MealEditor(this, meal, editors.Count);
            editors.Add(editor);
            var block = Stack(UiMetrics.Md);
            var card = Card(block, UiMetrics.Lg);
            var label = T("Meal", 14, true, Accent);
            Button remove = null!;
            remove = Button("Remove meal", () => { editors.Remove(editor); mealList.Children.Remove(card); removeButtons.Remove(remove); labels.Remove(label); count.Value = editors.Count; UpdateRemoveButtons(); });
            remove.Name = "RemoveMeal";
            block.Children.Add(Split(label, remove));
            var fields = new Grid { ColumnDefinitions = new("2*,*"), ColumnSpacing = UiMetrics.Md };
            var inputs = new Control[] { Field("Meal name", editor.Name), Field("Time · 24h", editor.Time) };
            for (var i = 0; i < inputs.Length; i++) { Grid.SetColumn(inputs[i], i); fields.Children.Add(inputs[i]); }
            block.Children.Add(fields);
            block.Children.Add(T("Food items", 14, true));
            block.Children.Add(editor.IngredientRows);
            var addItem = Button("＋  Add food item", () => editor.AddIngredient(null)); addItem.Name = "AddIngredient"; block.Children.Add(addItem);
            mealList.Children.Add(card);
            removeButtons.Add(remove);
            labels.Add(label);
            UpdateRemoveButtons();
        }
        foreach (var meal in plan?.Meals ?? [new PlannedMeal("", new TimeOnly(8, 0), 100, MealUnit.Grams)]) AddMeal(meal);
        count.ValueChanged += (_, _) =>
        {
            var target = (int)(count.Value ?? 1);
            while (editors.Count < target) AddMeal(null);
            while (editors.Count > target)
            {
                editors.RemoveAt(editors.Count - 1);
                mealList.Children.RemoveAt(mealList.Children.Count - 1);
                removeButtons.RemoveAt(removeButtons.Count - 1);
                labels.RemoveAt(labels.Count - 1);
            }
            UpdateRemoveButtons();
        };
        form.Children.Add(mealList);
        var add = Button("＋  Add meal", () => count.Value = Math.Min(30, editors.Count + 1)); add.Name = "AddMeal"; form.Children.Add(add);
        EditorContent(dialog, form, () =>
        {
            if (string.IsNullOrWhiteSpace(course.Text)) throw new InvalidDataException("Enter a course name.");
            if (editors.Count == 0) throw new InvalidDataException("Add at least one meal.");
            var meals = editors.Select(x => x.Read()).ToList();
            var id = plan?.Id ?? Guid.NewGuid().ToString("N");
            _repository.Save("nutrition", id, new NutritionPlan { Id = id, CourseName = course.Text.Trim(), CreatedOn = plan?.CreatedOn ?? Today, Meals = meals });
        });
        await dialog.ShowDialog(this);
    }

    private sealed class MealEditor
    {
        public TextBox Name { get; }
        public TextBox Time { get; }
        public StackPanel IngredientRows { get; } = Stack(UiMetrics.Sm);
        private readonly MainWindow _owner;
        private readonly List<IngredientEditor> _ingredients = [];
        private readonly List<Button> _removeButtons = [];

        public MealEditor(MainWindow owner, PlannedMeal? meal, int index)
        {
            _owner = owner;
            Name = new TextBox { Name = "MealName", Text = meal?.Name ?? "", Watermark = "e.g. Breakfast", MaxLength = 100 };
            Time = new TextBox { Name = "MealTime", Text = (meal?.Time ?? new TimeOnly((8 + index * 3) % 24, 0)).ToString("HH:mm", CultureInfo.InvariantCulture), Watermark = "08:00", MaxLength = 5 };
            foreach (var item in meal?.EffectiveIngredients ?? [new MealIngredient("", 100, MealUnit.Grams)]) AddIngredient(item);
        }

        public void AddIngredient(MealIngredient? item)
        {
            if (_ingredients.Count >= 30) return;
            var editor = new IngredientEditor(_owner, item);
            _ingredients.Add(editor);
            var row = new Grid { ColumnDefinitions = new("2*,*,*,Auto"), ColumnSpacing = UiMetrics.Sm };
            var controls = new Control[] { _owner.Field("Food item", editor.Name), _owner.Field("Amount", editor.Amount), _owner.Field("Unit", editor.Unit) };
            for (var i = 0; i < controls.Length; i++) { Grid.SetColumn(controls[i], i); row.Children.Add(controls[i]); }
            Button remove = null!;
            remove = _owner.Button("Remove", () => { _ingredients.Remove(editor); IngredientRows.Children.Remove(row); _removeButtons.Remove(remove); UpdateRemoveButtons(); });
            remove.Name = "RemoveIngredient"; Grid.SetColumn(remove, 3); row.Children.Add(remove);
            IngredientRows.Children.Add(row); _removeButtons.Add(remove); UpdateRemoveButtons();
        }

        private void UpdateRemoveButtons()
        {
            foreach (var button in _removeButtons) button.IsEnabled = _ingredients.Count > 1;
        }

        public PlannedMeal Read()
        {
            if (string.IsNullOrWhiteSpace(Name.Text)) throw new InvalidDataException("Enter a name for every meal.");
            if (!TimeOnly.TryParseExact(Time.Text, "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out var time)) throw new InvalidDataException("Enter a meal time as HH:mm.");
            var items = _ingredients.Select(x => x.Read()).ToList();
            var first = items[0];
            return new PlannedMeal(Name.Text.Trim(), time, first.Amount, first.Unit) { Ingredients = items };
        }
    }

    private sealed class IngredientEditor
    {
        public TextBox Name { get; }
        public NumericUpDown Amount { get; }
        public ComboBox Unit { get; }
        public IngredientEditor(MainWindow owner, MealIngredient? item)
        {
            Name = new TextBox { Name = "IngredientName", Text = item?.Name ?? "", Watermark = "e.g. Oats", MaxLength = 100 };
            Amount = Number(item?.Amount ?? 100, .1, 10000, .1); Amount.Name = "MealAmount";
            Unit = owner.SelectOptions(["g", "tbsp"], (int)(item?.Unit ?? MealUnit.Grams)); Unit.Name = "MealUnit";
        }
        public MealIngredient Read()
        {
            if (string.IsNullOrWhiteSpace(Name.Text)) throw new InvalidDataException("Enter a name for every food item.");
            return new MealIngredient(Name.Text.Trim(), Value(Amount), (MealUnit)Unit.SelectedIndex);
        }
    }
}
