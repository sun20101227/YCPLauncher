using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace YCPLauncher.Helpers;

/// <summary>
/// Provides enterprise-grade interoperability with the Desktop Window Manager (DWM)
/// to enable Windows 11 native features such as Mica material and rounded corners.
/// </summary>
public static class DwmHelper
{
    [DllImport("dwmapi.dll", PreserveSig = false)]
    private static extern void DwmSetWindowAttribute(IntPtr hwnd, DWMWINDOWATTRIBUTE attribute, ref int pvAttribute, uint cbAttribute);

    [Flags]
    private enum DWMWINDOWATTRIBUTE : uint
    {
        DWMWA_USE_IMMERSIVE_DARK_MODE = 20,
        DWMWA_WINDOW_CORNER_PREFERENCE = 33,
        DWMWA_SYSTEMBACKDROP_TYPE = 38
    }

    private enum DWM_WINDOW_CORNER_PREFERENCE
    {
        DWMWCP_DEFAULT = 0,
        DWMWCP_DONOTROUND = 1,
        DWMWCP_ROUND = 2,
        DWMWCP_ROUNDSMALL = 3
    }

    private enum DWM_SYSTEMBACKDROP_TYPE
    {
        DWMSBT_AUTO = 0,
        DWMSBT_NONE = 1,
        DWMSBT_MAINWINDOW = 2, // Mica
        DWMSBT_TRANSIENTWINDOW = 3, // Acrylic
        DWMSBT_TABBEDWINDOW = 4 // Mica Alt
    }

    /// <summary>
    /// Applies Windows 11 native immersive dark mode, native rounded corners, and Mica backdrop to the specified window.
    /// </summary>
    /// <param name="window">The WPF window to apply the effects to.</param>
    public static void ApplyNativeWindows11Styles(Window window)
    {
        var hwnd = new WindowInteropHelper(window).Handle;
        if (hwnd == IntPtr.Zero)
            throw new InvalidOperationException("Window handle is not initialized. Call this method in or after SourceInitialized event.");

        // 1. Apply Immersive Dark Mode for native titlebar and context menus
        int isDark = 1;
        try
        {
            DwmSetWindowAttribute(hwnd, DWMWINDOWATTRIBUTE.DWMWA_USE_IMMERSIVE_DARK_MODE, ref isDark, sizeof(int));
        }
        catch { /* OS might not support it */ }

        // 2. Enforce Native Windows 11 Rounded Corners
        int cornerPreference = (int)DWM_WINDOW_CORNER_PREFERENCE.DWMWCP_ROUND;
        try
        {
            DwmSetWindowAttribute(hwnd, DWMWINDOWATTRIBUTE.DWMWA_WINDOW_CORNER_PREFERENCE, ref cornerPreference, sizeof(int));
        }
        catch { /* OS might not support it */ }

        // 3. Apply Native Mica Backdrop
        int backdropType = (int)DWM_SYSTEMBACKDROP_TYPE.DWMSBT_MAINWINDOW;
        try
        {
            DwmSetWindowAttribute(hwnd, DWMWINDOWATTRIBUTE.DWMWA_SYSTEMBACKDROP_TYPE, ref backdropType, sizeof(int));
        }
        catch { /* OS might not support it */ }
    }
}
