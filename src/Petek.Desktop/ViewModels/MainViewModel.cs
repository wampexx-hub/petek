using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Petek.Desktop.Services;
using Petek.Shared.DTOs;
using Petek.Shared.Enums;

namespace Petek.Desktop.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly IAuthenticationService _authService;
    private readonly ISignalRService _signalRService;

    [ObservableProperty]
    private UserDto? _currentUser;

    [ObservableProperty]
    private bool _isConnected;

    [ObservableProperty]
    private string _connectionStatus = "Bağlanıyor...";

    public MainViewModel(IAuthenticationService authService, ISignalRService signalRService)
    {
        _authService = authService;
        _signalRService = signalRService;

        // Subscribe to connection events
        _signalRService.UserOnline += OnUserOnline;
        _signalRService.UserOffline += OnUserOffline;
    }

    [RelayCommand]
    private async Task InitializeAsync()
    {
        // Auto-login with Windows credentials
        var result = await _authService.LoginWithWindowsAsync();

        if (result.Success)
        {
            CurrentUser = result.User;

            // Connect to SignalR
            if (!string.IsNullOrEmpty(_authService.Token))
            {
                await _signalRService.ConnectAsync(_authService.Token);
                IsConnected = _signalRService.IsConnected;
                ConnectionStatus = IsConnected ? "Bağlı" : "Bağlantı Hatası";
            }
        }
        else
        {
            ConnectionStatus = "Giriş Başarısız";
        }
    }

    [RelayCommand]
    private async Task UpdateStatusAsync(UserStatus status)
    {
        await _signalRService.UpdateStatusAsync(status);
    }

    [RelayCommand]
    private async Task LogoutAsync()
    {
        await _signalRService.DisconnectAsync();
        await _authService.LogoutAsync();
        CurrentUser = null;
        IsConnected = false;
    }

    private void OnUserOnline(Guid userId)
    {
        // Handle user online event
    }

    private void OnUserOffline(Guid userId)
    {
        // Handle user offline event
    }
}
