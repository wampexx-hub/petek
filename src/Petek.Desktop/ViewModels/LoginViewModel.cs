using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Petek.Desktop.Services;

namespace Petek.Desktop.ViewModels;

public partial class LoginViewModel : ObservableObject
{
    private readonly IAuthenticationService _authService;
    public event Action? OnLoginSuccess;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string? _errorMessage;

    [ObservableProperty]
    private string _statusMessage = "Kullanıcı adı ve şifre ile giriş yapın";

    [ObservableProperty]
    private string _username = string.Empty;

    [ObservableProperty]
    private string _password = string.Empty;

    public LoginViewModel(IAuthenticationService authService)
    {
        _authService = authService;
    }

    [RelayCommand]
    private async Task LoginAsync()
    {
        IsLoading = true;
        ErrorMessage = null;
        StatusMessage = "Windows kimlik bilgileri ile giriş yapılıyor...";

        try
        {
            var result = await _authService.LoginWithWindowsAsync();

            if (result.Success)
            {
                StatusMessage = "Giriş başarılı!";
                OnLoginSuccess?.Invoke();
            }
            else
            {
                ErrorMessage = result.ErrorMessage ?? "Giriş başarısız";
                StatusMessage = "Tekrar deneyin";
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            StatusMessage = "Bağlantı hatası";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task LoginWithCredentialsAsync()
    {
        if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Password))
        {
            ErrorMessage = "Kullanıcı adı ve şifre gereklidir";
            return;
        }

        IsLoading = true;
        ErrorMessage = null;
        StatusMessage = "Giriş yapılıyor...";

        try
        {
            var result = await _authService.LoginWithCredentialsAsync(Username, Password);

            if (result.Success)
            {
                StatusMessage = "Giriş başarılı!";
                OnLoginSuccess?.Invoke();
            }
            else
            {
                ErrorMessage = result.ErrorMessage ?? "Giriş başarısız";
                StatusMessage = "Tekrar deneyin";
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            StatusMessage = "Bağlantı hatası";
        }
        finally
        {
            IsLoading = false;
        }
    }

}
