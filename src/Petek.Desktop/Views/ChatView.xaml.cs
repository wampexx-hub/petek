using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Petek.Shared.Enums;
using Microsoft.Win32;
using Petek.Desktop.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace Petek.Desktop.Views;

public partial class ChatView : Page
{
    private readonly ChatViewModel _viewModel;

    public ChatView()
    {
        InitializeComponent();
        _viewModel = App.Services.GetRequiredService<ChatViewModel>();
        DataContext = _viewModel;
        MessagesList.ItemsSource = _viewModel.Messages;
    }

    private void SendMessage_Click(object sender, RoutedEventArgs e)
    {
        _viewModel.SendMessageCommand.Execute(null);
        MessageInput.Clear();
    }

    private void MessageInput_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && !Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
        {
            e.Handled = true;
            _viewModel.SendMessageCommand.Execute(null);
            MessageInput.Clear();
        }
    }

    private void MessageInput_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_viewModel != null)
        {
            _viewModel.MessageText = MessageInput.Text;
        }
    }

    private void AttachFile_Click(object sender, RoutedEventArgs e)
    {
        // TODO: Implement file attach in ViewModel
    }

    private void Screenshot_Click(object sender, RoutedEventArgs e)
    {
        // TODO: Implement screenshot in ViewModel
    }
}
