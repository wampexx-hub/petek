using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Petek.Desktop.Services;
using Petek.Shared.DTOs;
using Petek.Shared.Enums;
using System.Collections.ObjectModel;
using System.IO;
using System.Net.Http;
using System.Text.Json;

namespace Petek.Desktop.ViewModels;

public partial class ChatViewModel : ObservableObject
{
    private readonly ISignalRService _signalRService;
    private readonly IApiClient _apiClient;
    private readonly IAuthenticationService _authService;

    [ObservableProperty]
    private ConversationDto? _currentConversation;

    [ObservableProperty]
    private string _messageText = string.Empty;

    [ObservableProperty]
    private bool _isTyping;

    [ObservableProperty]
    private string? _typingUserName;

    [ObservableProperty]
    private bool _isSendingFile;

    public ObservableCollection<MessageDisplayViewModel> Messages { get; } = new();

    public ChatViewModel(
        ISignalRService signalRService,
        IApiClient apiClient,
        IAuthenticationService authService)
    {
        _signalRService = signalRService;
        _apiClient = apiClient;
        _authService = authService;

        // Subscribe to events
        _signalRService.MessageReceived += OnMessageReceived;
        _signalRService.MessageStatusUpdated += OnMessageStatusUpdated;
        _signalRService.UserTyping += OnUserTyping;
    }

    [RelayCommand]
    private async Task LoadConversationAsync(Guid conversationId)
    {
        var response = await _apiClient.GetAsync<ApiResponse<ConversationDto>>(
            $"api/conversations/{conversationId}");

        if (response?.Success == true)
        {
            CurrentConversation = response.Data;
            await LoadMessagesAsync(conversationId);
            await _signalRService.JoinConversationAsync(conversationId);
        }
    }

    private async Task LoadMessagesAsync(Guid conversationId)
    {
        var response = await _apiClient.GetAsync<ApiResponse<List<MessageDto>>>(
            $"api/conversations/{conversationId}/messages");

        if (response?.Success == true && response.Data != null)
        {
            Messages.Clear();
            foreach (var message in response.Data.OrderBy(m => m.SentAt))
            {
                Messages.Add(new MessageDisplayViewModel(message, _authService.CurrentUser?.Id));
            }
        }
    }

    [RelayCommand]
    private async Task SendMessageAsync()
    {
        if (string.IsNullOrWhiteSpace(MessageText) || CurrentConversation == null) return;

        var dto = new SendMessageDto
        {
            ConversationId = CurrentConversation.Id,
            Content = MessageText.Trim(),
            Type = MessageType.Text
        };

        await _signalRService.SendMessageAsync(dto);
        MessageText = string.Empty;
    }

    /// <summary>
    /// Dosya gonder (dosya yolundan)
    /// </summary>
    public async Task SendFileAsync(string filePath)
    {
        if (CurrentConversation == null || !File.Exists(filePath)) return;

        try
        {
            IsSendingFile = true;

            using var fileStream = File.OpenRead(filePath);
            var fileName = Path.GetFileName(filePath);

            var response = await _apiClient.UploadFileAsync("api/files", fileStream, fileName);

            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<ApiResponse<FileUploadResultDto>>(json,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (result?.Data != null)
                {
                    var dto = new SendMessageDto
                    {
                        ConversationId = CurrentConversation.Id,
                        Content = $"[Dosya: {fileName}]",
                        Type = MessageType.File
                    };

                    await _signalRService.SendMessageAsync(dto);
                }
            }
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show(
                $"Dosya gonderilemedi: {ex.Message}",
                "Hata",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Error);
        }
        finally
        {
            IsSendingFile = false;
        }
    }

    /// <summary>
    /// Ekran goruntusu gonder (byte dizisinden)
    /// </summary>
    public async Task SendScreenshotAsync(byte[] screenshotBytes)
    {
        if (CurrentConversation == null || screenshotBytes == null || screenshotBytes.Length == 0) return;

        try
        {
            IsSendingFile = true;

            var fileName = $"ekran_goruntusu_{DateTime.Now:yyyyMMdd_HHmmss}.png";
            using var stream = new MemoryStream(screenshotBytes);

            var response = await _apiClient.UploadFileAsync("api/files", stream, fileName);

            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<ApiResponse<FileUploadResultDto>>(json,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (result?.Data != null)
                {
                    var dto = new SendMessageDto
                    {
                        ConversationId = CurrentConversation.Id,
                        Content = $"[Ekran Goruntusu: {fileName}]",
                        Type = MessageType.Screenshot
                    };

                    await _signalRService.SendMessageAsync(dto);
                }
            }
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show(
                $"Ekran goruntusu gonderilemedi: {ex.Message}",
                "Hata",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Error);
        }
        finally
        {
            IsSendingFile = false;
        }
    }

