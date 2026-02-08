using System.Windows;
using System.Windows.Controls;
using Petek.Desktop.Services;

namespace Petek.Desktop.Views;

public partial class SettingsView : Page
{
    private bool _isInitializing = true;
    private static readonly string SettingsFilePath = System.IO.Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "PetekMessenger", "settings.json");

    public SettingsView()
    {
        InitializeComponent();
        Loaded += SettingsView_Loaded;
    }

    private void SettingsView_Loaded(object sender, RoutedEventArgs e)
    {
        // Sync ComboBox with the actual current theme
        var currentTheme = ThemeService.Instance.CurrentTheme;
        var index = currentTheme switch
        {
            ThemeMode.Light => 1,
            ThemeMode.Dark => 2,
            _ => 0 // System
        };

        ThemeComboBox.SelectedIndex = index;

        // Load saved settings
        var settings = LoadSettings();

        if (settings.TryGetValue("DownloadPath", out var dlPath))
            DownloadPathBox.Text = dlPath;

        if (string.IsNullOrEmpty(DownloadPathBox.Text))
        {
            DownloadPathBox.Text = System.IO.Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
        }

        if (settings.TryGetValue("ServerUrl", out var serverUrl) && !string.IsNullOrWhiteSpace(serverUrl))
            ServerUrlBox.Text = serverUrl;
        else
            ServerUrlBox.Text = "http://localhost:5000";

        ServerUrlStatus.Text = "Değişiklik uygulanması için uygulamayı yeniden başlatmanız gerekir.";

        _isInitializing = false;
    }

    private void ThemeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isInitializing) return;

        if (ThemeComboBox.SelectedItem is ComboBoxItem item)
        {
            var theme = item.Content.ToString() switch
            {
                "Açık" => ThemeMode.Light,
                "Koyu" => ThemeMode.Dark,
                _ => ThemeMode.System
            };

            ThemeService.Instance.SetTheme(theme);
        }
    }

    private void BrowseDownloadFolder_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new System.Windows.Forms.FolderBrowserDialog
        {
            Description = "Dosya indirme konumunu seçin",
            UseDescriptionForTitle = true,
            ShowNewFolderButton = true
        };

        if (!string.IsNullOrEmpty(DownloadPathBox.Text))
        {
            dialog.SelectedPath = DownloadPathBox.Text;
        }

        if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
        {
            DownloadPathBox.Text = dialog.SelectedPath;
            SaveSetting("DownloadPath", dialog.SelectedPath);
        }
    }

    private void SaveServerUrl_Click(object sender, RoutedEventArgs e)
    {
        var url = ServerUrlBox.Text?.Trim() ?? "";

        if (string.IsNullOrWhiteSpace(url))
        {
            ServerUrlStatus.Text = "Sunucu adresi boş olamaz.";
            ServerUrlStatus.Foreground = (System.Windows.Media.Brush)FindResource("StatusBusyBrush");
            return;
        }

        // Basic URL validation
        if (!url.StartsWith("http://") && !url.StartsWith("https://"))
        {
            ServerUrlStatus.Text = "Adres http:// veya https:// ile başlamalıdır.";
            ServerUrlStatus.Foreground = (System.Windows.Media.Brush)FindResource("StatusBusyBrush");
            return;
        }

        SaveSetting("ServerUrl", url);
        ServerUrlStatus.Text = "Kaydedildi. Değişikliğin uygulanması için uygulamayı yeniden başlatın.";
        ServerUrlStatus.Foreground = (System.Windows.Media.Brush)FindResource("StatusAvailableBrush");
    }

    private static Dictionary<string, string> LoadSettings()
    {
        try
        {
            if (System.IO.File.Exists(SettingsFilePath))
            {
                var json = System.IO.File.ReadAllText(SettingsFilePath);
                return System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(json)
                       ?? new Dictionary<string, string>();
            }
        }
        catch { }

        return new Dictionary<string, string>();
    }

    private static void SaveSetting(string key, string value)
    {
        try
        {
            var dir = System.IO.Path.GetDirectoryName(SettingsFilePath)!;
            if (!System.IO.Directory.Exists(dir))
                System.IO.Directory.CreateDirectory(dir);

            var settings = LoadSettings();
            settings[key] = value;

            var json = System.Text.Json.JsonSerializer.Serialize(settings,
                new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
            System.IO.File.WriteAllText(SettingsFilePath, json);
        }
        catch { }
    }
}
