using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using Petek.Shared.Enums;

namespace Petek.Desktop.Views;

public partial class ConversationListView : Page
{
    public ObservableCollection<ConversationItemViewModel> Conversations { get; } = new();
    private readonly List<ConversationItemViewModel> _allConversations = new();

    public ConversationListView()
    {
        InitializeComponent();
        DataContext = this;
        ConversationList.ItemsSource = Conversations;
    }

    public void FilterConversations(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            Conversations.Clear();
            foreach (var c in _allConversations)
                Conversations.Add(c);
        }
        else
        {
            var filtered = _allConversations
                .Where(c => c.Name.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                             c.LastMessage.Contains(query, StringComparison.OrdinalIgnoreCase))
                .ToList();

            Conversations.Clear();
            foreach (var c in filtered)
                Conversations.Add(c);
        }
    }

    private void NewConversation_Click(object sender, RoutedEventArgs e)
    {
        // TODO: Open new conversation dialog
    }

    private void ConversationList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ConversationList.SelectedItem is ConversationItemViewModel conversation)
        {
            // TODO: Navigate to chat view with selected conversation
        }
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
