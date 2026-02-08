using Microsoft.Win32;
using System.Windows;
using System.Windows.Media;

namespace Petek.Desktop.Services;

public enum ThemeMode
{
    System,
    Light,
    Dark
}

public interface IThemeService
{
    ThemeMode CurrentTheme { get; }
    void SetTheme(ThemeMode theme);
    void ApplySystemTheme();
}

public class ThemeService : IThemeService
{
    private static ThemeService? _instance;
    public static ThemeService Instance => _instance ??= new ThemeService();

    public ThemeMode CurrentTheme { get; private set; } = ThemeMode.System;

    private ThemeService()
    {
        // Listen for system theme changes
        SystemEvents.UserPreferenceChanged += (s, e) =>
        {
            if (e.Category == UserPreferenceCategory.General && CurrentTheme == ThemeMode.System)
            {
                ApplySystemTheme();
            }
        };
    }

    public void SetTheme(ThemeMode theme)
    {
        CurrentTheme = theme;

        if (theme == ThemeMode.System)
        {
            ApplySystemTheme();
        }
        else
        {
            ApplyTheme(theme == ThemeMode.Dark);
        }
    }

    public void ApplySystemTheme()
    {
        var isDark = IsSystemDarkTheme();
        ApplyTheme(isDark);
    }

    private void ApplyTheme(bool isDark)
    {
        var app = Application.Current;
        if (app?.Resources == null) return;

        if (isDark)
        {
            // Dark Theme
            app.Resources["BackgroundBrush"] = new SolidColorBrush(Color.FromRgb(0x20, 0x20, 0x20));
            app.Resources["SurfaceBrush"] = new SolidColorBrush(Color.FromRgb(0x2D, 0x2D, 0x2D));
            app.Resources["SurfaceVariantBrush"] = new SolidColorBrush(Color.FromRgb(0x38, 0x38, 0x38));
            app.Resources["CardBackgroundBrush"] = new SolidColorBrush(Color.FromRgb(0x2D, 0x2D, 0x2D));
            app.Resources["TextPrimaryBrush"] = new SolidColorBrush(Colors.White);
            app.Resources["TextSecondaryBrush"] = new SolidColorBrush(Color.FromRgb(0xB3, 0xB3, 0xB3));
            app.Resources["TextTertiaryBrush"] = new SolidColorBrush(Color.FromRgb(0x76, 0x76, 0x76));
            app.Resources["BorderBrush"] = new SolidColorBrush(Color.FromRgb(0x40, 0x40, 0x40));
            app.Resources["DividerBrush"] = new SolidColorBrush(Color.FromRgb(0x3D, 0x3D, 0x3D));
            app.Resources["SidebarBackgroundBrush"] = new SolidColorBrush(Color.FromRgb(0x1F, 0x1F, 0x1F));
        }
        else
        {
            // Light Theme
            app.Resources["BackgroundBrush"] = new SolidColorBrush(Color.FromRgb(0xF3, 0xF3, 0xF3));
            app.Resources["SurfaceBrush"] = new SolidColorBrush(Colors.White);
            app.Resources["SurfaceVariantBrush"] = new SolidColorBrush(Color.FromRgb(0xF9, 0xF9, 0xF9));
            app.Resources["CardBackgroundBrush"] = new SolidColorBrush(Colors.White);
            app.Resources["TextPrimaryBrush"] = new SolidColorBrush(Color.FromRgb(0x1A, 0x1A, 0x1A));
            app.Resources["TextSecondaryBrush"] = new SolidColorBrush(Color.FromRgb(0x5C, 0x5C, 0x5C));
            app.Resources["TextTertiaryBrush"] = new SolidColorBrush(Color.FromRgb(0x8A, 0x8A, 0x8A));
            app.Resources["BorderBrush"] = new SolidColorBrush(Color.FromRgb(0xE0, 0xE0, 0xE0));
            app.Resources["DividerBrush"] = new SolidColorBrush(Color.FromRgb(0xEB, 0xEB, 0xEB));
            app.Resources["SidebarBackgroundBrush"] = new SolidColorBrush(Color.FromRgb(0xF5, 0xF5, 0xF5));
        }
    }

    private static bool IsSystemDarkTheme()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(
                @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
            var value = key?.GetValue("AppsUseLightTheme");
            return value is int intValue && intValue == 0;
        }
        catch
        {
            return false;
        }
    }
}
