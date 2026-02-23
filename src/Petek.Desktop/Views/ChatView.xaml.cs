using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Petek.Shared.Enums;
using Microsoft.Win32;
using Petek.Desktop.ViewModels;
using Petek.Desktop.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Petek.Desktop.Views;

public partial class ChatView : Page
{
    private readonly ChatViewModel _viewModel;
    private readonly IScreenshotService _screenshotService;

    public ChatView()
    {
        InitializeComponent();
        _viewModel = App.Services.GetRequiredService<ChatViewModel>();
        _screenshotService = App.Services.GetRequiredService<IScreenshotService>();
        DataContext = _viewModel;
        MessagesList.ItemsSource = _viewModel.Messages;
    }

    public async void LoadConversation(Guid conversationId)
    {
        await _viewModel.LoadConversationCommand.ExecuteAsync(conversationId);
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

    private async void AttachFile_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = "Dosya Sec",
            Filter = "Tum Dosyalar (*.*)|*.*|" +
                     "Resimler (*.png;*.jpg;*.jpeg;*.gif;*.bmp)|*.png;*.jpg;*.jpeg;*.gif;*.bmp|" +
                     "Belgeler (*.pdf;*.doc;*.docx;*.xls;*.xlsx;*.ppt;*.pptx;*.txt)|*.pdf;*.doc;*.docx;*.xls;*.xlsx;*.ppt;*.pptx;*.txt|" +
                     "Arsivler (*.zip;*.rar;*.7z)|*.zip;*.rar;*.7z",
            Multiselect = false
        };

        if (dialog.ShowDialog() == true)
        {
            await _viewModel.SendFileAsync(dialog.FileName);
        }
    }

    private async void Screenshot_Click(object sender, RoutedEventArgs e)
    {
        // Ana pencereyi gizle
        var mainWindow = Application.Current.MainWindow;
        if (mainWindow != null)
        {
            mainWindow.WindowState = WindowState.Minimized;
        }

        // Kisa bir bekleme (pencerenin minimize olmasi icin)
        await Task.Delay(300);

        var screenshotBytes = await _screenshotService.CaptureAndSelectRegionAsync();

        // Pencereyi geri getir
        if (mainWindow != null)
        {
            mainWindow.WindowState = WindowState.Normal;
            mainWindow.Activate();
        }

        if (screenshotBytes != null)
        {
            await _viewModel.SendScreenshotAsync(screenshotBytes);
        }
    }

    private void MessagesArea_DragOver(object sender, System.Windows.DragEventArgs e)
    {
        if (e.Data.GetDataPresent(System.Windows.DataFormats.FileDrop))
        {
            e.Effects = System.Windows.DragDropEffects.Copy;
            DragDropOverlay.Visibility = Visibility.Visible;
        }
        else
        {
            e.Effects = System.Windows.DragDropEffects.None;
        }
        e.Handled = true;
    }

    private async void MessagesArea_Drop(object sender, System.Windows.DragEventArgs e)
    {
        DragDropOverlay.Visibility = Visibility.Collapsed;

        if (!e.Data.GetDataPresent(System.Windows.DataFormats.FileDrop)) return;

        var files = (string[])e.Data.GetData(System.Windows.DataFormats.FileDrop);
        if (files == null || files.Length == 0) return;

        foreach (var filePath in files)
        {
            if (File.Exists(filePath))
            {
                await _viewModel.SendFileAsync(filePath);
            }
        }
    }

    private void Attachment_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement element && element.DataContext is MessageDisplayViewModel msg)
        {
            _viewModel.DownloadAttachmentCommand.Execute(msg);
        }
    }
}
