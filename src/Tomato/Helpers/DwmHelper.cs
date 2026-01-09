using System;
using System.Runtime.InteropServices;

namespace Tomato.Helpers;

/// <summary>
/// Helper class for Desktop Window Manager (DWM) interop.
/// Provides functionality to customize window title bar appearance.
/// </summary>
public static class DwmHelper
{
    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(
        IntPtr hwnd,
        int attr,
        ref int attrValue,
        int attrSize);

    private const int DWMWA_USE_IMMERSIVE_DARK_MODE_BEFORE_20H1 = 19;
    private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;

    /// <summary>
    /// Enables or disables dark mode for the window title bar.
    /// </summary>
    /// <param name="hwnd">The window handle.</param>
    /// <param name="enable">True to enable dark mode, false for light mode.</param>
    /// <returns>True if the operation was attempted, false if not supported.</returns>
    public static bool SetDarkTitleBar(IntPtr hwnd, bool enable)
    {
        if (hwnd == IntPtr.Zero)
            return false;

        if (!IsWindows10OrGreater(17763))
            return false;

        int attribute = IsWindows10OrGreater(18985)
            ? DWMWA_USE_IMMERSIVE_DARK_MODE
            : DWMWA_USE_IMMERSIVE_DARK_MODE_BEFORE_20H1;

        int value = enable ? 1 : 0;
        DwmSetWindowAttribute(hwnd, attribute, ref value, sizeof(int));
        return true;
    }

    /// <summary>
    /// Checks if the current OS is Windows 10 or greater with a minimum build number.
    /// </summary>
    /// <param name="minBuild">The minimum build number required.</param>
    /// <returns>True if running on Windows 10+ with at least the specified build.</returns>
    private static bool IsWindows10OrGreater(int minBuild)
    {
        return Environment.OSVersion.Version.Major >= 10
            && Environment.OSVersion.Version.Build >= minBuild;
    }
}
