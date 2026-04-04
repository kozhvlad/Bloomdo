using Bloomdo.Server.Application.Interfaces;
using Bloomdo.Server.Domain.Entities;
using Bloomdo.Server.Infrastructure.Data;
using Bloomdo.Shared.DTOs.Activities;
using Bloomdo.Shared.DTOs.Friends;
using Bloomdo.Shared.DTOs.Social;
using Bloomdo.Shared.Enums;
using Microsoft.EntityFrameworkCore;

namespace Bloomdo.Server.Infrastructure.Services;

public class SocialService(
    AppDbContext db,
    IRepository<Account> accountRepo,
    IRepository<Friendship> friendshipRepo,
    IRepository<Notification> notificationRepo,
    ISocialRealTimeNotifier notifier) : ISocialService
{
    // ─── Search ───────────────────────────────────────────────────────────────

    public async Task<List<FollowStatusDto>> SearchUsersAsync(Guid currentAccountId, string query, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(query)) return [];

        var normalized = query.ToLower();
        var users = await accountRepo.FindAsync(a =>
            a.Id != currentAccountId &&
            ((a.Username != null && a.Username.ToLower().Contains(normalized)) ||
             a.Email.ToLower().Contains(normalized) ||
             (a.FirstName + " " + a.LastName).ToLower().Contains(normalized)), ct);

        var myFriendships = await friendshipRepo.FindAsync(f =>
            f.RequesterId == currentAccountId || f.AddresseeId == currentAccountId, ct);

        return users.Select(u => BuildFollowStatus(currentAccountId, u, myFriendships)).ToList();
    }

    // ─── Followers / Following ────────────────────────────────────────────────

    public async Task<List<FollowStatusDto>> GetFollowersAsync(Guid accountId, CancellationToken ct = default)
    {
        // People who follow me: they are Requester, I am Addressee, Accepted
        var friendships = await friendshipRepo.FindAsync(f =>
            f.AddresseeId == accountId && f.Status == FriendshipStatus.Accepted, ct);

        var myFriendships = await friendshipRepo.FindAsync(f =>
            f.RequesterId == accountId || f.AddresseeId == accountId, ct);

        var result = new List<FollowStatusDto>();
        foreach (var f in friendships)
        {
            var follower = await accountRepo.GetByIdAsync(f.RequesterId, ct);
            if (follower == null) continue;
            result.Add(BuildFollowStatus(accountId, follower, myFriendships));
        }
        return result;
    }

    public async Task<List<FollowStatusDto>> GetFollowingAsync(Guid accountId, CancellationToken ct = default)
    {
        // People I follow: I am Requester, they are Addressee, Accepted
        var friendships = await friendshipRepo.FindAsync(f =>
            f.RequesterId == accountId && f.Status == FriendshipStatus.Accepted, ct);

        var myFriendships = await friendshipRepo.FindAsync(f =>
            f.RequesterId == accountId || f.AddresseeId == accountId, ct);

        var result = new List<FollowStatusDto>();
        foreach (var f in friendships)
        {
            var target = await accountRepo.GetByIdAsync(f.AddresseeId, ct);
            if (target == null) continue;
            result.Add(BuildFollowStatus(accountId, target, myFriendships));
        }
        return result;
    }

    public async Task<List<ProfileSummaryDto>> GetMutualFollowersAsync(Guid accountId, CancellationToken ct = default)
    {
        // I follow them AND they follow me
        var iFollow = await friendshipRepo.FindAsync(f =>
            f.RequesterId == accountId && f.Status == FriendshipStatus.Accepted, ct);
        var iFollowIds = iFollow.Select(f => f.AddresseeId).ToHashSet();

        var followMe = await friendshipRepo.FindAsync(f =>
            f.AddresseeId == accountId && f.Status == FriendshipStatus.Accepted, ct);
        var mutualIds = followMe.Select(f => f.RequesterId).Where(iFollowIds.Contains).ToList();

        var result = new List<ProfileSummaryDto>();
        foreach (var id in mutualIds)
        {
            var account = await accountRepo.GetByIdAsync(id, ct);
            if (account != null) result.Add(MapToProfileSummary(account));
        }
        return result;
    }

    public async Task<bool> FollowUserAsync(Guid followerId, Guid targetId, CancellationToken ct = default)
    {
        if (followerId == targetId) return false;

        var existing = await friendshipRepo.FirstOrDefaultAsync(f =>
            f.RequesterId == followerId && f.AddresseeId == targetId, ct);
        if (existing != null) return false;

        var target = await accountRepo.GetByIdAsync(targetId, ct);
        if (target == null) return false;

        var status = target.IsPrivateProfile ? FriendshipStatus.Pending : FriendshipStatus.Accepted;
        var friendship = new Friendship { RequesterId = followerId, AddresseeId = targetId, Status = status };
        await friendshipRepo.AddAsync(friendship, ct);

        // Notification
        var notifType = target.IsPrivateProfile ? NotificationType.FollowRequest : NotificationType.NewFollower;
        var notification = new Notification
        {
            RecipientId = targetId,
            ActorId = followerId,
            Type = notifType,
            ReferenceId = friendship.Id
        };
        await notificationRepo.AddAsync(notification, ct);

        // Push via SignalR
        var follower = await accountRepo.GetByIdAsync(followerId, ct);
        if (follower != null)
            await notifier.SendNewFollowerAsync(targetId, MapToProfileSummary(follower), ct);

        return true;
    }

    public async Task<bool> UnfollowUserAsync(Guid followerId, Guid targetId, CancellationToken ct = default)
    {
        var friendship = await friendshipRepo.FirstOrDefaultAsync(f =>
            f.RequesterId == followerId && f.AddresseeId == targetId, ct);
        if (friendship == null) return false;

        await friendshipRepo.DeleteAsync(friendship, ct);
        return true;
    }

    // ─── Follow Requests ──────────────────────────────────────────────────────

    public async Task<List<FollowStatusDto>> GetPendingFollowRequestsAsync(Guid accountId, CancellationToken ct = default)
    {
        var pending = await friendshipRepo.FindAsync(f =>
            f.AddresseeId == accountId && f.Status == FriendshipStatus.Pending, ct);

        var result = new List<FollowStatusDto>();
        foreach (var f in pending)
        {
            var requester = await accountRepo.GetByIdAsync(f.RequesterId, ct);
            if (requester == null) continue;
            result.Add(new FollowStatusDto
            {
                User = MapToProfileSummary(requester),
                IsFollower = true,
                IsFollowing = false,
                FollowId = f.Id,
                IsPending = true,
                IsPrivateProfile = requester.IsPrivateProfile
            });
        }
        return result;
    }

    public async Task<bool> RespondToFollowRequestAsync(Guid accountId, Guid followshipId, bool accept, CancellationToken ct = default)
    {
        var friendship = await friendshipRepo.GetByIdAsync(followshipId, ct);
        if (friendship == null || friendship.AddresseeId != accountId || friendship.Status != FriendshipStatus.Pending)
            return false;

        if (accept)
        {
            friendship.Status = FriendshipStatus.Accepted;
            await friendshipRepo.UpdateAsync(friendship, ct);

            // Notify follower that request was accepted
            var notification = new Notification
            {
                RecipientId = friendship.RequesterId,
                ActorId = accountId,
                Type = NotificationType.NewFollower,
                ReferenceId = friendship.Id
            };
            await notificationRepo.AddAsync(notification, ct);
        }
        else
        {
            await friendshipRepo.DeleteAsync(friendship, ct);
        }

        return true;
    }

    // ─── Notifications ────────────────────────────────────────────────────────

    public async Task<List<NotificationDto>> GetNotificationsAsync(Guid accountId, CancellationToken ct = default)
    {
        var notifications = await notificationRepo.FindAsync(n => n.RecipientId == accountId, ct);
        var result = new List<NotificationDto>();

        foreach (var n in notifications.OrderByDescending(x => x.CreatedAt).Take(50))
        {
            Account? actor = null;
            if (n.ActorId.HasValue)
                actor = await accountRepo.GetByIdAsync(n.ActorId.Value, ct);

            result.Add(new NotificationDto
            {
                Id = n.Id,
                Type = n.Type,
                Actor = actor != null ? MapToProfileSummary(actor) : null,
                ReferenceId = n.ReferenceId,
                IsRead = n.IsRead,
                CreatedAt = n.CreatedAt
            });
        }
        return result;
    }

    public async Task<bool> MarkNotificationReadAsync(Guid accountId, Guid notificationId, CancellationToken ct = default)
    {
        var notification = await notificationRepo.GetByIdAsync(notificationId, ct);
        if (notification == null || notification.RecipientId != accountId) return false;

        notification.IsRead = true;
        await notificationRepo.UpdateAsync(notification, ct);
        return true;
    }

    // ─── Shared Groups ────────────────────────────────────────────────────────

    public async Task<List<SharedGroupDto>> GetMySharedGroupsAsync(Guid accountId, CancellationToken ct = default)
    {
        var memberships = await db.GroupMemberships
            .Include(m => m.Group)
                .ThenInclude(g => g.Items)
            .Include(m => m.Group)
                .ThenInclude(g => g.Memberships)
                    .ThenInclude(gm => gm.Account)
            .Where(m => m.AccountId == accountId && m.Status == GroupMemberStatus.Accepted)
            .ToListAsync(ct);

        return memberships.Select(m =>
        {
            var group = m.Group;
            var ownerMembership = group.Memberships.FirstOrDefault(gm => gm.Role == GroupMemberRole.Owner);
            return new SharedGroupDto
            {
                Id = group.Id,
                Title = group.Title,
                Icon = group.Icon,
                Color = group.Color,
                MembersCount = group.Memberships.Count(gm => gm.Status == GroupMemberStatus.Accepted),
                TasksCount = group.Items.Count(i => i.IsActive),
                MemberAvatars = group.Memberships
                    .Where(gm => gm.Status == GroupMemberStatus.Accepted)
                    .Take(4)
                    .Select(gm => MapToProfileSummary(gm.Account))
                    .ToList(),
                IsOwner = ownerMembership?.AccountId == accountId,
                MyStatus = m.Status
            };
        }).ToList();
    }

    public async Task<SharedGroupDetailDto?> GetSharedGroupDetailAsync(Guid accountId, Guid groupId, CancellationToken ct = default)
    {
        var myMembership = await db.GroupMemberships
            .FirstOrDefaultAsync(m => m.ActivityGroupId == groupId && m.AccountId == accountId && m.Status == GroupMemberStatus.Accepted, ct);
        if (myMembership == null) return null;

        var group = await db.ActivityGroups
            .Include(g => g.Items.Where(i => i.IsActive))
            .Include(g => g.Memberships.Where(m => m.Status == GroupMemberStatus.Accepted))
                .ThenInclude(m => m.Account)
            .FirstOrDefaultAsync(g => g.Id == groupId, ct);
        if (group == null) return null;

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var memberIds = group.Memberships.Select(m => m.AccountId).ToList();
        var itemIds = group.Items.Select(i => i.Id).ToList();

        var completions = await db.ActivityCompletions
            .Where(c => itemIds.Contains(c.ActivityItemId) && memberIds.Contains(c.AccountId) && c.Date == today)
            .ToListAsync(ct);

        var items = group.Items.Select(item =>
        {
            var myCompletion = completions.FirstOrDefault(c => c.ActivityItemId == item.Id && c.AccountId == accountId);
            return new DailyActivityItemDto
            {
                Id = item.Id,
                Title = item.Title,
                Description = item.Description,
                TaskType = (ActivityItemType)item.TaskType,
                DurationMinutes = item.DurationMinutes,
                TargetCount = item.TargetCount,
                CurrentCount = myCompletion?.CountValue ?? 0,
                Icon = item.Icon ?? "✨",
                Color = item.Color ?? "#7E57C2",
                CurrentStreak = 0,
                IsCompleted = myCompletion != null,
                CompletedAtUtc = myCompletion?.CreatedAt,
                VerificationTemplate = item.VerificationTemplateId.HasValue
                    ? (VerificationTemplate?)item.VerificationTemplateId.Value
                    : null,
                CustomVerificationCriteria = item.CustomVerificationCriteria
            };
        }).ToList();

        var memberProgresses = group.Memberships.Select(m =>
        {
            var memberCompletions = completions.Where(c => c.AccountId == m.AccountId).ToList();
            return new GroupMemberProgressDto
            {
                Account = MapToProfileSummary(m.Account),
                CompletedItems = memberCompletions.Count,
                TotalItems = group.Items.Count
            };
        }).ToList();

        var ownerMembership = group.Memberships.FirstOrDefault(m => m.Role == GroupMemberRole.Owner);

        return new SharedGroupDetailDto
        {
            Id = group.Id,
            Title = group.Title,
            Icon = group.Icon,
            Color = group.Color,
            IsOwner = ownerMembership?.AccountId == accountId,
            Members = group.Memberships.Select(m => new GroupMembershipDto
            {
                Id = m.Id,
                ActivityGroupId = m.ActivityGroupId,
                Account = MapToProfileSummary(m.Account),
                Role = m.Role,
                Status = m.Status
            }).ToList(),
            Items = items,
            MemberProgresses = memberProgresses
        };
    }

    public async Task<SharedGroupDto?> CreateSharedGroupAsync(Guid accountId, string title, string icon, string color, CancellationToken ct = default)
    {
        var group = new ActivityGroup
        {
            AccountId = accountId,
            Title = title,
            Icon = icon,
            Color = color
        };
        db.ActivityGroups.Add(group);

        var ownerMembership = new GroupMembership
        {
            ActivityGroupId = group.Id,
            AccountId = accountId,
            Role = GroupMemberRole.Owner,
            Status = GroupMemberStatus.Accepted
        };
        db.GroupMemberships.Add(ownerMembership);
        await db.SaveChangesAsync(ct);

        return new SharedGroupDto
        {
            Id = group.Id,
            Title = group.Title,
            Icon = group.Icon,
            Color = group.Color,
            MembersCount = 1,
            TasksCount = 0,
            MemberAvatars = [],
            IsOwner = true,
            MyStatus = GroupMemberStatus.Accepted
        };
    }

    public async Task<SharedGroupDto?> UpdateSharedGroupAsync(Guid accountId, Guid groupId, UpdateSharedGroupRequest request, CancellationToken ct = default)
    {
        var membership = await db.GroupMemberships.FirstOrDefaultAsync(m =>
            m.ActivityGroupId == groupId && m.AccountId == accountId && m.Role == GroupMemberRole.Owner, ct);
        if (membership == null) return null;

        var group = await db.ActivityGroups
            .Include(g => g.Items)
            .Include(g => g.Memberships.Where(m => m.Status == GroupMemberStatus.Accepted))
                .ThenInclude(m => m.Account)
            .FirstOrDefaultAsync(g => g.Id == groupId && !g.IsDeleted, ct);
        if (group == null) return null;

        if (request.Title is not null) group.Title = request.Title;
        if (request.Icon is not null) group.Icon = request.Icon;
        if (request.Color is not null) group.Color = request.Color;

        await db.SaveChangesAsync(ct);

        return new SharedGroupDto
        {
            Id = group.Id,
            Title = group.Title,
            Icon = group.Icon,
            Color = group.Color,
            MembersCount = group.Memberships.Count(gm => gm.Status == GroupMemberStatus.Accepted),
            TasksCount = group.Items.Count(i => i.IsActive),
            MemberAvatars = group.Memberships
                .Where(gm => gm.Status == GroupMemberStatus.Accepted)
                .Take(4)
                .Select(gm => MapToProfileSummary(gm.Account))
                .ToList(),
            IsOwner = true,
            MyStatus = GroupMemberStatus.Accepted
        };
    }

    public async Task<bool> DeleteSharedGroupAsync(Guid accountId, Guid groupId, CancellationToken ct = default)
    {
        var membership = await db.GroupMemberships.FirstOrDefaultAsync(m =>
            m.ActivityGroupId == groupId && m.AccountId == accountId && m.Role == GroupMemberRole.Owner, ct);
        if (membership == null) return false;

        var group = await db.ActivityGroups.FirstOrDefaultAsync(g => g.Id == groupId, ct);
        if (group == null) return false;

        // Notify all members
        var memberIds = await db.GroupMemberships
            .Where(m => m.ActivityGroupId == groupId && m.Status == GroupMemberStatus.Accepted && m.AccountId != accountId)
            .Select(m => m.AccountId)
            .ToListAsync(ct);

        foreach (var memberId in memberIds)
            await notifier.SendGroupDeletedAsync(memberId, groupId, ct);

        group.IsDeleted = true;
        group.DeletedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> InviteToGroupAsync(Guid adminId, Guid groupId, Guid inviteeId, CancellationToken ct = default)
    {
        var adminMembership = await db.GroupMemberships.FirstOrDefaultAsync(m =>
            m.ActivityGroupId == groupId && m.AccountId == adminId && m.Role == GroupMemberRole.Owner, ct);
        if (adminMembership == null) return false;

        // Must be a mutual follower
        var iFollow = await friendshipRepo.FirstOrDefaultAsync(f =>
            f.RequesterId == adminId && f.AddresseeId == inviteeId && f.Status == FriendshipStatus.Accepted, ct);
        var theyFollow = await friendshipRepo.FirstOrDefaultAsync(f =>
            f.RequesterId == inviteeId && f.AddresseeId == adminId && f.Status == FriendshipStatus.Accepted, ct);
        if (iFollow == null || theyFollow == null) return false;

        // Not already in group
        var existing = await db.GroupMemberships.FirstOrDefaultAsync(m =>
            m.ActivityGroupId == groupId && m.AccountId == inviteeId, ct);
        if (existing != null) return false;

        var membership = new GroupMembership
        {
            ActivityGroupId = groupId,
            AccountId = inviteeId,
            Role = GroupMemberRole.Member,
            Status = GroupMemberStatus.Pending
        };
        db.GroupMemberships.Add(membership);

        var notification = new Notification
        {
            RecipientId = inviteeId,
            ActorId = adminId,
            Type = NotificationType.GroupInvite,
            ReferenceId = groupId
        };
        db.Notifications.Add(notification);

        await db.SaveChangesAsync(ct);

        // SignalR push
        var group = await db.ActivityGroups.FirstOrDefaultAsync(g => g.Id == groupId, ct);
        var admin = await accountRepo.GetByIdAsync(adminId, ct);
        if (group != null && admin != null)
        {
            await notifier.SendGroupInviteAsync(inviteeId,
                new SharedGroupDto
                {
                    Id = group.Id, Title = group.Title, Icon = group.Icon, Color = group.Color,
                    IsOwner = false, MyStatus = GroupMemberStatus.Pending
                },
                MapToProfileSummary(admin), ct);
        }

        return true;
    }

    public async Task<bool> RespondToGroupInviteAsync(Guid accountId, Guid groupId, bool accept, CancellationToken ct = default)
    {
        var membership = await db.GroupMemberships.FirstOrDefaultAsync(m =>
            m.ActivityGroupId == groupId && m.AccountId == accountId && m.Status == GroupMemberStatus.Pending, ct);
        if (membership == null) return false;

        if (accept)
        {
            membership.Status = GroupMemberStatus.Accepted;
            await db.SaveChangesAsync(ct);

            // Notify group owner
            var owner = await db.GroupMemberships.FirstOrDefaultAsync(m =>
                m.ActivityGroupId == groupId && m.Role == GroupMemberRole.Owner, ct);
            if (owner != null)
            {
                var joiner = await accountRepo.GetByIdAsync(accountId, ct);
                if (joiner != null)
                    await notifier.SendNewGroupMemberAsync(owner.AccountId, groupId, MapToProfileSummary(joiner), ct);
            }
        }
        else
        {
            membership.IsDeleted = true;
            membership.DeletedAt = DateTime.UtcNow;
            await db.SaveChangesAsync(ct);
        }

        return true;
    }

    public async Task<bool> RemoveMemberAsync(Guid adminId, Guid groupId, Guid memberId, CancellationToken ct = default)
    {
        var adminMembership = await db.GroupMemberships.FirstOrDefaultAsync(m =>
            m.ActivityGroupId == groupId && m.AccountId == adminId && m.Role == GroupMemberRole.Owner, ct);
        if (adminMembership == null) return false;

        var memberMembership = await db.GroupMemberships.FirstOrDefaultAsync(m =>
            m.ActivityGroupId == groupId && m.AccountId == memberId, ct);
        if (memberMembership == null) return false;

        memberMembership.IsDeleted = true;
        memberMembership.DeletedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);

        await notifier.SendGroupDeletedAsync(memberId, groupId, ct);
        return true;
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    private static FollowStatusDto BuildFollowStatus(Guid currentAccountId, Account user, IEnumerable<Friendship> myFriendships)
    {
        var iFollow = myFriendships.FirstOrDefault(f =>
            f.RequesterId == currentAccountId && f.AddresseeId == user.Id);
        var theyFollow = myFriendships.FirstOrDefault(f =>
            f.RequesterId == user.Id && f.AddresseeId == currentAccountId);

        return new FollowStatusDto
        {
            User = MapToProfileSummary(user),
            IsFollowing = iFollow?.Status == FriendshipStatus.Accepted,
            IsFollower = theyFollow?.Status == FriendshipStatus.Accepted,
            FollowId = iFollow?.Id,
            IsPending = iFollow?.Status == FriendshipStatus.Pending,
            IsPrivateProfile = user.IsPrivateProfile
        };
    }

    private static ProfileSummaryDto MapToProfileSummary(Account account) => new()
    {
        Id = account.Id,
        Username = account.Username ?? string.Empty,
        FirstName = account.FirstName,
        LastName = account.LastName,
        AvatarJson = account.AvatarJson
    };
}
