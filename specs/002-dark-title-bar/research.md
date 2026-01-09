# Research: Dark Title Bar Implementation

**Feature**: 002-dark-title-bar
**Date**: 2026-01-09
**Status**: Complete

## Research Summary

This document consolidates research findings for enabling dark title bars in the Tomato WPF application.

---

## 1. Windows DWM API for Dark Title Bars

### Decision
Use `DwmSetWindowAttribute` with `DWMWA_USE_IMMERSIVE_DARK_MODE` attribute to enable dark title bars.

### Rationale
- Native Windows API approach (no third-party dependencies)
- Used by major applications (VS Code, Windows Terminal, etc.)
- Maintains native window chrome appearance
- Zero runtime dependencies beyond Windows itself

### Alternatives Considered
| Alternative | Rejected Because |
|-------------|------------------|
| Custom-drawn title bar | Loses native window behavior (snap, maximize, accessibility); high maintenance; violates Principle II (Simplicity) |
| WindowChrome class with dark brushes | Only styles non-client area border, not actual title bar; Windows still controls title bar color |
| Third-party libraries (e.g., ModernWpf, MahApps.Metro) | Adds unnecessary dependencies for single feature; potential compatibility issues |

---

## 2. API Details

### Function Signature
```csharp
[DllImport("dwmapi.dll")]
private static extern int DwmSetWindowAttribute(
    IntPtr hwnd,      // Window handle
    int attr,         // Attribute ID (19 or 20)
    ref int attrValue,// 1 = dark, 0 = light
    int attrSize      // sizeof(int) = 4
);
```

### Attribute Constants
| Constant | Value | Windows Version |
|----------|-------|-----------------|
| `DWMWA_USE_IMMERSIVE_DARK_MODE_BEFORE_20H1` | 19 | Windows 10 1809-1909 (Build 17763-18362) |
| `DWMWA_USE_IMMERSIVE_DARK_MODE` | 20 | Windows 10 2004+ (Build 18985+), Windows 11 |

### Version Detection
```csharp
private static bool IsWindows10OrGreater(int minBuild)
{
    return Environment.OSVersion.Version.Major >= 10
        && Environment.OSVersion.Version.Build >= minBuild;
}
```

---

## 3. Implementation Timing

### Decision
Call `DwmSetWindowAttribute` in `OnSourceInitialized` override.

### Rationale
- Window handle (HWND) is not available until after `InitializeComponent()`
- `SourceInitialized` event fires when handle is created but before window is shown
- Ensures dark title bar is applied before user sees the window

### Alternative Considered
| Alternative | Rejected Because |
|-------------|------------------|
| Constructor after `InitializeComponent()` | HWND not yet available |
| `Loaded` event | Window briefly visible with light title bar before styling applied |

---

## 4. Version Compatibility Matrix

| Windows Version | Build | Dark Title Bar Support |
|-----------------|-------|----------------------|
| Windows 10 1809 | 17763 | ✅ Use attribute 19 |
| Windows 10 1903 | 18362 | ✅ Use attribute 19 |
| Windows 10 1909 | 18363 | ✅ Use attribute 19 |
| Windows 10 2004 | 19041 | ✅ Use attribute 20 |
| Windows 10 20H2+ | 19042+ | ✅ Use attribute 20 |
| Windows 11 | 22000+ | ✅ Use attribute 20 |
| Windows 10 < 1809 | < 17763 | ❌ Not supported - graceful fallback |
| Windows 8.1 and earlier | N/A | ❌ Not supported - graceful fallback |

---

## 5. Fallback Strategy

### Decision
Silently fall back to default (light) title bar on unsupported systems.

### Rationale
- Application remains fully functional
- No error dialogs or log spam for expected scenario
- Aligns with Principle V (Progressive Enhancement)

### Implementation
```csharp
if (!IsWindows10OrGreater(17763))
{
    // Silently skip - dark title bar not supported
    return;
}
```

---

## 6. Code Organization

### Decision
Create `Helpers/DwmHelper.cs` static class for P/Invoke declarations and helper methods.

### Rationale
- Separates Windows interop code from view logic
- Reusable if additional windows need dark title bars
- Follows existing project structure conventions (Services/, Converters/, etc.)
- Easy to locate and maintain

### Alternative Considered
| Alternative | Rejected Because |
|-------------|------------------|
| Inline P/Invoke in MainWindow.xaml.cs | Clutters view code with platform interop details |
| NativeMethods class in root | Helpers folder more descriptive of purpose |

---

## 7. Testing Strategy

### Decision
Manual visual verification only; no automated tests.

### Rationale
- P/Invoke calls to Windows DWM cannot be meaningfully unit tested
- Mocking HWND and DWM behavior adds complexity without value
- Visual outcome is binary (dark or light) and immediately obvious
- Integration/UI automation testing would require significant infrastructure for trivial benefit

### Verification Checklist
- [ ] Dark title bar appears on Windows 11
- [ ] Dark title bar appears on Windows 10 2004+
- [ ] Dark title bar appears on Windows 10 1809-1909
- [ ] Light title bar (fallback) on older Windows without error
- [ ] Title bar text remains readable
- [ ] Window controls (min/max/close) remain visible
- [ ] Works in both focused and unfocused states
