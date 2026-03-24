using Bloomdo.Server.Domain.Entities;

namespace Bloomdo.Server.Application.Interfaces;

public interface IChatRepository
{
    Task<List<ChatConversation>> GetConversationsAsync(Guid accountId, CancellationToken ct = default);
    Task<ChatConversation?> GetConversationWithMessagesAsync(Guid conversationId, Guid accountId, CancellationToken ct = default);
    Task<ChatConversation> CreateConversationAsync(ChatConversation conversation, CancellationToken ct = default);
    Task<ChatMessage> AddMessageAsync(ChatMessage message, CancellationToken ct = default);
    Task<bool> DeleteConversationAsync(Guid conversationId, Guid accountId, CancellationToken ct = default);
    Task UpdateConversationAsync(ChatConversation conversation, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
    Task<int> CountTodayUserMessagesAsync(Guid accountId, CancellationToken ct = default);
}
