using System.Windows;
using Petek.Desktop.Services;
using Petek.Shared.DTOs;
using Petek.Shared.Enums;
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

                // Mevcut durumu sec
                switch (user.Status)
                {
                    case UserStatus.Available: StatusAvailable.IsChecked = true; break;
                    case UserStatus.Busy:
                    case UserStatus.DoNotDisturb: StatusBusy.IsChecked = true; break;
                    case UserStatus.Away:
                    case UserStatus.BeRightBack: StatusAway.IsChecked = true; break;
                    default: StatusOffline.IsChecked = true; break;
                }

                StatusMessage.Text = user.StatusMessage ?? "";
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

    private async void Save_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var apiClient = App.Services.GetRequiredService<IApiClient>();

            // Durum ve durum mesajini guncelle
            var status = UserStatus.Available;
            if (StatusBusy.IsChecked == true) status = UserStatus.Busy;
            else if (StatusAway.IsChecked == true) status = UserStatus.Away;
            else if (StatusOffline.IsChecked == true) status = UserStatus.Invisible;

            await apiClient.PutAsync<ApiResponse<bool>>("api/users/me/status",
                new UpdateUserStatusDto
                {
                    Status = status,
                    StatusMessage = StatusMessage.Text?.Trim() ?? ""
                });

            // Yerel kullanici bilgisini guncelle
            var authService = App.Services.GetRequiredService<IAuthenticationService>();
            if (authService.CurrentUser != null)
            {
                authService.CurrentUser.Status = status;
                authService.CurrentUser.StatusMessage = StatusMessage.Text?.Trim();
            }

            MessageBox.Show("Profil kaydedildi.", "Basarili", MessageBoxButton.OK, MessageBoxImage.Information);
            DialogResult = true;
            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Kaydetme hatasi: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
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
