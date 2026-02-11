using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Petek.Desktop.Services;
using Petek.Shared.DTOs;
using Microsoft.Extensions.DependencyInjection;

namespace Petek.Desktop.Views;

public partial class ContactListView : Page
{
    private readonly List<ContactItemViewModel> _allContacts = new();
    private readonly List<string> _customCategories = new();
    private System.Threading.CancellationTokenSource? _searchCts;

    public ContactListView()
    {
        InitializeComponent();
        LoadCustomCategories();
        LoadAllUsersAsync();
    }

    private async void LoadAllUsersAsync()
    {
        try
        {
            var apiClient = App.Services.GetRequiredService<IApiClient>();
            var response = await apiClient.GetAsync<ApiResponse<List<UserDto>>>("api/users");
            if (response?.Success == true && response.Data != null)
            {
                _allContacts.Clear();
                AllContactsNode.Items.Clear();

                foreach (var user in response.Data)
                {
                    var vm = new ContactItemViewModel
                    {
                        Id = user.Id,
                        Name = user.DisplayName ?? user.Username,
                        Title = user.Title ?? "",
                        Department = user.Department ?? "",
                        Status = user.Status.ToString(),
                        IsFavorite = false
                    };
                    _allContacts.Add(vm);

                    var item = CreateContactTreeItem(vm);
                    AllContactsNode.Items.Add(item);
                }
            }
        }
        catch { }
    }

