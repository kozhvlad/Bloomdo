using Bloomdo.Server.Application.Interfaces;
using Bloomdo.Server.Domain.Entities;
using Bloomdo.Shared.DTOs.Activities;
using Bloomdo.Shared.Enums;

namespace Bloomdo.Server.Application.Services;

public class DailyActivityService(
    IRepository<ActivityGroup> groupRepo,
    IRepository<ActivityItem> itemRepo,
    IRepository<ActivityCompletion> completionRepo,
    IRepository<GroupMembership> membershipRepo,
    IVisionService visionService,
    IStatsService statsService) : IDailyActivityService
{
    public async Task<List<ActivityGroupResponse>> GetGroupsAsync(Guid accountId, CancellationToken ct = default)
    {
        // Get owned groups
        var ownedGroups = await groupRepo.FindAsync(g => g.AccountId == accountId && !g.IsDeleted, ct);
        
        // Get groups where user is a member
        var memberships = await membershipRepo.FindAsync(m => m.AccountId == accountId && m.Status == GroupMemberStatus.Accepted, ct);
        var sharedGroupIds = memberships.Select(m => m.ActivityGroupId).ToList();
        var sharedGroups = await groupRepo.FindAsync(g => sharedGroupIds.Contains(g.Id) && !g.IsDeleted, ct);

        var allGroups = ownedGroups.Concat(sharedGroups.Where(sg => sg.AccountId != accountId)).OrderBy(g => g.SortOrder).ToList();

        var result = new List<ActivityGroupResponse>();
        foreach (var group in allGroups)
        {
            var items = (await itemRepo.FindAsync(i => i.ActivityGroupId == group.Id, ct))
                .OrderBy(i => i.SortOrder)
                .ToList();

            result.Add(MapGroupResponse(group, items));
        }

        return result;
    }

    public async Task<ActivityGroupResponse> CreateGroupAsync(Guid accountId, CreateActivityGroupRequest request, CancellationToken ct = default)
    {
        var existingGroups = await groupRepo.FindAsync(g => g.AccountId == accountId, ct);
        var maxOrder = existingGroups.Any() ? existingGroups.Max(g => g.SortOrder) : 0;

        var group = new ActivityGroup
        {
            AccountId = accountId,
            Title = request.Title,
            Icon = request.Icon,
            Color = request.Color,
            SortOrder = maxOrder + 1,
            IsActive = true
        };

        await groupRepo.AddAsync(group, ct);
        return MapGroupResponse(group, []);
    }

    public async Task<ActivityGroupResponse?> UpdateGroupAsync(Guid accountId, Guid groupId, UpdateActivityGroupRequest request, CancellationToken ct = default)
    {
        var group = await groupRepo.FirstOrDefaultAsync(g => g.Id == groupId && g.AccountId == accountId, ct);
        if (group is null) return null;

        if (request.Title is not null) group.Title = request.Title;
        if (request.Icon is not null) group.Icon = request.Icon;
        if (request.Color is not null) group.Color = request.Color;
        if (request.SortOrder.HasValue) group.SortOrder = request.SortOrder.Value;
        if (request.IsActive.HasValue) group.IsActive = request.IsActive.Value;

        await groupRepo.UpdateAsync(group, ct);

        var items = (await itemRepo.FindAsync(i => i.ActivityGroupId == group.Id, ct))
            .OrderBy(i => i.SortOrder)
            .ToList();

        return MapGroupResponse(group, items);
    }

    public async Task<bool> DeleteGroupAsync(Guid accountId, Guid groupId, CancellationToken ct = default)
    {
        var group = await groupRepo.FirstOrDefaultAsync(g => g.Id == groupId && g.AccountId == accountId, ct);
        if (group is null) return false;

        // Soft-delete all items in the group
        var items = await itemRepo.FindAsync(i => i.ActivityGroupId == groupId, ct);
        foreach (var item in items)
            await itemRepo.DeleteAsync(item, ct);

        await groupRepo.DeleteAsync(group, ct);
        return true;
    }

    public async Task<ActivityItemResponse> CreateItemAsync(Guid accountId, CreateActivityItemRequest request, CancellationToken ct = default)
    {
        var group = await groupRepo.FirstOrDefaultAsync(g => g.Id == request.ActivityGroupId && g.AccountId == accountId, ct)
            ?? throw new InvalidOperationException("Activity group not found.");

        var existingItems = await itemRepo.FindAsync(i => i.ActivityGroupId == group.Id, ct);
        var maxOrder = existingItems.Any() ? existingItems.Max(i => i.SortOrder) : 0;

        var item = new ActivityItem
        {
            ActivityGroupId = group.Id,
            Title = request.Title,
            Description = request.Description,
            TaskType = (int)request.TaskType,
            DurationMinutes = request.DurationMinutes,
            TargetCount = request.TargetCount,
            Icon = request.Icon,
            Color = request.Color,
            SortOrder = maxOrder + 1,
            IsActive = true,
            VerificationTemplateId = request.VerificationTemplate.HasValue ? (int)request.VerificationTemplate.Value : null,
            CustomVerificationCriteria = request.CustomVerificationCriteria
        };

        await itemRepo.AddAsync(item, ct);
        return MapItemResponse(item);
    }

    public async Task<ActivityItemResponse?> UpdateItemAsync(Guid accountId, Guid itemId, UpdateActivityItemRequest request, CancellationToken ct = default)
    {
        var item = await itemRepo.FirstOrDefaultAsync(i => i.Id == itemId, ct);
        if (item is null) return null;

        // Verify ownership through the group
        var group = await groupRepo.FirstOrDefaultAsync(g => g.Id == item.ActivityGroupId && g.AccountId == accountId, ct);
        if (group is null) return null;

        if (request.Title is not null) item.Title = request.Title;
        if (request.Description is not null) item.Description = request.Description;
        if (request.TaskType.HasValue) item.TaskType = (int)request.TaskType.Value;
        if (request.DurationMinutes.HasValue) item.DurationMinutes = request.DurationMinutes;
        if (request.TargetCount.HasValue) item.TargetCount = request.TargetCount;
        if (request.Icon is not null) item.Icon = request.Icon;
        if (request.Color is not null) item.Color = request.Color;
        if (request.SortOrder.HasValue) item.SortOrder = request.SortOrder.Value;
        if (request.IsActive.HasValue) item.IsActive = request.IsActive.Value;
        if (request.VerificationTemplate.HasValue) item.VerificationTemplateId = (int)request.VerificationTemplate.Value;
        if (request.CustomVerificationCriteria is not null) item.CustomVerificationCriteria = request.CustomVerificationCriteria;

        await itemRepo.UpdateAsync(item, ct);
        return MapItemResponse(item);
    }

    public async Task<bool> DeleteItemAsync(Guid accountId, Guid itemId, CancellationToken ct = default)
    {
        var item = await itemRepo.FirstOrDefaultAsync(i => i.Id == itemId, ct);
        if (item is null) return false;

        var group = await groupRepo.FirstOrDefaultAsync(g => g.Id == item.ActivityGroupId && g.AccountId == accountId, ct);
        if (group is null) return false;

        await itemRepo.DeleteAsync(item, ct);
        return true;
    }

    public async Task<DailyActivitiesResponse> GetDailyAsync(Guid accountId, DateOnly date, CancellationToken ct = default)
    {
        // Get owned + shared groups
        var ownedGroups = await groupRepo.FindAsync(g => g.AccountId == accountId && g.IsActive && !g.IsDeleted, ct);
        var memberships = await membershipRepo.FindAsync(m => m.AccountId == accountId && m.Status == GroupMemberStatus.Accepted, ct);
        var sharedGroupIds = memberships.Select(m => m.ActivityGroupId).ToList();
        var sharedGroups = await groupRepo.FindAsync(g => sharedGroupIds.Contains(g.Id) && g.IsActive && !g.IsDeleted, ct);

        var allGroups = ownedGroups.Concat(sharedGroups.Where(sg => sg.AccountId != accountId)).OrderBy(g => g.SortOrder).ToList();

        // Determine which groups are shared (have memberships)
        var allGroupIds = allGroups.Select(g => g.Id).ToList();
        var allMemberships = await membershipRepo.FindAsync(m => allGroupIds.Contains(m.ActivityGroupId), ct);
        var sharedGroupIdSet = allMemberships.Select(m => m.ActivityGroupId).ToHashSet();

        var response = new DailyActivitiesResponse
        {
            Date = date,
            Groups = []
        };

        foreach (var group in allGroups)
        {
            var items = (await itemRepo.FindAsync(i => i.ActivityGroupId == group.Id && i.IsActive, ct))
                .OrderBy(i => i.SortOrder)
                .ToList();

            var completions = items.Count > 0
                ? await completionRepo.FindAsync(
                    c => c.AccountId == accountId && c.Date == date && items.Select(i => i.Id).Contains(c.ActivityItemId), ct)
                : [];

            var completionSet = completions.Select(c => c.ActivityItemId).ToHashSet();

            var isShared = sharedGroupIdSet.Contains(group.Id);

            var dailyItems = new List<DailyActivityItemDto>();
            foreach (var item in items)
            {
                var itemStreak = await CalculateItemStreakAsync(accountId, item.Id, date, ct);
                var completion = completions.FirstOrDefault(c => c.ActivityItemId == item.Id);
                var isCountType = item.TaskType == (int)ActivityItemType.Count;
                var isCountBasedType = isCountType || item.TaskType == (int)ActivityItemType.Steps;
                var currentCount = completion?.CountValue ?? 0;
                var isCompleted = isCountBasedType
                    ? (item.TargetCount.HasValue && currentCount >= item.TargetCount.Value)
                    : completionSet.Contains(item.Id);

                dailyItems.Add(new DailyActivityItemDto
                {
                    Id = item.Id,
                    Title = item.Title,
                    Description = item.Description,
                    TaskType = (ActivityItemType)item.TaskType,
                    DurationMinutes = item.DurationMinutes,
                    TargetCount = item.TargetCount,
                    CurrentCount = currentCount,
                    Icon = item.Icon,
                    Color = item.Color,
                    CurrentStreak = itemStreak,
                    IsCompleted = isCompleted,
                    CompletedAtUtc = completion?.CompletedAtUtc,
                    VerificationTemplate = item.VerificationTemplateId.HasValue
                        ? (VerificationTemplate)item.VerificationTemplateId.Value
                        : null,
                    CustomVerificationCriteria = item.CustomVerificationCriteria
                });
            }

            var streak = await CalculateStreakAsync(accountId, group.Id, date, ct);

            response.Groups.Add(new DailyActivityGroupDto
            {
                Id = group.Id,
                Title = group.Title,
                Icon = group.Icon,
                Color = group.Color,
                SortOrder = group.SortOrder,
                CurrentStreak = streak,
                IsShared = isShared,
                Items = dailyItems
            });

            response.TotalItems += dailyItems.Count;
            response.CompletedItems += dailyItems.Count(i => i.IsCompleted);
        }

        return response;
    }

    public async Task<bool> ToggleCompletionAsync(Guid accountId, ToggleCompletionRequest request, CancellationToken ct = default)
    {
        var item = await itemRepo.FirstOrDefaultAsync(i => i.Id == request.ActivityItemId, ct);
        if (item is null) return false;

        // Check if owner or member
        var isOwner = await groupRepo.ExistsAsync(g => g.Id == item.ActivityGroupId && g.AccountId == accountId, ct);
        var isMember = !isOwner && await membershipRepo.ExistsAsync(m => m.ActivityGroupId == item.ActivityGroupId && m.AccountId == accountId && m.Status == GroupMemberStatus.Accepted, ct);
        
        if (!isOwner && !isMember) return false;

        var existing = await completionRepo.FirstOrDefaultAsync(
            c => c.ActivityItemId == request.ActivityItemId && c.AccountId == accountId && c.Date == request.Date, ct);

        if (existing is not null)
        {
            if (request.CountValue.HasValue)
            {
                existing.CountValue = request.CountValue.Value;
                existing.CompletedAtUtc = DateTime.UtcNow;
                await completionRepo.UpdateAsync(existing, ct);
            }
            else
            {
                await completionRepo.DeleteAsync(existing, ct);
            }
        }
        else
        {
            var completion = new ActivityCompletion
            {
                ActivityItemId = request.ActivityItemId,
                AccountId = accountId,
                Date = request.Date,
                CompletedAtUtc = DateTime.UtcNow,
                CountValue = request.CountValue,
                Note = request.Note
            };
            await completionRepo.AddAsync(completion, ct);
        }

        await statsService.RecalculateGoalMetAsync(accountId, request.Date, ct);

        return true;
    }

    public async Task<VerifyPhotoResponse> VerifyPhotoAsync(Guid accountId, VerifyPhotoRequest request, CancellationToken ct = default)
    {
        var item = await itemRepo.FirstOrDefaultAsync(i => i.Id == request.ActivityItemId, ct)
            ?? throw new InvalidOperationException("Activity item not found.");

        var isOwner = await groupRepo.ExistsAsync(g => g.Id == item.ActivityGroupId && g.AccountId == accountId, ct);
        var isMember = !isOwner && await membershipRepo.ExistsAsync(m => m.ActivityGroupId == item.ActivityGroupId && m.AccountId == accountId && m.Status == GroupMemberStatus.Accepted, ct);

        if (!isOwner && !isMember) throw new InvalidOperationException("Activity item not found.");

        var imageBytes = Convert.FromBase64String(request.ImageBase64);
        var template = item.VerificationTemplateId.HasValue
            ? (VerificationTemplate)item.VerificationTemplateId.Value
            : VerificationTemplate.Custom;

        var result = await visionService.VerifyAsync(imageBytes, template, item.CustomVerificationCriteria, ct);

        if (result.Status == VerificationStatus.Verified)
        {
            await ToggleCompletionAsync(accountId, new ToggleCompletionRequest
            {
                ActivityItemId = request.ActivityItemId,
                Date = request.Date
            }, ct);
        }

        return new VerifyPhotoResponse
        {
            Status = result.Status,
            Explanation = result.Explanation
        };
    }

    private async Task<int> CalculateStreakAsync(Guid accountId, Guid groupId, DateOnly currentDate, CancellationToken ct)
    {
        var items = (await itemRepo.FindAsync(i => i.ActivityGroupId == groupId && i.IsActive, ct)).ToList();
        if (items.Count == 0) return 0;

        var itemIds = items.Select(i => i.Id).ToHashSet();
        var streak = 0;

        // Check up to 365 days back
        for (var dayOffset = 0; dayOffset < 365; dayOffset++)
        {
            var checkDate = currentDate.AddDays(-dayOffset);
            var completions = await completionRepo.FindAsync(
                c => c.AccountId == accountId && c.Date == checkDate && itemIds.Contains(c.ActivityItemId), ct);

            var completedIds = completions.Select(c => c.ActivityItemId).ToHashSet();
            if (itemIds.All(id => completedIds.Contains(id)))
            {
                streak++;
            }
            else
            {
                // For today, don't break the streak if not all items completed yet
                if (dayOffset == 0) continue;
                break;
            }
        }

        return streak;
    }

    private static ActivityGroupResponse MapGroupResponse(ActivityGroup group, List<ActivityItem> items) => new()
    {
        Id = group.Id,
        Title = group.Title,
        Icon = group.Icon,
        Color = group.Color,
        SortOrder = group.SortOrder,
        IsActive = group.IsActive,
        Items = items.Select(MapItemResponse).ToList()
    };

    private static ActivityItemResponse MapItemResponse(ActivityItem item) => new()
    {
        Id = item.Id,
        ActivityGroupId = item.ActivityGroupId,
        Title = item.Title,
        Description = item.Description,
        TaskType = (ActivityItemType)item.TaskType,
        DurationMinutes = item.DurationMinutes,
        TargetCount = item.TargetCount,
        Icon = item.Icon,
        Color = item.Color,
        SortOrder = item.SortOrder,
        IsActive = item.IsActive,
        VerificationTemplate = item.VerificationTemplateId.HasValue ? (VerificationTemplate)item.VerificationTemplateId.Value : null,
        CustomVerificationCriteria = item.CustomVerificationCriteria
    };

    private async Task<int> CalculateItemStreakAsync(Guid accountId, Guid itemId, DateOnly currentDate, CancellationToken ct)
    {
        var recentCompletions = await completionRepo.FindAsync(
            c => c.AccountId == accountId && c.ActivityItemId == itemId && c.Date >= currentDate.AddDays(-365), ct);

        var completionDates = recentCompletions.Select(c => c.Date).ToHashSet();
        var streak = 0;

        for (var dayOffset = 0; dayOffset < 365; dayOffset++)
        {
            var checkDate = currentDate.AddDays(-dayOffset);
            if (completionDates.Contains(checkDate))
            {
                streak++;
            }
            else
            {
                if (dayOffset == 0) continue;
                break;
            }
        }

        return streak;
    }
}
