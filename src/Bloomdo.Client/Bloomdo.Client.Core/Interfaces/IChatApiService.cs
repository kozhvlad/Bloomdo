using Bloomdo.Shared.DTOs.Chat;

namespace Bloomdo.Client.Core.Interfaces;

public interface IChatApiService
{
    Task<List<ChatConversationResponse>?> GetConversationsAsync(CancellationToken ct = default);
    Task<ChatConversationDetailResponse?> GetConversationAsync(Guid conversationId, CancellationToken ct = default);
    Task<SendMessageResponse?> CreateConversationAsync(string message, TodayLocalContext? todayContext = null, CancellationToken ct = default);
    Task<SendMessageResponse?> SendMessageAsync(Guid conversationId, string message, TodayLocalContext? todayContext = null, CancellationToken ct = default);
    Task<bool> DeleteConversationAsync(Guid conversationId, CancellationToken ct = default);
}
