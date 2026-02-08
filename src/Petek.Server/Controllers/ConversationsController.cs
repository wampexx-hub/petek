using Petek.Server.Services;
using Petek.Shared.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Petek.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ConversationsController : ControllerBase
{
    private readonly IConversationService _conversationService;
    private readonly IMessageService _messageService;
    private readonly ILogger<ConversationsController> _logger;

    public ConversationsController(
        IConversationService conversationService,
        IMessageService messageService,
        ILogger<ConversationsController> logger)
    {
        _conversationService = conversationService;
        _messageService = messageService;
        _logger = logger;
    }

    /// <summary>
    /// Kullanıcının sohbetlerini getir
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<ConversationSummaryDto>>>> GetConversations()
    {
        var userId = GetUserId();
        var conversations = await _conversationService.GetUserConversationsAsync(userId);
        return Ok(ApiResponse<List<ConversationSummaryDto>>.Ok(conversations));
    }

    /// <summary>
    /// Belirli bir sohbeti getir
    /// </summary>
    [HttpGet("{conversationId}")]
    public async Task<ActionResult<ApiResponse<ConversationDto>>> GetConversation(Guid conversationId)
    {
        var userId = GetUserId();
        var conversation = await _conversationService.GetConversationAsync(conversationId, userId);
        if (conversation == null)
        {
            return NotFound(ApiResponse<ConversationDto>.Fail("Sohbet bulunamadı"));
        }
        return Ok(ApiResponse<ConversationDto>.Ok(conversation));
    }

    /// <summary>
    /// Yeni sohbet oluştur
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<ApiResponse<ConversationDto>>> CreateConversation([FromBody] CreateConversationDto dto)
    {
        var userId = GetUserId();
        var conversation = await _conversationService.CreateConversationAsync(userId, dto);
        if (conversation == null)
        {
            return BadRequest(ApiResponse<ConversationDto>.Fail("Sohbet oluşturulamadı"));
        }
        return Ok(ApiResponse<ConversationDto>.Ok(conversation, "Sohbet oluşturuldu"));
    }

    /// <summary>
    /// Direkt sohbet başlat veya mevcut olanı getir
    /// </summary>
    [HttpPost("direct/{targetUserId}")]
    public async Task<ActionResult<ApiResponse<ConversationDto>>> GetOrCreateDirect(Guid targetUserId)
    {
        var userId = GetUserId();
        var conversation = await _conversationService.GetOrCreateDirectConversationAsync(userId, targetUserId);
        if (conversation == null)
        {
            return BadRequest(ApiResponse<ConversationDto>.Fail("Sohbet oluşturulamadı"));
        }
        return Ok(ApiResponse<ConversationDto>.Ok(conversation));
    }

    /// <summary>
    /// Sohbeti güncelle (grup adı, avatar vb.)
    /// </summary>
    [HttpPut("{conversationId}")]
    public async Task<ActionResult<ApiResponse<ConversationDto>>> UpdateConversation(
        Guid conversationId,
        [FromBody] UpdateConversationDto dto)
    {
        var userId = GetUserId();
        var conversation = await _conversationService.UpdateConversationAsync(userId, conversationId, dto);
        if (conversation == null)
        {
            return NotFound(ApiResponse<ConversationDto>.Fail("Sohbet bulunamadı veya yetkiniz yok"));
        }
        return Ok(ApiResponse<ConversationDto>.Ok(conversation, "Sohbet güncellendi"));
    }

    /// <summary>
    /// Sohbete katılımcı ekle
    /// </summary>
    [HttpPost("{conversationId}/participants/{userId}")]
    public async Task<ActionResult<ApiResponse<bool>>> AddParticipant(Guid conversationId, Guid userId)
    {
        var currentUserId = GetUserId();
        var result = await _conversationService.AddParticipantAsync(conversationId, userId, currentUserId);
        if (!result)
        {
            return BadRequest(ApiResponse<bool>.Fail("Katılımcı eklenemedi"));
        }
        return Ok(ApiResponse<bool>.Ok(true, "Katılımcı eklendi"));
    }

    /// <summary>
    /// Sohbetten katılımcı çıkar
    /// </summary>
    [HttpDelete("{conversationId}/participants/{userId}")]
    public async Task<ActionResult<ApiResponse<bool>>> RemoveParticipant(Guid conversationId, Guid userId)
    {
        var currentUserId = GetUserId();
        var result = await _conversationService.RemoveParticipantAsync(conversationId, userId, currentUserId);
        if (!result)
        {
            return BadRequest(ApiResponse<bool>.Fail("Katılımcı çıkarılamadı"));
        }
        return Ok(ApiResponse<bool>.Ok(true, "Katılımcı çıkarıldı"));
    }

    /// <summary>
    /// Sohbetin mesajlarını getir
    /// </summary>
    [HttpGet("{conversationId}/messages")]
    public async Task<ActionResult<ApiResponse<List<MessageDto>>>> GetMessages(
        Guid conversationId,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 50)
    {
        var userId = GetUserId();
        if (!await _conversationService.IsParticipantAsync(conversationId, userId))
        {
            return Forbid();
        }

        var messages = await _messageService.GetMessagesAsync(conversationId, skip, take);
        return Ok(ApiResponse<List<MessageDto>>.Ok(messages));
    }

    /// <summary>
    /// Sohbete mesaj gönder (HTTP üzerinden test için)
    /// </summary>
    [HttpPost("{conversationId}/messages")]
    public async Task<ActionResult<ApiResponse<MessageDto>>> SendMessage(Guid conversationId, [FromBody] SendMessageDto dto)
    {
        var userId = GetUserId();
        dto.ConversationId = conversationId;
        
        var message = await _messageService.SendMessageAsync(userId, dto);
        if (message == null)
        {
            return BadRequest(ApiResponse<MessageDto>.Fail("Mesaj gönderilemedi. Sohbete kayıtlı olmayabilirsiniz."));
        }

        return Ok(ApiResponse<MessageDto>.Ok(message, "Mesaj gönderildi"));
    }

    private Guid GetUserId()
    {
        var userIdClaim = User.FindFirst("sub")?.Value
                          ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(userIdClaim, out var userId) ? userId : Guid.Empty;
    }
}
