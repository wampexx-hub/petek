using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using Petek.Desktop.Services;
using Petek.Shared.Enums;
using Microsoft.Extensions.DependencyInjection;

namespace Petek.Desktop.Views;

public partial class MainWindow : Window
{
    private Page? _currentListPage;
    private UserStatus _currentStatus = UserStatus.Available;

    public MainWindow()
    {
        InitializeComponent();
        Loaded += MainWindow_Loaded;
    }

    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        NavigateToChat();
        UpdateProfileInitial();
        LoadCurrentStatus();
        SubscribeToNotifications();
        SetupDesktopNotifications();
    }

    #region Status Change

    private void LoadCurrentStatus()
    {
        try
        {
            var authService = App.Services.GetRequiredService<IAuthenticationService>();
            if (authService.CurrentUser != null)
            {
                _currentStatus = authService.CurrentUser.Status;
                UpdateStatusUI(_currentStatus);
            }
        }
        catch { }
    }

    private void StatusButton_Click(object sender, RoutedEventArgs e)
    {
        var button = sender as System.Windows.Controls.Button;
        if (button != null)
        {
            var parent = button.Parent as Border;
            if (parent?.ContextMenu != null)
            {
                parent.ContextMenu.PlacementTarget = button;
                parent.ContextMenu.Placement = System.Windows.Controls.Primitives.PlacementMode.Right;
                parent.ContextMenu.IsOpen = true;
            }
        }
    }

    private async void StatusAvailable_Click(object sender, RoutedEventArgs e) => await ChangeStatusAsync(UserStatus.Available);
    private async void StatusBusy_Click(object sender, RoutedEventArgs e) => await ChangeStatusAsync(UserStatus.Busy);
    private async void StatusAway_Click(object sender, RoutedEventArgs e) => await ChangeStatusAsync(UserStatus.Away);
    private async void StatusInvisible_Click(object sender, RoutedEventArgs e) => await ChangeStatusAsync(UserStatus.Invisible);

    private async Task ChangeStatusAsync(UserStatus newStatus)
    {
        try
        {
            var signalR = App.Services.GetRequiredService<ISignalRService>();
            if (signalR.IsConnected)
            {
                await signalR.UpdateStatusAsync(newStatus);
            }

            var apiClient = App.Services.GetRequiredService<IApiClient>();
            await apiClient.PutAsync<object>("api/users/me/status", new { Status = newStatus });

            _currentStatus = newStatus;
            UpdateStatusUI(newStatus);

            ShowToast("Durum Güncellendi", GetStatusText(newStatus) + " olarak değiştirildi", ToastType.Success);
        }
        catch (Exception ex)
        {
            ShowToast("Hata", $"Durum güncellenemedi: {ex.Message}", ToastType.Error);
        }
    }

    private void UpdateStatusUI(UserStatus status)
    {
        var brushKey = status switch
        {
            UserStatus.Available => "StatusAvailableBrush",
            UserStatus.Busy => "StatusBusyBrush",
            UserStatus.Away => "StatusAwayBrush",
            _ => "StatusOfflineBrush"
        };

        StatusIndicatorDot.SetResourceReference(System.Windows.Shapes.Ellipse.FillProperty, brushKey);
    }

    private static string GetStatusText(UserStatus status) => status switch
    {
        UserStatus.Available => "Uygun",
        UserStatus.Busy => "Meşgul",
        UserStatus.Away => "Dışarıda",
        UserStatus.Invisible => "Görünmez",
        UserStatus.Offline => "Çevrimdışı",
        _ => "Bilinmiyor"
    };

    #endregion

    #region Notifications

    private void SubscribeToNotifications()
    {
        try
        {
            var signalR = App.Services.GetRequiredService<ISignalRService>();
            signalR.NotificationReceived += (title, message) =>
            {
                Dispatcher.Invoke(() =>
                {
                    ShowToast(title, message, ToastType.Info);
                });

                // Masaustu bildirimi de goster
                try
                {
                    var notificationService = App.Services.GetRequiredService<INotificationService>();
                    notificationService.ShowNotification(title, message);
                }
                catch { }
            };

            // Yeni mesaj geldiginde masaustu bildirimi goster
            signalR.MessageReceived += (messageDto) =>
            {
                try
                {
                    var authService = App.Services.GetRequiredService<IAuthenticationService>();
                    // Kendi mesajlarimiz icin bildirim gosterme
                    if (authService.CurrentUser != null && messageDto.SenderId == authService.CurrentUser.Id)
                        return;

                    var notificationService = App.Services.GetRequiredService<INotificationService>();
                    var senderName = !string.IsNullOrEmpty(messageDto.SenderName) ? messageDto.SenderName : "Bilinmeyen";
                    notificationService.ShowMessageNotification(
                        senderName,
                        messageDto.Content ?? "",
                        messageDto.Type,
                        messageDto.ConversationId);
                }
                catch { }
            };
        }
        catch { }
    }

    private void SetupDesktopNotifications()
    {
        try
        {
            var notificationService = App.Services.GetRequiredService<INotificationService>();
            notificationService.SetupSystemTray(this);
        }
        catch { }
    }

    #endregion

    #region Toast Notifications

    private enum ToastType { Info, Success, Error, Warning }

    private void ShowToast(string title, string message, ToastType type = ToastType.Info)
    {
        var toast = CreateToastBorder(title, message, type);

        ToastContainer.Children.Insert(0, toast);

        // Slide-in animasyonu
        var fadeIn = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(300));
        var slideIn = new ThicknessAnimation(
            new Thickness(360, 0, 0, 8),
            new Thickness(0, 0, 0, 8),
            TimeSpan.FromMilliseconds(300))
        { EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut } };

        toast.BeginAnimation(OpacityProperty, fadeIn);
        toast.BeginAnimation(MarginProperty, slideIn);

        // 5 saniye sonra otomatik kapat
        var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(5) };
        timer.Tick += (s, e) =>
        {
            timer.Stop();
            RemoveToast(toast);
        };
        timer.Start();

        // Maksimum 3 bildirim
        while (ToastContainer.Children.Count > 3)
        {
            ToastContainer.Children.RemoveAt(ToastContainer.Children.Count - 1);
        }
    }

    private void RemoveToast(Border toast)
    {
        if (!ToastContainer.Children.Contains(toast)) return;

        var fadeOut = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(300));
        fadeOut.Completed += (s, e) =>
        {
            if (ToastContainer.Children.Contains(toast))
                ToastContainer.Children.Remove(toast);
        };
        toast.BeginAnimation(OpacityProperty, fadeOut);
    }

    private Border CreateToastBorder(string title, string message, ToastType type)
    {
        Color accentColor = type switch
        {
            ToastType.Success => (Color)System.Windows.Media.ColorConverter.ConvertFromString("#6CCB5F"),
            ToastType.Error => (Color)System.Windows.Media.ColorConverter.ConvertFromString("#C42B1C"),
            ToastType.Warning => (Color)System.Windows.Media.ColorConverter.ConvertFromString("#F7B93E"),
            _ => (Color)System.Windows.Media.ColorConverter.ConvertFromString("#F59E0B")
        };

        string iconChar = type switch
        {
            ToastType.Success => "\uE73E",
            ToastType.Error => "\uE783",
            ToastType.Warning => "\uE7BA",
            _ => "\uEA8F"
        };

        var toast = new Border
        {
            CornerRadius = new CornerRadius(10),
            Padding = new Thickness(14, 12, 14, 12),
            Margin = new Thickness(0, 0, 0, 8),
            Cursor = System.Windows.Input.Cursors.Hand,
            BorderThickness = new Thickness(1),
            Effect = new System.Windows.Media.Effects.DropShadowEffect
            {
                BlurRadius = 16,
                Opacity = 0.2,
                ShadowDepth = 4,
                Color = Colors.Black
            }
        };
        toast.SetResourceReference(Border.BackgroundProperty, "CardBackgroundBrush");
        toast.SetResourceReference(Border.BorderBrushProperty, "BorderBrush");

        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(4) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(32) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(24) });

        // Sol accent çizgi
        var accentBar = new Border
        {
            Background = new SolidColorBrush(accentColor),
            CornerRadius = new CornerRadius(2),
            Width = 3,
            VerticalAlignment = VerticalAlignment.Stretch,
            Margin = new Thickness(0, 0, 8, 0)
        };
        Grid.SetColumn(accentBar, 0);
        grid.Children.Add(accentBar);

        // Ikon
        var icon = new TextBlock
        {
            Text = iconChar,
            FontFamily = new System.Windows.Media.FontFamily("Segoe MDL2 Assets"),
            FontSize = 18,
            Foreground = new SolidColorBrush(accentColor),
            VerticalAlignment = VerticalAlignment.Top,
            Margin = new Thickness(0, 2, 8, 0)
        };
        Grid.SetColumn(icon, 1);
        grid.Children.Add(icon);

        // İçerik
        var content = new StackPanel();
        var titleBlock = new TextBlock
        {
            Text = title,
            FontWeight = FontWeights.SemiBold,
            FontSize = 13,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 0, 0, 2)
        };
        titleBlock.SetResourceReference(TextBlock.ForegroundProperty, "TextPrimaryBrush");
        content.Children.Add(titleBlock);

        var messageBlock = new TextBlock
        {
            Text = message,
            FontSize = 12,
            TextWrapping = TextWrapping.Wrap,
            MaxHeight = 60
        };
        messageBlock.SetResourceReference(TextBlock.ForegroundProperty, "TextSecondaryBrush");
        content.Children.Add(messageBlock);

        Grid.SetColumn(content, 2);
        grid.Children.Add(content);

        // Kapat butonu
        var closeBtn = new TextBlock
        {
            Text = "✕",
            FontSize = 11,
            VerticalAlignment = VerticalAlignment.Top,
            HorizontalAlignment = HorizontalAlignment.Right,
            Cursor = System.Windows.Input.Cursors.Hand
        };
        closeBtn.SetResourceReference(TextBlock.ForegroundProperty, "TextTertiaryBrush");
        closeBtn.MouseLeftButtonDown += (s, e) =>
        {
            e.Handled = true;
            RemoveToast(toast);
        };
        Grid.SetColumn(closeBtn, 3);
        grid.Children.Add(closeBtn);

        toast.Child = grid;

        toast.MouseLeftButtonDown += (s, e) =>
        {
            RemoveToast(toast);
        };

        return toast;
    }

    #endregion

    #region Profile

    private void UpdateProfileInitial()
    {
        try
        {
            var authService = App.Services.GetRequiredService<IAuthenticationService>();
            if (authService.CurrentUser != null)
            {
                var name = authService.CurrentUser.DisplayName ?? authService.CurrentUser.Username;
                ProfileInitial.Text = !string.IsNullOrEmpty(name) ? name[0].ToString().ToUpper() : "?";
            }
        }
        catch { }
    }

    private void ProfileButton_Click(object sender, RoutedEventArgs e)
    {
        var profileWindow = new ProfileWindow();
        profileWindow.Owner = this;
        profileWindow.ShowDialog();

        UpdateProfileInitial();
        LoadCurrentStatus();
    }

    #endregion

    #region Navigation

    private void NavItem_Checked(object sender, RoutedEventArgs e)
    {
        if (ListFrame == null || ContentFrame == null) return;

        if (sender is RadioButton radioButton)
        {
            switch (radioButton.Name)
            {
                case "NavChat": NavigateToChat(); break;
                case "NavContacts": NavigateToContacts(); break;
                case "NavFiles": NavigateToFiles(); break;
                case "NavSettings": NavigateToSettings(); break;
            }
        }
    }

    private void NavigateToChat()
    {
        if (ListFrame == null || ContentFrame == null) return;
        var page = new ConversationListView();
        _currentListPage = page;
        ListFrame.Navigate(page);
        ContentFrame.Navigate(new ChatView());
        if (SearchBox != null) SearchBox.Text = "";
    }

    private void NavigateToContacts()
    {
        if (ListFrame == null || ContentFrame == null) return;
        var page = new ContactListView();
        _currentListPage = page;
        ListFrame.Navigate(page);
        ContentFrame.Navigate(new ContactDetailView());
        if (SearchBox != null) SearchBox.Text = "";
    }

    private void NavigateToFiles()
    {
        if (ListFrame == null || ContentFrame == null) return;
        var page = new FileListView();
        _currentListPage = page;
        ListFrame.Navigate(page);
        ContentFrame.Navigate(new FileDetailView());
        if (SearchBox != null) SearchBox.Text = "";
    }

    private void NavigateToSettings()
    {
        if (ListFrame == null || ContentFrame == null) return;
        _currentListPage = null;
        ListFrame.Content = null;
        ContentFrame.Navigate(new SettingsView());
    }

    private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        var query = SearchBox.Text?.Trim() ?? "";

        if (_currentListPage is ContactListView contactList)
        {
            contactList.FilterContacts(query);
        }
        else if (_currentListPage is ConversationListView convList)
        {
            convList.FilterConversations(query);
        }
    }

    #endregion
}
