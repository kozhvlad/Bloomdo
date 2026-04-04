using Bloomdo.Client.Core.Interfaces;
using Bloomdo.Shared.DTOs.Friends;
using Bloomdo.Shared.DTOs.Social;
using Microsoft.AspNetCore.SignalR.Client;

namespace Bloomdo.Client.Infrastructure.Services;

public class SignalRClientService : ISignalRClientService, IAsyncDisposable
{
    private HubConnection? _connection;
    private readonly string _hubUrl;

    public bool IsConnected => _connection?.State == HubConnectionState.Connected;

    public event Action<ProfileSummaryDto>? NewFollowerReceived;
    public event Action<SharedGroupDto, ProfileSummaryDto>? GroupInviteReceived;
    public event Action<Guid>? GroupDeletedReceived;
    public event Action<Guid, ProfileSummaryDto>? NewGroupMemberReceived;
    public event Action<Guid, Guid>? TaskCompletedReceived;
    public event Action<Guid>? NewGroupTaskReceived;

    public SignalRClientService(string apiBaseUrl)
    {
        _hubUrl = apiBaseUrl.TrimEnd('/') + "/hubs/social";
    }

    public async Task ConnectAsync(string token, CancellationToken ct = default)
    {
        if (_connection != null)
            await DisconnectAsync();

        _connection = new HubConnectionBuilder()
            .WithUrl(_hubUrl, options =>
            {
                options.AccessTokenProvider = () => Task.FromResult<string?>(token);
#if DEBUG
                options.HttpMessageHandlerFactory = _ =>
                {
                    var handler = new HttpClientHandler();
                    handler.ServerCertificateCustomValidationCallback = (_, _, _, _) => true;
                    return handler;
                };
#endif
            })
            .WithAutomaticReconnect()
            .Build();

        RegisterHandlers();

        await _connection.StartAsync(ct);
    }

    public async Task DisconnectAsync()
    {
        if (_connection != null)
        {
            await _connection.DisposeAsync();
            _connection = null;
        }
    }

    public Task JoinGroupAsync(Guid groupId)
        => _connection?.InvokeAsync("JoinGroup", groupId.ToString()) ?? Task.CompletedTask;

    public Task LeaveGroupAsync(Guid groupId)
        => _connection?.InvokeAsync("LeaveGroup", groupId.ToString()) ?? Task.CompletedTask;

    private void RegisterHandlers()
    {
        if (_connection == null) return;

        _connection.On<ProfileSummaryDto>("ReceiveNewFollower",
            follower => NewFollowerReceived?.Invoke(follower));

        _connection.On<SharedGroupDto, ProfileSummaryDto>("ReceiveGroupInvite",
            (group, inviter) => GroupInviteReceived?.Invoke(group, inviter));

        _connection.On<Guid>("ReceiveGroupDeleted",
            groupId => GroupDeletedReceived?.Invoke(groupId));

        _connection.On<Guid, ProfileSummaryDto>("ReceiveNewGroupMember",
            (groupId, member) => NewGroupMemberReceived?.Invoke(groupId, member));

        _connection.On<Guid, Guid>("ReceiveTaskCompleted",
            (actorId, itemId) => TaskCompletedReceived?.Invoke(actorId, itemId));

        _connection.On<Guid>("ReceiveNewGroupTask",
            itemId => NewGroupTaskReceived?.Invoke(itemId));
    }

    public async ValueTask DisposeAsync()
    {
        await DisconnectAsync();
    }
}
