using System.Runtime.InteropServices;
using Avalonia.Controls;

namespace Fitlog;

internal static class WindowsChrome
{
    private const int UseImmersiveDarkMode = 20;
    private const int CaptionColor = 35;
    private const int TextColor = 36;

    [DllImport("dwmapi.dll", ExactSpelling = true)]
    private static extern int DwmSetWindowAttribute(nint window, int attribute, ref int value, int size);

    public static void Apply(Window window, bool light)
    {
        if (!OperatingSystem.IsWindows() || window.TryGetPlatformHandle() is not { Handle: var handle } || handle == 0) return;
        var dark = light ? 0 : 1;
        var caption = light ? 0x00FFFFFF : 0x0032281B; // COLORREF uses blue, green, red.
        var text = light ? 0x003D301B : 0x00F7F4F0;
        _ = DwmSetWindowAttribute(handle, UseImmersiveDarkMode, ref dark, sizeof(int));
        _ = DwmSetWindowAttribute(handle, CaptionColor, ref caption, sizeof(int));
        _ = DwmSetWindowAttribute(handle, TextColor, ref text, sizeof(int));
    }
}
