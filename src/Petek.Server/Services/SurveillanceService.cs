using Petek.Server.Data;
using Petek.Server.Data.Entities;
using Petek.Shared.DTOs;
using Microsoft.EntityFrameworkCore;
using System.Text;
using System.Text.Json;

namespace Petek.Server.Services;

public class SurveillanceService : ISurveillanceService
{
    private readonly PetekDbContext _context;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<SurveillanceService> _logger;

    public SurveillanceService(
        PetekDbContext context,
        IHttpClientFactory httpClientFactory,
        ILogger<SurveillanceService> logger)
    {
        _context = context;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<bool> SendMessageEventAsync(MessageDto message)
    {
        var configs = await _context.SurveillanceConfigs
            .Where(c => c.IsEnabled && c.CaptureMessages)
            .ToListAsync();

        var success = true;
        foreach (var config in configs)
        {
            try
            {
                await SendWebhookAsync(config, "message", new
                {
                    eventType = "message",
                    timestamp = DateTime.UtcNow,
                    data = message
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send message event to webhook {WebhookUrl}", config.WebhookUrl);
                config.LastError = ex.Message;
                success = false;
            }
        }

        await _context.SaveChangesAsync();
        return success;
    }

    public async Task<bool> SendFileEventAsync(FileAttachmentDto file, Guid senderId)
    {
        var configs = await _context.SurveillanceConfigs
            .Where(c => c.IsEnabled && c.CaptureFiles)
            .ToListAsync();

        var success = true;
        foreach (var config in configs)
        {
            try
            {
                await SendWebhookAsync(config, "file", new
                {
                    eventType = "file",
                    timestamp = DateTime.UtcNow,
                    senderId,
                    data = file
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send file event to webhook {WebhookUrl}", config.WebhookUrl);
                config.LastError = ex.Message;
                success = false;
            }
        }

        await _context.SaveChangesAsync();
        return success;
    }

    public async Task<bool> SendUserActivityEventAsync(Guid userId, string activity)
    {
        var configs = await _context.SurveillanceConfigs
            .Where(c => c.IsEnabled && c.CaptureUserActivity)
            .ToListAsync();

        var success = true;
        foreach (var config in configs)
        {
            try
            {
                await SendWebhookAsync(config, "activity", new
                {
                    eventType = "user_activity",
                    timestamp = DateTime.UtcNow,
                    userId,
                    activity
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send activity event to webhook {WebhookUrl}", config.WebhookUrl);
                config.LastError = ex.Message;
                success = false;
            }
        }

        await _context.SaveChangesAsync();
        return success;
    }

    public async Task<List<SurveillanceConfigDto>> GetConfigsAsync()
    {
        return await _context.SurveillanceConfigs
            .Select(c => new SurveillanceConfigDto
            {
                Id = c.Id,
                Name = c.Name,
                WebhookUrl = c.WebhookUrl,
                IsEnabled = c.IsEnabled,
                CaptureMessages = c.CaptureMessages,
                CaptureFiles = c.CaptureFiles,
                CaptureUserActivity = c.CaptureUserActivity,
                LastSuccessfulSync = c.LastSuccessfulSync,
                LastError = c.LastError
            })
            .ToListAsync();
    }

    public async Task<SurveillanceConfigDto?> SaveConfigAsync(SaveSurveillanceConfigDto dto)
    {
        var config = new SurveillanceConfig
        {
            Id = Guid.NewGuid(),
            Name = dto.Name,
            WebhookUrl = dto.WebhookUrl,
            ApiKey = dto.ApiKey,
            IsEnabled = dto.IsEnabled,
            CaptureMessages = dto.CaptureMessages,
            CaptureFiles = dto.CaptureFiles,
            CaptureUserActivity = dto.CaptureUserActivity,
            CreatedAt = DateTime.UtcNow
        };

        _context.SurveillanceConfigs.Add(config);
        await _context.SaveChangesAsync();

        return new SurveillanceConfigDto
        {
            Id = config.Id,
            Name = config.Name,
            WebhookUrl = config.WebhookUrl,
            IsEnabled = config.IsEnabled,
            CaptureMessages = config.CaptureMessages,
            CaptureFiles = config.CaptureFiles,
            CaptureUserActivity = config.CaptureUserActivity
        };
    }

    public async Task<bool> TestWebhookAsync(Guid configId)
    {
        var config = await _context.SurveillanceConfigs.FindAsync(configId);
        if (config == null) return false;

        try
        {
            await SendWebhookAsync(config, "test", new
            {
                eventType = "test",
                timestamp = DateTime.UtcNow,
                message = "Petek Messenger webhook test"
            });

            config.LastSuccessfulSync = DateTime.UtcNow;
            config.LastError = null;
            await _context.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            config.LastError = ex.Message;
            await _context.SaveChangesAsync();
            return false;
        }
    }

    private async Task SendWebhookAsync(SurveillanceConfig config, string eventType, object payload)
    {
        var client = _httpClientFactory.CreateClient();

        if (!string.IsNullOrEmpty(config.ApiKey))
        {
            client.DefaultRequestHeaders.Add("X-API-Key", config.ApiKey);
        }

        var json = JsonSerializer.Serialize(payload);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await client.PostAsync(config.WebhookUrl, content);
        response.EnsureSuccessStatusCode();

        config.LastSuccessfulSync = DateTime.UtcNow;
    }
}
