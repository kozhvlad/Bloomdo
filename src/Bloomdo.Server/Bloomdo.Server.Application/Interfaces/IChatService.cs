using Bloomdo.Shared.DTOs.Chat;

namespace Bloomdo.Server.Application.Interfaces;

public interface IChatService
{
    Task<List<ChatConversationResponse>> GetConversationsAsync(Guid accountId, CancellationToken ct = default);
    Task<ChatConversationDetailResponse?> GetConversationAsync(Guid conversationId, Guid accountId, CancellationToken ct = default);
    Task<SendMessageResponse> SendMessageAsync(Guid accountId, Guid? conversationId, string message, TodayLocalContext? todayContext = null, CancellationToken ct = default);
    Task<bool> DeleteConversationAsync(Guid conversationId, Guid accountId, CancellationToken ct = default);
}
