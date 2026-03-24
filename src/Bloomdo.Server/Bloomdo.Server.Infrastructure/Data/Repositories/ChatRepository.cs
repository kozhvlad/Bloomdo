using Bloomdo.Server.Application.Interfaces;
using Bloomdo.Server.Domain.Entities;
using Bloomdo.Server.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Bloomdo.Server.Infrastructure.Data.Repositories;

public class ChatRepository(AppDbContext context) : IChatRepository
{
    public async Task<List<ChatConversation>> GetConversationsAsync(Guid accountId, CancellationToken ct = default)
    {
        return await context.ChatConversations
            .Where(c => c.AccountId == accountId && !c.IsDeleted)
            .Include(c => c.Messages.Where(m => !m.IsDeleted).OrderByDescending(m => m.CreatedAt).Take(1))
            .OrderByDescending(c => c.UpdatedAt ?? c.CreatedAt)
            .ToListAsync(ct);
    }

    public async Task<ChatConversation?> GetConversationWithMessagesAsync(Guid conversationId, Guid accountId, CancellationToken ct = default)
    {
        return await context.ChatConversations
            .Where(c => c.Id == conversationId && c.AccountId == accountId && !c.IsDeleted)
            .Include(c => c.Messages.Where(m => !m.IsDeleted).OrderBy(m => m.CreatedAt))
            .FirstOrDefaultAsync(ct);
    }

    public async Task<ChatConversation> CreateConversationAsync(ChatConversation conversation, CancellationToken ct = default)
    {
        var entry = await context.ChatConversations.AddAsync(conversation, ct);
        await context.SaveChangesAsync(ct);
        return entry.Entity;
    }

    public async Task<ChatMessage> AddMessageAsync(ChatMessage message, CancellationToken ct = default)
    {
        var entry = await context.ChatMessages.AddAsync(message, ct);
        await context.SaveChangesAsync(ct);
        return entry.Entity;
    }

    public async Task<bool> DeleteConversationAsync(Guid conversationId, Guid accountId, CancellationToken ct = default)
    {
        var conversation = await context.ChatConversations
            .FirstOrDefaultAsync(c => c.Id == conversationId && c.AccountId == accountId && !c.IsDeleted, ct);

        if (conversation is null) return false;

        conversation.IsDeleted = true;
        conversation.DeletedAt = DateTime.UtcNow;
        await context.SaveChangesAsync(ct);
        return true;
    }

    public async Task UpdateConversationAsync(ChatConversation conversation, CancellationToken ct = default)
    {
        context.ChatConversations.Update(conversation);
        await Task.CompletedTask;
    }

    public async Task SaveChangesAsync(CancellationToken ct = default)
    {
        await context.SaveChangesAsync(ct);
    }

    public async Task<int> CountTodayUserMessagesAsync(Guid accountId, CancellationToken ct = default)
    {
        var todayUtc = DateTime.UtcNow.Date;
        var tomorrowUtc = todayUtc.AddDays(1);

        return await context.ChatMessages
            .Where(m => !m.IsDeleted
                && m.Role == "user"
                && m.CreatedAt >= todayUtc
                && m.CreatedAt < tomorrowUtc
                && m.Conversation.AccountId == accountId
                && !m.Conversation.IsDeleted)
            .CountAsync(ct);
    }
}
