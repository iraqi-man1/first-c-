using Avalonia;

namespace Fitlog;

internal static class UiMetrics
{
    public const double Xs = 4;
    public const double Sm = 8;
    public const double Md = 12;
    public const double Lg = 16;
    public const double Xl = 24;
    public const double Xxl = 32;

    public const double ControlHeight = 40;
    public const double NavigationHeight = 44;
    public const double IconSize = 18;
    public const double CardRadius = 16;
    public const double ControlRadius = 8;
    public const double SidebarWidth = 232;
    public const double HeaderHeight = 68;
    public const double PageGutter = Xl;
    public static readonly Thickness PageMargin = new(PageGutter, Xl, PageGutter, Xl);
}