    private TreeViewItem CreateContactTreeItem(ContactItemViewModel contact)
    {
        var item = new TreeViewItem { Cursor = System.Windows.Input.Cursors.Hand };

        var panel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 3, 0, 3) };

        // Status indicator
        var statusDot = new System.Windows.Shapes.Ellipse
        {
            Width = 8,
            Height = 8,
            Margin = new Thickness(0, 0, 8, 0),
            VerticalAlignment = VerticalAlignment.Center
        };

        var statusColor = contact.Status switch
        {
            "Available" => "StatusAvailableBrush",
            "Busy" or "DoNotDisturb" => "StatusBusyBrush",
            "Away" or "BeRightBack" => "StatusAwayBrush",
            _ => "StatusOfflineBrush"
        };
        statusDot.SetResourceReference(System.Windows.Shapes.Ellipse.FillProperty, statusColor);
        panel.Children.Add(statusDot);

        // Name
        var nameText = new TextBlock
        {
            Text = contact.Name,
            FontSize = 13,
            VerticalAlignment = VerticalAlignment.Center
        };
        nameText.SetResourceReference(TextBlock.ForegroundProperty, "TextPrimaryBrush");
        panel.Children.Add(nameText);

        // Status label for offline
        if (contact.Status == "Offline")
        {
            var offlineLabel = new TextBlock
            {
                Text = " (Çevrimdışı)",
                FontSize = 11,
                VerticalAlignment = VerticalAlignment.Center
            };
            offlineLabel.SetResourceReference(TextBlock.ForegroundProperty, "TextTertiaryBrush");
            panel.Children.Add(offlineLabel);
        }

        // Title (if available)
        if (!string.IsNullOrEmpty(contact.Title))
        {
            var titleText = new TextBlock
            {
                Text = $" - {contact.Title}",
                FontSize = 11,
                VerticalAlignment = VerticalAlignment.Center
            };
            titleText.SetResourceReference(TextBlock.ForegroundProperty, "TextTertiaryBrush");
            panel.Children.Add(titleText);
        }

        item.Header = panel;
        item.Tag = contact;

        // Tek tikla kisi detayini goster
        item.Selected += (s, e) =>
        {
            e.Handled = true;
            var mainWindow = System.Windows.Application.Current.MainWindow as MainWindow;
            mainWindow?.ShowContactDetail(contact);
        };

        // Cift tikla direkt sohbet baslat
        item.MouseDoubleClick += (s, e) =>
        {
            e.Handled = true;
            StartConversationWithContact(contact);
        };

        return item;
    }

    private async void StartConversationWithContact(ContactItemViewModel contact)
    {
        try
        {
            var apiClient = App.Services.GetRequiredService<IApiClient>();
            var response = await apiClient.PostAsync<ApiResponse<ConversationDto>>(
                $"api/conversations/direct/{contact.Id}", null);

            if (response?.Success == true && response.Data != null)
            {
                var mainWindow = System.Windows.Application.Current.MainWindow as MainWindow;
                mainWindow?.OpenConversation(response.Data.Id);
            }
        }
        catch { }
    }

    private void LoadCustomCategories()
    {
        try
        {
            var settingsPath = System.IO.Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "PetekMessenger", "settings.json");

            if (System.IO.File.Exists(settingsPath))
            {
                var json = System.IO.File.ReadAllText(settingsPath);
                var settings = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, System.Text.Json.JsonElement>>(json);
                if (settings != null && settings.TryGetValue("ContactCategories", out var cats))
                {
                    var list = System.Text.Json.JsonSerializer.Deserialize<List<string>>(cats.GetRawText());
                    if (list != null)
                    {
                        foreach (var cat in list)
                        {
                            _customCategories.Add(cat);
                            AddCategoryNode(cat);
                        }
                    }
                }
            }
        }
        catch { }
    }

    private void SaveCustomCategories()
    {
        try
        {
            var settingsPath = System.IO.Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "PetekMessenger", "settings.json");

            var dir = System.IO.Path.GetDirectoryName(settingsPath)!;
            if (!System.IO.Directory.Exists(dir))
                System.IO.Directory.CreateDirectory(dir);

            Dictionary<string, object> settings;
            if (System.IO.File.Exists(settingsPath))
            {
                var existingJson = System.IO.File.ReadAllText(settingsPath);
                settings = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(existingJson)
                           ?? new Dictionary<string, object>();
            }
            else
            {
                settings = new Dictionary<string, object>();
            }

            settings["ContactCategories"] = _customCategories;
            var json = System.Text.Json.JsonSerializer.Serialize(settings,
                new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
            System.IO.File.WriteAllText(settingsPath, json);
        }
        catch { }
    }

    private TreeViewItem AddCategoryNode(string name)
    {
        var node = new TreeViewItem { Header = name, IsExpanded = true };
        node.HeaderTemplate = CreateCategoryHeaderTemplate(name, "\uE8D5");

        var contextMenu = new ContextMenu();
        var deleteItem = new MenuItem { Header = "Kategoriyi Sil" };
        deleteItem.Click += (s, e) => RemoveCategory(name, node);
        contextMenu.Items.Add(deleteItem);
        node.ContextMenu = contextMenu;

        var index = ContactTree.Items.IndexOf(AllContactsNode);
        ContactTree.Items.Insert(index, node);

        return node;
    }

    private DataTemplate CreateCategoryHeaderTemplate(string name, string icon)
    {
        var template = new DataTemplate();
        var factory = new FrameworkElementFactory(typeof(StackPanel));
        factory.SetValue(StackPanel.OrientationProperty, Orientation.Horizontal);

        var iconFactory = new FrameworkElementFactory(typeof(TextBlock));
        iconFactory.SetValue(TextBlock.TextProperty, icon);
        iconFactory.SetValue(TextBlock.FontFamilyProperty, new System.Windows.Media.FontFamily("Segoe MDL2 Assets"));
        iconFactory.SetValue(TextBlock.FontSizeProperty, 14.0);
        iconFactory.SetValue(TextBlock.MarginProperty, new Thickness(0, 0, 8, 0));
        iconFactory.SetResourceReference(TextBlock.ForegroundProperty, "TextSecondaryBrush");
        factory.AppendChild(iconFactory);

        var textFactory = new FrameworkElementFactory(typeof(TextBlock));
        textFactory.SetBinding(TextBlock.TextProperty, new System.Windows.Data.Binding());
        textFactory.SetValue(TextBlock.FontWeightProperty, FontWeights.SemiBold);
        textFactory.SetResourceReference(TextBlock.ForegroundProperty, "TextPrimaryBrush");
        factory.AppendChild(textFactory);

        template.VisualTree = factory;
        return template;
    }

    private void RemoveCategory(string name, TreeViewItem node)
    {
        var result = MessageBox.Show(
            $"'{name}' kategorisini silmek istediğinize emin misiniz?",
            "Kategori Sil",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result == MessageBoxResult.Yes)
        {
            ContactTree.Items.Remove(node);
            _customCategories.Remove(name);
            SaveCustomCategories();
        }
    }

    public void AddCategory_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new CategoryInputDialog();
        dialog.Owner = Window.GetWindow(this);

        if (dialog.ShowDialog() == true && !string.IsNullOrWhiteSpace(dialog.CategoryName))
        {
            var name = dialog.CategoryName.Trim();

            if (_customCategories.Contains(name))
            {
                MessageBox.Show("Bu kategori zaten var.", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            _customCategories.Add(name);
            AddCategoryNode(name);
            SaveCustomCategories();
        }
    }

    public async void FilterContacts(string query)
    {
        // Önceki aramayı iptal et
        _searchCts?.Cancel();
        _searchCts = new System.Threading.CancellationTokenSource();
        var token = _searchCts.Token;

        if (string.IsNullOrWhiteSpace(query))
        {
            ContactTree.Visibility = Visibility.Visible;
            SearchResultsPanel.Visibility = Visibility.Collapsed;
            EmptySearchResult.Visibility = Visibility.Collapsed;

            foreach (TreeViewItem group in ContactTree.Items)
            {
                group.Visibility = Visibility.Visible;
                foreach (var item in group.Items)
                {
                    if (item is TreeViewItem childItem)
                        childItem.Visibility = Visibility.Visible;
                }
            }
            return;
        }

        if (query.Length < 2)
        {
            // 1 karakter: yerel filtreleme
            FilterLocal(query);
            return;
        }

        // Debounce: kısa bir bekleme
        try { await Task.Delay(300, token); } catch { return; }
        if (token.IsCancellationRequested) return;

        try
        {
            var apiClient = App.Services.GetRequiredService<IApiClient>();
            var response = await apiClient.GetAsync<ApiResponse<List<UserDto>>>(
                $"api/users/search?q={Uri.EscapeDataString(query)}");

            if (token.IsCancellationRequested) return;

            if (response?.Success == true && response.Data != null)
            {
                ContactTree.Visibility = Visibility.Collapsed;
                SearchResultsList.Items.Clear();

                if (response.Data.Count == 0)
                {
                    SearchResultsPanel.Visibility = Visibility.Collapsed;
                    EmptySearchResult.Visibility = Visibility.Visible;
                }
                else
                {
                    EmptySearchResult.Visibility = Visibility.Collapsed;
                    SearchResultsPanel.Visibility = Visibility.Visible;
                    SearchResultsTitle.Text = $"Arama Sonuçları ({response.Data.Count})";

                    foreach (var user in response.Data)
                    {
                        var vm = new ContactItemViewModel
                        {
                            Id = user.Id,
                            Name = user.DisplayName ?? user.Username,
                            Title = user.Title ?? "",
                            Department = user.Department ?? "",
                            Status = user.Status.ToString(),
                            IsFavorite = false
                        };
                        var item = CreateContactTreeItem(vm);
                        SearchResultsList.Items.Add(item);
                    }
                }
            }
            else
            {
                // API başarısız - yerel filtrele
                FilterLocal(query);
            }
        }
        catch
        {
            if (!token.IsCancellationRequested)
                FilterLocal(query);
        }
    }

    private void FilterLocal(string query)
    {
        SearchResultsPanel.Visibility = Visibility.Collapsed;
        ContactTree.Visibility = Visibility.Visible;

        bool found = false;
        foreach (TreeViewItem group in ContactTree.Items)
        {
            bool groupHasMatch = false;
            foreach (var item in group.Items)
            {
                if (item is TreeViewItem childItem)
                {
                    var contact = childItem.Tag as ContactItemViewModel;
                    var name = contact?.Name ?? "";
                    var matches = name.Contains(query, StringComparison.OrdinalIgnoreCase);
                    childItem.Visibility = matches ? Visibility.Visible : Visibility.Collapsed;
                    if (matches) { groupHasMatch = true; found = true; }
                }
            }
            group.Visibility = groupHasMatch ? Visibility.Visible : Visibility.Collapsed;
        }

        ContactTree.Visibility = found ? Visibility.Visible : Visibility.Collapsed;
        EmptySearchResult.Visibility = found ? Visibility.Collapsed : Visibility.Visible;
    }
}

public class ContactItemViewModel
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    public string Status { get; set; } = "Offline";
    public bool IsFavorite { get; set; }
    public string? Category { get; set; }
}
