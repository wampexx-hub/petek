using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Petek.Desktop.Services;
using Petek.Shared.DTOs;
using System.Collections.ObjectModel;

namespace Petek.Desktop.ViewModels;

public partial class ContactsViewModel : ObservableObject
{
    private readonly IApiClient _apiClient;

    [ObservableProperty]
    private ContactDto? _selectedContact;

    [ObservableProperty]
    private string _searchQuery = string.Empty;

    public ObservableCollection<ContactDto> Contacts { get; } = new();
    public ObservableCollection<ContactDto> Favorites { get; } = new();
    public ObservableCollection<DepartmentDto> Departments { get; } = new();

    public ContactsViewModel(IApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    [RelayCommand]
    private async Task LoadContactsAsync()
    {
        var response = await _apiClient.GetAsync<ApiResponse<List<ContactDto>>>("api/contacts");

        if (response?.Success == true && response.Data != null)
        {
            Contacts.Clear();
            Favorites.Clear();

            foreach (var contact in response.Data)
            {
                Contacts.Add(contact);
                if (contact.IsFavorite)
                {
                    Favorites.Add(contact);
                }
            }
        }
    }

    [RelayCommand]
    private async Task LoadDepartmentsAsync()
    {
        var response = await _apiClient.GetAsync<ApiResponse<List<DepartmentDto>>>("api/users/departments");

        if (response?.Success == true && response.Data != null)
        {
            Departments.Clear();
            foreach (var dept in response.Data)
            {
                Departments.Add(dept);
            }
        }
    }

    [RelayCommand]
    private async Task SearchAsync()
    {
        if (string.IsNullOrWhiteSpace(SearchQuery) || SearchQuery.Length < 2) return;

        var response = await _apiClient.GetAsync<ApiResponse<List<UserDto>>>(
            $"api/users/search?q={Uri.EscapeDataString(SearchQuery)}");

        // Handle search results
    }

    [RelayCommand]
    private async Task AddToFavoritesAsync(ContactDto contact)
    {
        await _apiClient.PutAsync<ApiResponse<bool>>(
            $"api/contacts/{contact.UserId}/favorite",
            true);

        contact.IsFavorite = true;
        if (!Favorites.Contains(contact))
        {
            Favorites.Add(contact);
        }
    }

    [RelayCommand]
    private async Task RemoveFromFavoritesAsync(ContactDto contact)
    {
        await _apiClient.PutAsync<ApiResponse<bool>>(
            $"api/contacts/{contact.UserId}/favorite",
            false);

        contact.IsFavorite = false;
        Favorites.Remove(contact);
    }

    [RelayCommand]
    private async Task StartConversationAsync(ContactDto contact)
    {
        var response = await _apiClient.PostAsync<ApiResponse<ConversationDto>>(
            $"api/conversations/direct/{contact.UserId}",
            null);

        if (response?.Success == true && response.Data != null)
        {
            // Navigate to conversation
            // This would be handled by the navigation service
        }
    }
}
