using System.Collections.ObjectModel;
using Bloomdo.Client.Application.ViewModels.Items;
using Bloomdo.Client.Core.Interfaces;
using Bloomdo.Shared.DTOs.Chat;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Bloomdo.Client.Application.ViewModels.MainComponents;

public partial class AiChatViewModel : PageViewModel
{
    private readonly IChatApiService _chatApiService;
    private readonly ISubscriptionApiService? _subscriptionApiService;

    private Guid? _currentConversationId;

    [ObservableProperty]
    private string _messageText = string.Empty;

    [ObservableProperty]
    private bool _isSending;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private bool _isHistoryVisible;

    [ObservableProperty]
    private ObservableCollection<ChatMessageItemViewModel> _messages = [];

    [ObservableProperty]
    private ObservableCollection<ChatConversationItemViewModel> _conversations = [];

    [ObservableProperty]
    private string _currentTitle = "New Chat";

    [ObservableProperty]
    private int _remainingMessages = -1;

    [ObservableProperty]
    private int _maxDailyMessages;

    [ObservableProperty]
    private bool _isLimitReached;

    public bool HasLimit => RemainingMessages >= 0;

    public Func<string, Task>? CopyToClipboardFunc { get; set; }

    public AiChatViewModel(IChatApiService chatApiService, ISubscriptionApiService? subscriptionApiService = null)
    {
        _chatApiService = chatApiService;
        _subscriptionApiService = subscriptionApiService;
    }

