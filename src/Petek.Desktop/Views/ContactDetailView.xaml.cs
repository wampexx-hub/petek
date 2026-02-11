using System.Windows;
using System.Windows.Controls;
using Petek.Desktop.Services;
using Petek.Shared.DTOs;
using Microsoft.Extensions.DependencyInjection;

namespace Petek.Desktop.Views;

public partial class ContactDetailView : Page
{
    private ContactItemViewModel? _contact;

    public ContactDetailView()
    {
        InitializeComponent();
    }

    public ContactDetailView(ContactItemViewModel contact) : this()
    {
        _contact = contact;
        ShowContact(contact);
    }

    private void ShowContact(ContactItemViewModel contact)
    {
        EmptyState.Visibility = Visibility.Collapsed;
        ContactDetail.Visibility = Visibility.Visible;

        ContactInitial.Text = contact.Name.Length > 0 ? contact.Name[0].ToString().ToUpper() : "?";
        ContactName.Text = contact.Name;

        var statusText = contact.Status switch
        {
            "Available" => "Uygun",
            "Busy" or "DoNotDisturb" => "Mesgul",
            "Away" or "BeRightBack" => "Disarida",
            "Invisible" => "Gorunmez",
            _ => "Cevrimdisi"
        };
        ContactStatus.Text = statusText;

        var statusBrushKey = contact.Status switch
        {
            "Available" => "StatusAvailableBrush",
            "Busy" or "DoNotDisturb" => "StatusBusyBrush",
            "Away" or "BeRightBack" => "StatusAwayBrush",
            _ => "StatusOfflineBrush"
        };
        StatusDot.SetResourceReference(System.Windows.Shapes.Ellipse.FillProperty, statusBrushKey);

        ContactTitle.Text = contact.Title;
        ContactTitle.Visibility = string.IsNullOrEmpty(contact.Title) ? Visibility.Collapsed : Visibility.Visible;

        ContactDepartment.Text = contact.Department;
        ContactDepartment.Visibility = string.IsNullOrEmpty(contact.Department) ? Visibility.Collapsed : Visibility.Visible;
    }

    private async void StartConversation_Click(object sender, RoutedEventArgs e)
    {
        if (_contact == null) return;

        try
        {
            var apiClient = App.Services.GetRequiredService<IApiClient>();
            var response = await apiClient.PostAsync<ApiResponse<ConversationDto>>(
                $"api/conversations/direct/{_contact.Id}", null);

            if (response?.Success == true && response.Data != null)
            {
                var mainWindow = Application.Current.MainWindow as MainWindow;
                mainWindow?.OpenConversation(response.Data.Id);
            }
        }
        catch { }
    }
}
