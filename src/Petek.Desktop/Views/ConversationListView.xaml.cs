using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using Petek.Desktop.Services;
using Petek.Shared.DTOs;
using Petek.Shared.Enums;
using Microsoft.Extensions.DependencyInjection;

namespace Petek.Desktop.Views;

public partial class ConversationListView : Page
{
    public ObservableCollection<ConversationItemViewModel> Conversations { get; } = new();
    private readonly List<ConversationItemViewModel> _allConversations = new();
    private System.Threading.CancellationTokenSource? _searchCts;

    public ConversationListView()
    {
        InitializeComponent();
        DataContext = this;
        ConversationList.ItemsSource = Conversations;
        LoadConversationsAsync();
    }

    private async void LoadConversationsAsync()
    {
        try
        {
            var apiClient = App.Services.GetRequiredService<IApiClient>();
            var response = await apiClient.GetAsync<ApiResponse<List<ConversationSummaryDto>>>("api/conversations");

            if (response?.Success == true && response.Data != null)
            {
                _allConversations.Clear();
                Conversations.Clear();

                foreach (var conv in response.Data)
                {
                    var vm = new ConversationItemViewModel
                    {
                        Id = conv.Id,
                        Name = conv.Name,
                        LastMessage = conv.LastMessageContent ?? "",
                        LastMessageTime = FormatTime(conv.LastMessageAt),
                        UnreadCount = conv.UnreadCount,
                        Status = conv.OtherUserStatus ?? UserStatus.Offline,
                        IsGroup = conv.Type == ConversationType.Group
                    };
                    _allConversations.Add(vm);
                    Conversations.Add(vm);
                }
            }

            EmptyState.Visibility = _allConversations.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
            ConversationList.Visibility = _allConversations.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
        }
        catch { }
    }

    public async void FilterConversations(string query)
    {
        _searchCts?.Cancel();
        _searchCts = new System.Threading.CancellationTokenSource();
        var token = _searchCts.Token;

        if (string.IsNullOrWhiteSpace(query))
        {
            // Arama temizlendi - normal konusma listesini goster
            UserSearchPanel.Visibility = Visibility.Collapsed;
            ConversationList.Visibility = _allConversations.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
            EmptyState.Visibility = _allConversations.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

            Conversations.Clear();
            foreach (var c in _allConversations)
                Conversations.Add(c);
            return;
        }

        // Yerel konusma filtreleme
        var filtered = _allConversations
            .Where(c => c.Name.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                         c.LastMessage.Contains(query, StringComparison.OrdinalIgnoreCase))
            .ToList();

        Conversations.Clear();
        foreach (var c in filtered)
            Conversations.Add(c);

        ConversationList.Visibility = filtered.Count > 0 ? Visibility.Visible : Visibility.Collapsed;

        // 2+ karakter icin API'den kullanici ara
        if (query.Length >= 2)
        {
            try { await Task.Delay(300, token); } catch { return; }
            if (token.IsCancellationRequested) return;

            try
            {
                var apiClient = App.Services.GetRequiredService<IApiClient>();
                var response = await apiClient.GetAsync<ApiResponse<List<UserDto>>>(
                    $"api/users/search?q={Uri.EscapeDataString(query)}");

                if (token.IsCancellationRequested) return;

                if (response?.Success == true && response.Data != null && response.Data.Count > 0)
                {
                    // Mevcut kullanicinin ID'sini al
                    Guid? currentUserId = null;
                    try
                    {
                        var authService = App.Services.GetRequiredService<IAuthenticationService>();
                        currentUserId = authService.CurrentUser?.Id;
                    }
                    catch { }

                    var userResults = new ObservableCollection<UserSearchResultViewModel>();
                    foreach (var user in response.Data)
                    {
                        // Kendini gosterme
                        if (currentUserId.HasValue && user.Id == currentUserId.Value) continue;

                        userResults.Add(new UserSearchResultViewModel
                        {
                            Id = user.Id,
                            DisplayName = user.DisplayName ?? user.Username,
                            Detail = string.Join(" - ", new[] { user.Title, user.Department }
                                .Where(s => !string.IsNullOrEmpty(s))),
                            Status = user.Status
                        });
                    }

                    if (userResults.Count > 0)
                    {
                        UserSearchTitle.Text = $"Kullanicilar ({userResults.Count})";
                        UserSearchList.ItemsSource = userResults;
                        UserSearchPanel.Visibility = Visibility.Visible;
                        EmptyState.Visibility = Visibility.Collapsed;
                    }
                    else
                    {
                        UserSearchPanel.Visibility = Visibility.Collapsed;
                    }
                }
                else
                {
                    UserSearchPanel.Visibility = Visibility.Collapsed;
                }
            }
            catch
            {
                if (!token.IsCancellationRequested)
                    UserSearchPanel.Visibility = Visibility.Collapsed;
            }
        }
        else
        {
            UserSearchPanel.Visibility = Visibility.Collapsed;
        }

        // Hicbir sonuc yoksa empty state goster
        if (filtered.Count == 0 && UserSearchPanel.Visibility == Visibility.Collapsed)
        {
            EmptyState.Visibility = Visibility.Visible;
        }
        else
        {
            EmptyState.Visibility = Visibility.Collapsed;
        }
    }

