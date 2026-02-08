using System.Windows;
using Petek.Desktop.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Petek.Desktop.Views;

public partial class ProfileWindow : Window
{
    public ProfileWindow()
    {
        InitializeComponent();
        LoadUserInfo();
    }

    private void LoadUserInfo()
    {
        try
        {
            var authService = App.Services.GetRequiredService<IAuthenticationService>();
            if (authService.CurrentUser != null)
            {
                var user = authService.CurrentUser;
                var displayName = user.DisplayName ?? user.Username;

                UserName.Text = displayName;
                UserTitle.Text = user.Title ?? "";
                UserDepartment.Text = user.Department ?? "";

                // Avatar initials
                var parts = displayName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length >= 2)
                    AvatarInitials.Text = $"{parts[0][0]}{parts[1][0]}".ToUpper();
                else if (parts.Length == 1 && parts[0].Length > 0)
                    AvatarInitials.Text = parts[0][0].ToString().ToUpper();
            }
            else
            {
                UserName.Text = Environment.UserName;
            }
        }
        catch
        {
            UserName.Text = Environment.UserName;
        }
    }

    private async void Logout_Click(object sender, RoutedEventArgs e)
    {
        var result = MessageBox.Show(
            "Çıkış yapmak istediğinize emin misiniz?",
            "Çıkış",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result == MessageBoxResult.Yes)
        {
            try
            {
                var authService = App.Services.GetRequiredService<IAuthenticationService>();
                await authService.LogoutAsync();
            }
            catch { }

            // Login ekranına geri dön
            var loginWindow = new LoginWindow();
            loginWindow.Show();

            // Tüm MainWindow'ları kapat
            foreach (Window window in Application.Current.Windows)
            {
                if (window is MainWindow || window is ProfileWindow)
                {
                    if (window != this) window.Close();
                }
            }

            this.Close();
        }
    }
}
