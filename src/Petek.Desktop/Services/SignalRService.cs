using Petek.Shared.DTOs;
using Petek.Shared.Enums;
using Petek.Shared.Models;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json.Serialization;

namespace Petek.Desktop.Services;

public interface ISignalRService
{
    bool IsConnected { get; }
    event Action<MessageDto>? MessageReceived;
    event Action<Guid, MessageStatus, DateTime?>? MessageStatusUpdated;
    event Action<Guid, UserStatus>? UserStatusChanged;
    event Action<TypingIndicatorDto>? UserTyping;
    event Action<ConversationDto>? ConversationCreated;
    event Action<Guid>? UserOnline;
    event Action<Guid>? UserOffline;
    event Action<string, string>? NotificationReceived;

    Task ConnectAsync(string token);
    Task DisconnectAsync();
    Task SendMessageAsync(SendMessageDto message);
    Task MarkAsReadAsync(Guid conversationId, Guid messageId);
    Task SetTypingAsync(Guid conversationId, bool isTyping);
    Task UpdateStatusAsync(UserStatus status);
    Task JoinConversationAsync(Guid conversationId);
    Task LeaveConversationAsync(Guid conversationId);
}

public class SignalRService : ISignalRService, IAsyncDisposable
{
    private HubConnection? _connection;
    private readonly string _hubUrl;

    public bool IsConnected => _connection?.State == HubConnectionState.Connected;

    public event Action<MessageDto>? MessageReceived;
    public event Action<Guid, MessageStatus, DateTime?>? MessageStatusUpdated;
    public event Action<Guid, UserStatus>? UserStatusChanged;
    public event Action<TypingIndicatorDto>? UserTyping;
    public event Action<ConversationDto>? ConversationCreated;
    public event Action<Guid>? UserOnline;
    public event Action<Guid>? UserOffline;
    public event Action<string, string>? NotificationReceived;

    public SignalRService(IConfiguration? configuration = null)
    {
        // Server URL'yi local settings'den veya configuration'dan oku
        var hubUrl = configuration?["Api:BaseUrl"];

        if (string.IsNullOrEmpty(hubUrl))
        {
            try
            {
                var settingsPath = System.IO.Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "PetekMessenger", "settings.json");

                if (System.IO.File.Exists(settingsPath))
                {
                    var json = System.IO.File.ReadAllText(settingsPath);
                    var settings = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(json);
                    if (settings != null && settings.TryGetValue("ServerUrl", out var url) && !string.IsNullOrWhiteSpace(url))
                    {
                        hubUrl = url;
                    }
                }
            }
            catch { }
        }

        _hubUrl = hubUrl ?? "http://localhost:5000";
    }

    public async Task ConnectAsync(string token)
    {
        if (_connection != null)
        {
            await DisconnectAsync();
        }

        _connection = new HubConnectionBuilder()
            .WithUrl($"{_hubUrl}{SignalRConstants.HubPath}", options =>
            {
                options.AccessTokenProvider = () => Task.FromResult<string?>(token);
            })
            .WithAutomaticReconnect()
            .AddJsonProtocol(options =>
            {
                options.PayloadSerializerOptions.Converters.Add(new JsonStringEnumConverter());
            })
            .Build();

        RegisterHandlers();

        await _connection.StartAsync();
    }

    public async Task DisconnectAsync()
    {
        if (_connection != null)
        {
            await _connection.StopAsync();
            await _connection.DisposeAsync();
            _connection = null;
        }
    }

    private void RegisterHandlers()
    {
        if (_connection == null) return;

        _connection.On<MessageDto>(SignalRConstants.Methods.ReceiveMessage, message =>
        {
            MessageReceived?.Invoke(message);
        });

        _connection.On<Guid, MessageStatus, DateTime?>(SignalRConstants.Methods.MessageStatusUpdated,
            (messageId, status, timestamp) =>
            {
                MessageStatusUpdated?.Invoke(messageId, status, timestamp);
            });

        _connection.On<Guid, UserStatus>(SignalRConstants.Methods.UserStatusChanged, (userId, status) =>
        {
            UserStatusChanged?.Invoke(userId, status);
        });

        _connection.On<TypingIndicatorDto>(SignalRConstants.Methods.UserTyping, indicator =>
        {
            UserTyping?.Invoke(indicator);
        });

        _connection.On<ConversationDto>(SignalRConstants.Methods.ConversationCreated, conversation =>
        {
            ConversationCreated?.Invoke(conversation);
        });

        _connection.On<Guid>(SignalRConstants.Methods.UserOnline, userId =>
        {
            UserOnline?.Invoke(userId);
        });

        _connection.On<Guid>(SignalRConstants.Methods.UserOffline, userId =>
        {
            UserOffline?.Invoke(userId);
        });

        _connection.On<string, string>(SignalRConstants.Methods.ReceiveNotification, (title, message) =>
        {
            NotificationReceived?.Invoke(title, message);
        });
    }

    public async Task SendMessageAsync(SendMessageDto message)
    {
        if (_connection == null || !IsConnected) return;
        await _connection.InvokeAsync(SignalRConstants.Methods.SendMessage, message);
    }

    public async Task MarkAsReadAsync(Guid conversationId, Guid messageId)
    {
        if (_connection == null || !IsConnected) return;
        await _connection.InvokeAsync(SignalRConstants.Methods.MarkAsRead, conversationId, messageId);
    }

    public async Task SetTypingAsync(Guid conversationId, bool isTyping)
    {
        if (_connection == null || !IsConnected) return;
        await _connection.InvokeAsync(SignalRConstants.Methods.SetTyping, conversationId, isTyping);
    }

    public async Task UpdateStatusAsync(UserStatus status)
    {
        if (_connection == null || !IsConnected) return;
        await _connection.InvokeAsync(SignalRConstants.Methods.UpdateStatus, status);
    }

    public async Task JoinConversationAsync(Guid conversationId)
    {
        if (_connection == null || !IsConnected) return;
        await _connection.InvokeAsync(SignalRConstants.Methods.JoinConversation, conversationId);
    }

    public async Task LeaveConversationAsync(Guid conversationId)
    {
        if (_connection == null || !IsConnected) return;
        await _connection.InvokeAsync(SignalRConstants.Methods.LeaveConversation, conversationId);
    }

    public async ValueTask DisposeAsync()
    {
        await DisconnectAsync();
    }
}