    private void NewConversation_Click(object sender, RoutedEventArgs e)
    {
        // Kisiler sekmesine yonlendir
        var mainWindow = System.Windows.Application.Current.MainWindow as MainWindow;
        if (mainWindow != null)
        {
            var navContacts = mainWindow.FindName("NavContacts") as System.Windows.Controls.Primitives.ToggleButton;
            if (navContacts != null)
                navContacts.IsChecked = true;
        }
    }

    private void ConversationList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ConversationList.SelectedItem is ConversationItemViewModel conversation)
        {
            var mainWindow = System.Windows.Application.Current.MainWindow as MainWindow;
            mainWindow?.OpenConversation(conversation.Id);
        }
    }

    private async void UserSearchList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (UserSearchList.SelectedItem is UserSearchResultViewModel user)
        {
            try
            {
                var apiClient = App.Services.GetRequiredService<IApiClient>();
                var response = await apiClient.PostAsync<ApiResponse<ConversationDto>>(
                    $"api/conversations/direct/{user.Id}", null);

                if (response?.Success == true && response.Data != null)
                {
                    var mainWindow = System.Windows.Application.Current.MainWindow as MainWindow;
                    mainWindow?.OpenConversation(response.Data.Id);

                    // Konusma listesini yenile
                    LoadConversationsAsync();
                }
            }
            catch { }

            UserSearchList.SelectedItem = null;
        }
    }

    private static string FormatTime(DateTime? dateTime)
    {
        if (dateTime == null) return "";
        var local = dateTime.Value.ToLocalTime();
        var today = DateTime.Today;

        if (local.Date == today) return local.ToString("HH:mm");
        if (local.Date == today.AddDays(-1)) return "Dun";
        if (local.Date > today.AddDays(-7)) return local.ToString("ddd");
        return local.ToString("dd.MM.yy");
    }
}

public class ConversationItemViewModel
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string LastMessage { get; set; } = string.Empty;
    public string LastMessageTime { get; set; } = string.Empty;
    public int UnreadCount { get; set; }
    public UserStatus Status { get; set; }
    public bool IsGroup { get; set; }

    public string Initials => IsGroup ? "#" : (Name.Length > 0 ? Name[0].ToString().ToUpper() : "?");
    public Visibility UnreadVisibility => UnreadCount > 0 ? Visibility.Visible : Visibility.Collapsed;
    public Visibility ShowStatus => IsGroup ? Visibility.Collapsed : Visibility.Visible;

    public System.Windows.Media.Brush StatusBrush => Status switch
    {
        UserStatus.Available => Application.Current.FindResource("StatusAvailableBrush") as System.Windows.Media.Brush,
        UserStatus.Busy => Application.Current.FindResource("StatusBusyBrush") as System.Windows.Media.Brush,
        UserStatus.Away => Application.Current.FindResource("StatusAwayBrush") as System.Windows.Media.Brush,
        _ => Application.Current.FindResource("StatusOfflineBrush") as System.Windows.Media.Brush
    } ?? System.Windows.Media.Brushes.Gray;
}

public class UserSearchResultViewModel
{
    public Guid Id { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string Detail { get; set; } = string.Empty;
    public UserStatus Status { get; set; }

    public string Initials => DisplayName.Length > 0 ? DisplayName[0].ToString().ToUpper() : "?";
}