    public override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadSubscriptionLimitsAsync();
        await LoadConversationsAsync();
    }

    [RelayCommand]
    private void ToggleHistory()
    {
        IsHistoryVisible = !IsHistoryVisible;
    }

    [RelayCommand]
    private void NewChat()
    {
        _currentConversationId = null;
        Messages.Clear();
        CurrentTitle = "New Chat";
        MessageText = string.Empty;
        IsHistoryVisible = false;
    }

    [RelayCommand]
    private async Task SelectConversation(ChatConversationItemViewModel? item)
    {
        if (item is null) return;

        IsHistoryVisible = false;
        IsLoading = true;

        try
        {
            var detail = await _chatApiService.GetConversationAsync(item.Id);
            if (detail is not null)
            {
                _currentConversationId = detail.Id;
                CurrentTitle = detail.Title;
                Messages.Clear();

                foreach (var msg in detail.Messages)
                {
                    Messages.Add(new ChatMessageItemViewModel
                    {
                        Id = msg.Id,
                        Role = msg.Role,
                        Content = msg.Content,
                        CreatedAt = msg.CreatedAt
                    });
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"SelectConversation failed: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task DeleteConversation(ChatConversationItemViewModel? item)
    {
        if (item is null) return;

        var success = await _chatApiService.DeleteConversationAsync(item.Id);
        if (success)
        {
            Conversations.Remove(item);
            if (_currentConversationId == item.Id)
            {
                NewChat();
            }
        }
    }

    [RelayCommand(CanExecute = nameof(CanSendMessage))]
    private async Task SendMessage()
    {
        var text = MessageText.Trim();
        if (string.IsNullOrWhiteSpace(text)) return;

        // Add user message optimistically
        var userMsg = new ChatMessageItemViewModel
        {
            Role = "user",
            Content = text,
            CreatedAt = DateTime.UtcNow
        };
        Messages.Add(userMsg);
        MessageText = string.Empty;

        IsSending = true;

        try
        {
            SendMessageResponse? response;

            if (_currentConversationId.HasValue)
            {
                response = await _chatApiService.SendMessageAsync(_currentConversationId.Value, text);
            }
            else
            {
                response = await _chatApiService.CreateConversationAsync(text);
            }

            if (response is not null)
            {
                // Update the user message with server id
                userMsg.Id = response.UserMessage.Id;
                userMsg.CreatedAt = response.UserMessage.CreatedAt;

                // Add assistant response
                Messages.Add(new ChatMessageItemViewModel
                {
                    Id = response.AssistantMessage.Id,
                    Role = response.AssistantMessage.Role,
                    Content = response.AssistantMessage.Content,
                    CreatedAt = response.AssistantMessage.CreatedAt
                });

                // Decrement remaining messages counter
                if (RemainingMessages > 0)
                {
                    RemainingMessages--;
                    OnPropertyChanged(nameof(HasLimit));
                    if (RemainingMessages <= 0)
                        IsLimitReached = true;
                }

                // If this was a new conversation, update conversation id and title
                if (!_currentConversationId.HasValue)
                {
                    // Reload conversations to get the new one
                    await LoadConversationsAsync();
                    if (Conversations.Count > 0)
                    {
                        _currentConversationId = Conversations[0].Id;
                        CurrentTitle = Conversations[0].Title;
                    }
                }
            }
            else
            {
                // Show error as assistant message
                Messages.Add(new ChatMessageItemViewModel
                {
                    Role = "assistant",
                    Content = "Sorry, something went wrong. Please try again.",
                    CreatedAt = DateTime.UtcNow
                });
            }
        }
        catch (HttpRequestException httpEx) when (httpEx.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
        {
            RemainingMessages = 0;
            IsLimitReached = true;
            OnPropertyChanged(nameof(HasLimit));
            Messages.Add(new ChatMessageItemViewModel
            {
                Role = "assistant",
                Content = "You've reached your daily message limit. Upgrade to Bloomdo Plus for unlimited messages!",
                CreatedAt = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"SendMessage failed: {ex.Message}");
            Messages.Add(new ChatMessageItemViewModel
            {
                Role = "assistant",
                Content = "Connection error. Please check your network and try again.",
                CreatedAt = DateTime.UtcNow
            });
        }
        finally
        {
            IsSending = false;
        }
    }

    private bool CanSendMessage() => !IsSending && !IsLimitReached && !string.IsNullOrWhiteSpace(MessageText);

    partial void OnMessageTextChanged(string value)
    {
        SendMessageCommand.NotifyCanExecuteChanged();
    }

    partial void OnIsSendingChanged(bool value)
    {
        SendMessageCommand.NotifyCanExecuteChanged();
    }

    partial void OnIsLimitReachedChanged(bool value)
    {
        SendMessageCommand.NotifyCanExecuteChanged();
    }

    private async Task LoadSubscriptionLimitsAsync()
    {
        if (_subscriptionApiService is null) return;

        try
        {
            var status = await _subscriptionApiService.GetStatusAsync();
                if (status is null) return;

                if (status.IsPremium)
                {
                    IsLimitReached = false;
                    return;
                }

                if (status.Limits is not null)
                {
                    MaxDailyMessages = status.Limits.MaxDailyChatMessages;
                    RemainingMessages = status.Limits.RemainingChatMessagesToday;
                    IsLimitReached = RemainingMessages <= 0;
                    OnPropertyChanged(nameof(HasLimit));
                }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"LoadSubscriptionLimits failed: {ex.Message}");
        }
    }

    private async Task LoadConversationsAsync()
    {
        try
        {
            var result = await _chatApiService.GetConversationsAsync();
            if (result is not null)
            {
                Conversations.Clear();
                foreach (var conv in result)
                {
                    Conversations.Add(new ChatConversationItemViewModel
                    {
                        Id = conv.Id,
                        Title = conv.Title,
                        LastMessagePreview = conv.LastMessage?.Content.Length > 60
                            ? conv.LastMessage.Content[..60] + "..."
                            : conv.LastMessage?.Content ?? string.Empty,
                        UpdatedAt = conv.UpdatedAt ?? conv.CreatedAt
                    });
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"LoadConversations failed: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task CopyMessage(ChatMessageItemViewModel? item)
    {
        if (item is null || CopyToClipboardFunc is null) return;

        try
        {
            await CopyToClipboardFunc(item.Content);
            item.IsCopied = true;
            _ = ResetCopyStateAsync(item);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"CopyMessage failed: {ex.Message}");
        }
    }

    private static async Task ResetCopyStateAsync(ChatMessageItemViewModel item)
    {
        await Task.Delay(2000);
        item.IsCopied = false;
    }
}
