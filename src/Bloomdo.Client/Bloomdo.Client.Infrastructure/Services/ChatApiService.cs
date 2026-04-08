using System.Net;
using System.Net.Http.Json;
using Bloomdo.Client.Core.Interfaces;
using Bloomdo.Shared.Constants;
using Bloomdo.Shared.DTOs.Chat;

namespace Bloomdo.Client.Infrastructure.Services;

public class ChatApiService(HttpClient httpClient) : IChatApiService
{
    public async Task<List<ChatConversationResponse>?> GetConversationsAsync(CancellationToken ct = default)
    {
        try
        {
            var response = await httpClient.GetAsync(ApiRoutes.Chat.Conversations, ct);
            if (response.IsSuccessStatusCode)
                return await response.Content.ReadFromJsonAsync<List<ChatConversationResponse>>(ct);
            return null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"GetConversations failed: {ex.Message}");
            return null;
        }
    }

    public async Task<ChatConversationDetailResponse?> GetConversationAsync(Guid conversationId, CancellationToken ct = default)
    {
        try
        {
            var url = ApiRoutes.Chat.ConversationById.Replace("{id}", conversationId.ToString());
            var response = await httpClient.GetAsync(url, ct);
            if (response.IsSuccessStatusCode)
                return await response.Content.ReadFromJsonAsync<ChatConversationDetailResponse>(ct);
            return null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"GetConversation failed: {ex.Message}");
            return null;
        }
    }

    public async Task<SendMessageResponse?> CreateConversationAsync(string message, TodayLocalContext? todayContext = null, CancellationToken ct = default)
    {
        try
        {
            var request = new SendMessageRequest { Message = message, TodayContext = todayContext };
            var response = await httpClient.PostAsJsonAsync(ApiRoutes.Chat.Conversations, request, ct);
            if (response.IsSuccessStatusCode)
                return await response.Content.ReadFromJsonAsync<SendMessageResponse>(ct);
            if (response.StatusCode == HttpStatusCode.TooManyRequests)
                throw new HttpRequestException("Daily message limit reached", null, HttpStatusCode.TooManyRequests);
            return null;
        }
        catch (HttpRequestException) { throw; }
        catch (Exception ex)
        {
            Console.WriteLine($"CreateConversation failed: {ex.Message}");
            return null;
        }
    }

    public async Task<SendMessageResponse?> SendMessageAsync(Guid conversationId, string message, TodayLocalContext? todayContext = null, CancellationToken ct = default)
    {
        try
        {
            var url = ApiRoutes.Chat.Messages.Replace("{id}", conversationId.ToString());
            var request = new SendMessageRequest { Message = message, TodayContext = todayContext };
            var response = await httpClient.PostAsJsonAsync(url, request, ct);
            if (response.IsSuccessStatusCode)
                return await response.Content.ReadFromJsonAsync<SendMessageResponse>(ct);
            if (response.StatusCode == HttpStatusCode.TooManyRequests)
                throw new HttpRequestException("Daily message limit reached", null, HttpStatusCode.TooManyRequests);
            return null;
        }
        catch (HttpRequestException) { throw; }
        catch (Exception ex)
        {
            Console.WriteLine($"SendMessage failed: {ex.Message}");
            return null;
        }
    }

    public async Task<bool> DeleteConversationAsync(Guid conversationId, CancellationToken ct = default)
    {
        try
        {
            var url = ApiRoutes.Chat.ConversationById.Replace("{id}", conversationId.ToString());
            var response = await httpClient.DeleteAsync(url, ct);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"DeleteConversation failed: {ex.Message}");
            return false;
        }
    }
}
