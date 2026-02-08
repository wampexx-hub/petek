using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Petek.Desktop.Services;

namespace Petek.Desktop.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private readonly IThemeService _themeService;

    [ObservableProperty]
    private ThemeMode _selectedTheme;

    [ObservableProperty]
    private bool _enableNotifications = true;

    [ObservableProperty]
    private bool _enableSounds = true;

    [ObservableProperty]
    private bool _autoAway = true;

    [ObservableProperty]
    private int _awayTimeoutMinutes = 5;

    [ObservableProperty]
    private bool _enableTransparency = true;

    public SettingsViewModel(IThemeService themeService)
    {
        _themeService = themeService;
        _selectedTheme = _themeService.CurrentTheme;
    }

    partial void OnSelectedThemeChanged(ThemeMode value)
    {
        _themeService.SetTheme(value);
    }

    [RelayCommand]
    private void SaveSettings()
    {
        // Save settings to local storage or user preferences
        // This would typically use a settings storage service
    }

    [RelayCommand]
    private void ResetSettings()
    {
        SelectedTheme = ThemeMode.System;
        EnableNotifications = true;
        EnableSounds = true;
        AutoAway = true;
        AwayTimeoutMinutes = 5;
        EnableTransparency = true;
    }
}
