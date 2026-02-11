using System.Windows;
using System.Windows.Input;
using Petek.Desktop.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace Petek.Desktop.Views;

public partial class LoginWindow : Window
{
    private static readonly string SettingsFilePath = System.IO.Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "PetekMessenger", "settings.json");

    public LoginWindow()
    {
        InitializeComponent();
        DataContext = App.Services.GetRequiredService<LoginViewModel>();

        // Listen for login success
        if (DataContext is LoginViewModel vm)
        {
            vm.OnLoginSuccess += () =>
            {
                Dispatcher.Invoke(() =>
                {
                    var mainWindow = new MainWindow();
                    mainWindow.Show();
                    this.Close();
                });
            };
        }

        LoadServerUrl();
    }

    private void LoadServerUrl()
    {
        try
        {
            var settings = LoadSettings();
            if (settings.TryGetValue("ServerUrl", out var url) && !string.IsNullOrWhiteSpace(url))
                ServerUrlBox.Text = url;
            else
                ServerUrlBox.Text = "http://localhost:5000";
        }
        catch
        {
            ServerUrlBox.Text = "http://localhost:5000";
        }
    }

    private void ServerSettingsToggle_Click(object sender, RoutedEventArgs e)
    {
        if (ServerSettingsPanel.Visibility == Visibility.Collapsed)
        {
            ServerSettingsPanel.Visibility = Visibility.Visible;
            ServerSettingsArrow.Text = "  \u25B2";
        }
        else
        {
            ServerSettingsPanel.Visibility = Visibility.Collapsed;
            ServerSettingsArrow.Text = "  \u25BC";
        }
    }

    private void ApplyServerUrl_Click(object sender, RoutedEventArgs e)
    {
        var url = ServerUrlBox.Text?.Trim() ?? "";

        if (string.IsNullOrWhiteSpace(url))
        {
            ServerUrlStatus.Text = "Sunucu adresi bos olamaz.";
            ServerUrlStatus.SetResourceReference(System.Windows.Controls.TextBlock.ForegroundProperty, "StatusBusyBrush");
            return;
        }

        if (!url.StartsWith("http://") && !url.StartsWith("https://"))
        {
            ServerUrlStatus.Text = "Adres http:// veya https:// ile baslamalidir.";
            ServerUrlStatus.SetResourceReference(System.Windows.Controls.TextBlock.ForegroundProperty, "StatusBusyBrush");
            return;
        }

        // Sondaki / temizle
        url = url.TrimEnd('/');

        try
        {
            // Mevcut ayarlarla karsilastir
            var settings = LoadSettings();
            var currentUrl = settings.TryGetValue("ServerUrl", out var existing) ? existing : "http://localhost:5000";

            if (url == currentUrl)
            {
                ServerUrlStatus.Text = "Zaten bu sunucuya bagli.";
                ServerUrlStatus.SetResourceReference(System.Windows.Controls.TextBlock.ForegroundProperty, "TextTertiaryBrush");
                return;
            }

            // Kaydet
            SaveSetting("ServerUrl", url);

            ServerUrlStatus.Text = "Kaydedildi. Uygulama yeniden baslatiliyor...";
            ServerUrlStatus.SetResourceReference(System.Windows.Controls.TextBlock.ForegroundProperty, "StatusAvailableBrush");

            // Uygulamayi yeniden baslat
            var exePath = Environment.ProcessPath ?? System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName;
            if (exePath != null)
            {
                System.Diagnostics.Process.Start(exePath);
            }
            Application.Current.Shutdown();
        }
        catch (Exception ex)
        {
            ServerUrlStatus.Text = $"Hata: {ex.Message}";
            ServerUrlStatus.SetResourceReference(System.Windows.Controls.TextBlock.ForegroundProperty, "StatusBusyBrush");
        }
    }

    private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
    {
        if (DataContext is LoginViewModel vm)
        {
            vm.Password = PasswordBox.Password;
        }
    }

    private void PasswordBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && DataContext is LoginViewModel vm)
        {
            if (vm.LoginWithCredentialsCommand.CanExecute(null))
            {
                vm.LoginWithCredentialsCommand.Execute(null);
            }
        }
    }

    private void Exit_Click(object sender, RoutedEventArgs e)
    {
        Application.Current.Shutdown();
    }

    protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonDown(e);
        // Allow dragging the borderless window
        this.DragMove();
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