    [RelayCommand]
    private async Task DownloadAttachmentAsync(MessageDisplayViewModel message)
    {
        if (message.AttachmentId == null) return;

        try
        {
            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                FileName = message.AttachmentName ?? "dosya",
                Title = "Dosyayi Kaydet"
            };

            if (dialog.ShowDialog() == true)
            {
                var response = await _apiClient.GetAsync<byte[]>($"api/files/{message.AttachmentId}");
                // Basit indirme - gercek implementasyonda stream kullanilmali
            }
        }
        catch { }
    }

    [RelayCommand]
    private async Task MarkAsReadAsync(MessageDisplayViewModel message)
    {
        if (CurrentConversation == null) return;
        await _signalRService.MarkAsReadAsync(CurrentConversation.Id, message.Id);
    }

    partial void OnMessageTextChanged(string value)
    {
        // Send typing indicator
        if (CurrentConversation != null)
        {
            var isTyping = !string.IsNullOrEmpty(value);
            _ = _signalRService.SetTypingAsync(CurrentConversation.Id, isTyping);
        }
    }

    private void OnMessageReceived(MessageDto message)
    {
        if (CurrentConversation != null && message.ConversationId == CurrentConversation.Id)
        {
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                Messages.Add(new MessageDisplayViewModel(message, _authService.CurrentUser?.Id));
            });
        }
    }

    private void OnMessageStatusUpdated(Guid messageId, MessageStatus status, DateTime? timestamp)
    {
        var message = Messages.FirstOrDefault(m => m.Id == messageId);
        if (message != null)
        {
            message.Status = status;
        }
    }

    private void OnUserTyping(TypingIndicatorDto indicator)
    {
        if (CurrentConversation != null && indicator.ConversationId == CurrentConversation.Id)
        {
            IsTyping = indicator.IsTyping;
            TypingUserName = indicator.IsTyping ? indicator.UserName : null;
        }
    }
}

public partial class MessageDisplayViewModel : ObservableObject
{
    private readonly MessageDto _dto;
    private readonly Guid? _currentUserId;

    public MessageDisplayViewModel(MessageDto dto, Guid? currentUserId)
    {
        _dto = dto;
        _currentUserId = currentUserId;
    }

    public Guid Id => _dto.Id;
    public string Content => _dto.Content;
    public string Time => _dto.SentAt.ToLocalTime().ToString("HH:mm");
    public bool IsOutgoing => _dto.SenderId == _currentUserId;
    public bool HasTextContent => !string.IsNullOrWhiteSpace(_dto.Content) && _dto.Type == MessageType.Text;
    public bool HasAttachment => _dto.Attachment != null || _dto.Type == MessageType.File || _dto.Type == MessageType.Screenshot;
    public Guid? AttachmentId => _dto.Attachment?.Id;
    public string? AttachmentName => _dto.Attachment?.FileName ?? (_dto.Type == MessageType.File || _dto.Type == MessageType.Screenshot ? _dto.Content : null);
    public string? AttachmentSize => _dto.Attachment != null ? FormatFileSize(_dto.Attachment.FileSize) : null;

    [ObservableProperty]
    private MessageStatus _status;

    public Visibility IsIncomingVisibility => IsOutgoing ? Visibility.Collapsed : Visibility.Visible;
    public Visibility IsOutgoingVisibility => IsOutgoing ? Visibility.Visible : Visibility.Collapsed;

    public string StatusIcon => _dto.Status switch
    {
        MessageStatus.Sent => "\uE73E",
        MessageStatus.Delivered => "\uE73E",
        MessageStatus.Read => "\uE8FB",
        _ => ""
    };

    private static string FormatFileSize(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB" };
        int order = 0;
        double size = bytes;
        while (size >= 1024 && order < sizes.Length - 1)
        {
            order++;
            size /= 1024;
        }
        return $"{size:0.##} {sizes[order]}";
    }
}
