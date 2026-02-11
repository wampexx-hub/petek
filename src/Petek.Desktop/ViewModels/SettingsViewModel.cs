using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Petek.Desktop.Services;

namespace Petek.Desktop.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private readonly IThemeService _themeService;
    private readonly INotificationService _notificationService;

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

    [ObservableProperty]
    private bool _minimizeToTray = true;

    public SettingsViewModel(IThemeService themeService, INotificationService notificationService)
    {
        _themeService = themeService;
        _notificationService = notificationService;
        _selectedTheme = _themeService.CurrentTheme;

        // Bildirim servisinden mevcut ayarlari yukle
        _enableNotifications = _notificationService.IsEnabled;
        _enableSounds = _notificationService.SoundEnabled;
        _minimizeToTray = _notificationService.ShowInTaskbar;
    }

    partial void OnSelectedThemeChanged(ThemeMode value)
    {
        _themeService.SetTheme(value);
    }

    partial void OnEnableNotificationsChanged(bool value)
    {
        _notificationService.IsEnabled = value;
    }

    partial void OnEnableSoundsChanged(bool value)
    {
        _notificationService.SoundEnabled = value;
    }

    partial void OnMinimizeToTrayChanged(bool value)
    {
        _notificationService.ShowInTaskbar = value;
    }

    [RelayCommand]
    private void SaveSettings()
    {
        // Ayarlar zaten partial OnChanged metodlariyla aninda uygulanir
        // Ek olarak local dosyaya kaydet
        try
        {
            var settingsPath = System.IO.Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "PetekMessenger", "notification-settings.json");

            var dir = System.IO.Path.GetDirectoryName(settingsPath);
            if (dir != null && !System.IO.Directory.Exists(dir))
                System.IO.Directory.CreateDirectory(dir);

            var settings = new Dictionary<string, object>
            {
                ["EnableNotifications"] = EnableNotifications,
                ["EnableSounds"] = EnableSounds,
                ["MinimizeToTray"] = MinimizeToTray,
                ["AutoAway"] = AutoAway,
                ["AwayTimeoutMinutes"] = AwayTimeoutMinutes
            };

            var json = System.Text.Json.JsonSerializer.Serialize(settings,
                new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
            System.IO.File.WriteAllText(settingsPath, json);
        }
        catch { }
    }

    [RelayCommand]
    private void ResetSettings()
    {
        SelectedTheme = ThemeMode.System;
        EnableNotifications = true;
        EnableSounds = true;
        MinimizeToTray = true;
        AutoAway = true;
        AwayTimeoutMinutes = 5;
        EnableTransparency = true;
    }
}
