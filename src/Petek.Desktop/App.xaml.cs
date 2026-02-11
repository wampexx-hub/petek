using Petek.Desktop.Services;
using Petek.Desktop.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;

namespace Petek.Desktop;

public partial class App : Application
{
    public static IServiceProvider Services { get; private set; } = null!;

    public App()
    {
        AppDomain.CurrentDomain.UnhandledException += (s, e) =>
        {
            MessageBox.Show($"Kritik Hata: {e.ExceptionObject}", "Petek Messenger - Hata", MessageBoxButton.OK, MessageBoxImage.Error);
        };
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        this.DispatcherUnhandledException += (s, ex) =>
        {
            MessageBox.Show($"Uygulama Hatası: {ex.Exception.Message}", "Petek Messenger - Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            ex.Handled = true;
        };

        base.OnStartup(e);

        try
        {
            var services = new ServiceCollection();
            ConfigureServices(services);
            Services = services.BuildServiceProvider();

            // Sistem temasını algıla ve uygula
            ThemeService.Instance.ApplySystemTheme();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Başlatma Hatası: {ex.Message}\n\n{ex.StackTrace}", "Petek Messenger - Hata", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void ConfigureServices(IServiceCollection services)
    {
        // HTTP Client - base address loaded from local settings
        services.AddHttpClient<IApiClient, ApiClient>(client =>
        {
            var serverUrl = LoadServerUrl();
            client.BaseAddress = new Uri(serverUrl);
        });

        // Services
        services.AddSingleton<IThemeService, ThemeService>();
        services.AddSingleton<ISignalRService, SignalRService>();
        services.AddSingleton<IAuthenticationService, AuthenticationService>();
        services.AddSingleton<IScreenshotService, ScreenshotService>();
        services.AddSingleton<INotificationService, NotificationService>();

        // ViewModels
        services.AddTransient<MainViewModel>();
        services.AddTransient<LoginViewModel>();
        services.AddTransient<ChatViewModel>();
        services.AddTransient<ContactsViewModel>();
        services.AddTransient<SettingsViewModel>();
    }

    private static string LoadServerUrl()
    {
        try
        {
            var settingsPath = System.IO.Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "PetekMessenger", "settings.json");

            if (System.IO.File.Exists(settingsPath))
            {
                var json = System.IO.File.ReadAllText(settingsPath);
                var settings = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(json);
                if (settings != null && settings.TryGetValue("ServerUrl", out var url) && !string.IsNullOrWhiteSpace(url))
                {
                    return url;
                }
            }
        }
        catch { }

        return "http://localhost:5000";
    }

    protected override void OnExit(ExitEventArgs e)
    {
        // Cleanup: Safely dispose async services before exiting
        try
        {
            var notificationService = Services.GetService<INotificationService>();
            notificationService?.Dispose();
        }
        catch { }

        try
        {
            var signalR = Services.GetService<ISignalRService>();
            if (signalR is IAsyncDisposable asyncDisposable)
            {
                asyncDisposable.DisposeAsync().AsTask().GetAwaiter().GetResult();
            }
        }
        catch { }

        if (Services is IDisposable disposable)
        {
            disposable.Dispose();
        }
        base.OnExit(e);
    }
}
