using System.Text;
using Bloomdo.Server.Application.Exceptions;
using Bloomdo.Server.Application.Interfaces;
using Bloomdo.Server.Application.Settings;
using Bloomdo.Server.Domain.Entities;
using Bloomdo.Shared.DTOs.Chat;
using Google.GenAI;
using Google.GenAI.Types;

namespace Bloomdo.Server.Application.Services;

public class ChatService(
    IChatRepository chatRepository,
    IStatsRepository statsRepository,
    IDailyActivityService activityService,
    IRepository<BlockRule> blockRuleRepository,
    IGeminiSettings geminiSettings,
    ISubscriptionService subscriptionService,
    IFreeLimitsSettings freeLimitsSettings) : IChatService
{
    private const string Model = "gemini-2.5-flash";
    private const int MaxHistoryMessages = 50;

    public async Task<List<ChatConversationResponse>> GetConversationsAsync(Guid accountId, CancellationToken ct = default)
    {
        var conversations = await chatRepository.GetConversationsAsync(accountId, ct);

        return conversations
            .OrderByDescending(c => c.UpdatedAt ?? c.CreatedAt)
            .Select(c =>
            {
                var lastMsg = c.Messages
                    .Where(m => !m.IsDeleted)
                    .OrderByDescending(m => m.CreatedAt)
                    .FirstOrDefault();

                return new ChatConversationResponse
                {
                    Id = c.Id,
                    Title = c.Title,
                    CreatedAt = c.CreatedAt,
                    UpdatedAt = c.UpdatedAt,
                    LastMessage = lastMsg is not null
                        ? new ChatMessageResponse
                        {
                            Id = lastMsg.Id,
                            Role = lastMsg.Role,
                            Content = lastMsg.Content,
                            CreatedAt = lastMsg.CreatedAt
                        }
                        : null
                };
            })
            .ToList();
    }

    public async Task<ChatConversationDetailResponse?> GetConversationAsync(Guid conversationId, Guid accountId, CancellationToken ct = default)
    {
        var conversation = await chatRepository.GetConversationWithMessagesAsync(conversationId, accountId, ct);
        if (conversation is null) return null;

        return new ChatConversationDetailResponse
        {
            Id = conversation.Id,
            Title = conversation.Title,
            CreatedAt = conversation.CreatedAt,
            Messages = conversation.Messages
                .Where(m => !m.IsDeleted)
                .OrderBy(m => m.CreatedAt)
                .Select(m => new ChatMessageResponse
                {
                    Id = m.Id,
                    Role = m.Role,
                    Content = m.Content,
                    CreatedAt = m.CreatedAt
                })
                .ToList()
        };
    }

    public async Task<SendMessageResponse> SendMessageAsync(Guid accountId, Guid? conversationId, string message, TodayLocalContext? todayContext = null, CancellationToken ct = default)
    {
        // Enforce daily message limit for free users
        var isPremium = await subscriptionService.IsPremiumAsync(accountId, ct);
        if (!isPremium)
        {
            var todayCount = await chatRepository.CountTodayUserMessagesAsync(accountId, ct);
            if (todayCount >= freeLimitsSettings.MaxDailyChatMessages)
            {
                throw new ChatLimitExceededException(freeLimitsSettings.MaxDailyChatMessages);
            }
        }

        ChatConversation conversation;

        if (conversationId.HasValue)
        {
            conversation = await chatRepository.GetConversationWithMessagesAsync(conversationId.Value, accountId, ct)
                ?? throw new InvalidOperationException("Conversation not found.");
        }
        else
        {
            conversation = new ChatConversation
            {
                AccountId = accountId,
                Title = message.Length > 50 ? message[..50] + "..." : message,
                CreatedAt = DateTime.UtcNow
            };
            conversation = await chatRepository.CreateConversationAsync(conversation, ct);
        }

        // Save user message
        var userMessage = new ChatMessage
        {
            ConversationId = conversation.Id,
            Role = "user",
            Content = message,
            CreatedAt = DateTime.UtcNow
        };
        userMessage = await chatRepository.AddMessageAsync(userMessage, ct);

        // Build context and call Gemini
        var systemPrompt = await BuildSystemPromptAsync(accountId, todayContext, ct);
        var history = conversation.Messages
            .Where(m => !m.IsDeleted)
            .OrderBy(m => m.CreatedAt)
            .TakeLast(MaxHistoryMessages)
            .ToList();

        var aiResponseText = await CallGeminiAsync(systemPrompt, history, message, ct);

        // Save assistant message
        var assistantMessage = new ChatMessage
        {
            ConversationId = conversation.Id,
            Role = "assistant",
            Content = aiResponseText,
            CreatedAt = DateTime.UtcNow
        };
        assistantMessage = await chatRepository.AddMessageAsync(assistantMessage, ct);

        // Update conversation timestamp
        conversation.UpdatedAt = DateTime.UtcNow;
        await chatRepository.UpdateConversationAsync(conversation, ct);
        await chatRepository.SaveChangesAsync(ct);

        return new SendMessageResponse
        {
            UserMessage = new ChatMessageResponse
            {
                Id = userMessage.Id,
                Role = userMessage.Role,
                Content = userMessage.Content,
                CreatedAt = userMessage.CreatedAt
            },
            AssistantMessage = new ChatMessageResponse
            {
                Id = assistantMessage.Id,
                Role = assistantMessage.Role,
                Content = assistantMessage.Content,
                CreatedAt = assistantMessage.CreatedAt
            }
        };
    }

    public async Task<bool> DeleteConversationAsync(Guid conversationId, Guid accountId, CancellationToken ct = default)
    {
        return await chatRepository.DeleteConversationAsync(conversationId, accountId, ct);
    }

    private async Task<string> BuildSystemPromptAsync(Guid accountId, TodayLocalContext? todayContext, CancellationToken ct)
    {
        var sb = new StringBuilder();

        sb.AppendLine("You are Bloomdo AI — a friendly, supportive personal productivity assistant inside the Bloomdo app.");
        sb.AppendLine("Bloomdo helps users manage screen time, build daily habits, block distracting apps, and track activity goals.");
        sb.AppendLine("You have access to the user's statistics, activities, and blocking rules.");
        sb.AppendLine("Provide actionable, encouraging advice. Be concise but insightful.");
        sb.AppendLine("If the user asks about their data, analyze it and provide personalized recommendations.");
        sb.AppendLine("Respond in the same language the user writes to you.");
        sb.AppendLine();

        // Gather recent stats
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var weekStart = today.AddDays(-(int)today.DayOfWeek + (int)DayOfWeek.Monday);
        if (today.DayOfWeek == DayOfWeek.Sunday) weekStart = weekStart.AddDays(-7);

        var snapshots = await statsRepository.GetSnapshotsForMonthAsync(accountId, weekStart, today, ct);
        if (snapshots.Count > 0)
        {
            sb.AppendLine("=== USER'S RECENT SCREEN TIME DATA ===");
            foreach (var snap in snapshots.OrderBy(s => s.Date))
            {
                var hours = snap.TotalScreenTimeSeconds / 3600;
                var mins = (snap.TotalScreenTimeSeconds % 3600) / 60;
                sb.AppendLine($"- {snap.Date:yyyy-MM-dd}: {hours}h {mins}m screen time, {snap.Pickups} pickups, goal met: {snap.GoalMet}");
            }
            sb.AppendLine();
        }

        // Today's live data from user's device (not yet synced to server)
        if (todayContext is not null)
        {
            var todayHours = todayContext.TotalScreenTimeSeconds / 3600;
            var todayMins = (todayContext.TotalScreenTimeSeconds % 3600) / 60;
            sb.AppendLine("=== TODAY'S LIVE DATA (from device) ===");
            sb.AppendLine($"- Screen time so far: {todayHours}h {todayMins}m");
            sb.AppendLine($"- Pickups so far: {todayContext.Pickups}");
            if (todayContext.TopApps.Count > 0)
            {
                sb.AppendLine("- Top apps today:");
                foreach (var app in todayContext.TopApps)
                {
                    var h = app.ForegroundSeconds / 3600;
                    var m = (app.ForegroundSeconds % 3600) / 60;
                    sb.AppendLine($"  - {app.AppName}: {h}h {m}m");
                }
            }
            sb.AppendLine();
        }

        // Top apps this week
        var usageRecords = await statsRepository.GetUsageRecordsForRangeAsync(accountId, weekStart, today, ct);
        if (usageRecords.Count > 0)
        {
            var topApps = usageRecords
                .GroupBy(r => r.PackageName)
                .Select(g => new { App = g.FirstOrDefault(r => !string.IsNullOrEmpty(r.AppLabel))?.AppLabel ?? g.Key, Seconds = g.Sum(r => r.ForegroundSeconds) })
                .OrderByDescending(a => a.Seconds)
                .Take(5);

            sb.AppendLine("=== TOP APPS THIS WEEK ===");
            foreach (var app in topApps)
            {
                var h = app.Seconds / 3600;
                var m = (app.Seconds % 3600) / 60;
                sb.AppendLine($"- {app.App}: {h}h {m}m");
            }
            sb.AppendLine();
        }

        // Activity groups & tasks
        try
        {
            var groups = await activityService.GetGroupsAsync(accountId, ct);
            if (groups.Count > 0)
            {
                sb.AppendLine("=== USER'S ACTIVITY GROUPS & TASKS ===");
                foreach (var group in groups)
                {
                    sb.AppendLine($"Group: {group.Icon} {group.Title}");
                    foreach (var item in group.Items)
                    {
                        sb.AppendLine($"  - {item.Title} (type: {item.TaskType})");
                    }
                }
                sb.AppendLine();
            }
        }
        catch
        {
            // Activities may not be available, that's ok
        }

        // Block rules
        try
        {
            var blockRules = await blockRuleRepository.FindAsync(b => !b.IsDeleted, ct);
            var userBlocks = blockRules.Where(b => b.AccountId == accountId).ToList();
            if (userBlocks.Count > 0)
            {
                sb.AppendLine("=== USER'S APP BLOCKING RULES ===");
                foreach (var rule in userBlocks)
                {
                    sb.AppendLine($"- Rule: {rule.Title} (type: {rule.Type}, active: {rule.IsActive})");
                }
                sb.AppendLine();
            }
        }
        catch
        {
            // Block rules may not be available
        }

        // Streaks
        var goalDates = await statsRepository.GetGoalMetDatesAsync(accountId, ct);
        if (goalDates.Count > 0)
        {
            var sorted = goalDates.OrderByDescending(d => d).ToList();
            var currentStreak = 0;
            var checkDate = today;
            foreach (var d in sorted)
            {
                if (d == checkDate)
                {
                    currentStreak++;
                    checkDate = checkDate.AddDays(-1);
                }
                else break;
            }
            sb.AppendLine($"=== STREAKS ===");
            sb.AppendLine($"Current streak: {currentStreak} days");
            sb.AppendLine($"Total goal-met days: {goalDates.Count}");
            sb.AppendLine();
        }

        return sb.ToString();
    }

    private async Task<string> CallGeminiAsync(string systemPrompt, List<ChatMessage> history, string newMessage, CancellationToken ct)
    {
        var contents = new List<Content>();

        foreach (var msg in history)
        {
            contents.Add(new Content
            {
                Role = msg.Role == "assistant" ? "model" : "user",
                Parts = [new Part { Text = msg.Content }]
            });
        }

        contents.Add(new Content
        {
            Role = "user",
            Parts = [new Part { Text = newMessage }]
        });

        var config = new GenerateContentConfig
        {
            SystemInstruction = new Content
            {
                Parts = [new Part { Text = systemPrompt }]
            },
            Temperature = 0.7f,
            MaxOutputTokens = 2048
        };

        var apiKeys = geminiSettings.ApiKeys;

        for (var i = 0; i < apiKeys.Count; i++)
        {
            var client = new Client(apiKey: apiKeys[i]);

            try
            {
                var response = await client.Models.GenerateContentAsync(
                    model: Model,
                    contents: contents,
                    config: config
                );

                return response.Candidates?.FirstOrDefault()?.Content?.Parts?.FirstOrDefault()?.Text
                       ?? "I'm sorry, I couldn't generate a response. Please try again.";
            }
            catch (ClientError) when (i < apiKeys.Count - 1)
            {
                // Key is inactive or rate-limited — try the next one
            }
        }

        return "⚠️ AI assistant is temporarily unavailable (all API keys exhausted). Please try again later.";
    }
}
