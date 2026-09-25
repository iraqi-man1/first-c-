using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;

namespace Fitlog;
public partial class App : Application
{
    public override void Initialize()
    {
        // Keep the English reference UI consistent on Arabic Windows installations.
        System.Globalization.CultureInfo.CurrentCulture = System.Globalization.CultureInfo.GetCultureInfo("en-US");
        System.Globalization.CultureInfo.CurrentUICulture = System.Globalization.CultureInfo.GetCultureInfo("en-US");
        AvaloniaXamlLoader.Load(this);
    }
    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            desktop.MainWindow = new MainWindow(new Data.FitnessRepository(Path.Combine(Program.DataDirectory, "fitlog.db")));
        base.OnFrameworkInitializationCompleted();
    }
}
