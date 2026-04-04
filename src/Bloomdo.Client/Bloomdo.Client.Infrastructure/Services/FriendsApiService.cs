using Bloomdo.Client.Core.Interfaces;
using Bloomdo.Shared.DTOs.Friends;

namespace Bloomdo.Client.Infrastructure.Services;

public class FriendsApiService(HttpClient httpClient) : IFriendsApiService
{
    public Task<List<ProfileSummaryDto>> SearchUsersAsync(string query) => Task.FromResult(new List<ProfileSummaryDto>());
    public Task<List<FriendshipDto>> GetFriendsAsync() => Task.FromResult(new List<FriendshipDto>());
    public Task<bool> SendFriendRequestAsync(Guid friendId) => Task.FromResult(false);
    public Task<bool> RespondToRequestAsync(Guid friendshipId, bool accept) => Task.FromResult(false);
    public Task<bool> RemoveFriendAsync(Guid friendId) => Task.FromResult(false);
}
