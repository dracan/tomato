# Quickstart: Dark Title Bar Implementation

**Feature**: 002-dark-title-bar
**Date**: 2026-01-09

## Overview

This guide explains how to implement dark title bar support for the Tomato WPF application using the Windows DWM API.

## Prerequisites

- .NET 8.0 SDK
- Windows 10 1809+ or Windows 11 for testing
- Visual Studio 2022 or VS Code with C# extension

## Implementation Steps

### Step 1: Create DwmHelper Class

Create a new file `src/Tomato/Helpers/DwmHelper.cs`:

```csharp
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

    private static bool IsWindows10OrGreater(int minBuild)
    {
        return Environment.OSVersion.Version.Major >= 10
            && Environment.OSVersion.Version.Build >= minBuild;
    }
}
```

### Step 2: Modify MainWindow.xaml.cs

Add the dark title bar initialization to MainWindow:

```csharp
using System.Windows.Interop;
using Tomato.Helpers;

// In MainWindow class:

protected override void OnSourceInitialized(EventArgs e)
{
    base.OnSourceInitialized(e);

    var hwnd = new WindowInteropHelper(this).Handle;
    DwmHelper.SetDarkTitleBar(hwnd, enable: true);
}
```

### Step 3: Build and Test

```powershell
cd src
dotnet build Tomato/Tomato.csproj
dotnet run --project Tomato/Tomato.csproj
```

## Verification

1. Launch the application on Windows 10 1809+ or Windows 11
2. Verify the title bar displays with dark coloring
3. Verify title text "Tomato - Pomodoro Timer" is readable (white text)
4. Verify window controls (minimize, maximize, close) are visible
5. Focus/unfocus the window to verify both states work correctly

## Troubleshooting

### Title bar still appears light
- Verify Windows build version: `[Environment]::OSVersion.Version` in PowerShell
- Ensure `OnSourceInitialized` is being called (add breakpoint)
- Check that `DwmHelper.SetDarkTitleBar` returns `true`

### Application crashes on startup
- Verify `dwmapi.dll` import signature matches exactly
- Ensure P/Invoke is called after window handle is available (not in constructor before `InitializeComponent`)

### Different behavior on different machines
- Expected: Windows version determines attribute value (19 vs 20)
- Expected: Pre-1809 Windows will show light title bar (graceful fallback)
