using System.Text.Json;
using Bloomdo.Server.Application.Interfaces;
using Bloomdo.Server.Domain.Entities;
using Bloomdo.Shared.DTOs.Blocks;

namespace Bloomdo.Server.Application.Services;

public class BlockService(
    IRepository<BlockRule> blockRuleRepository,
    IRepository<ActivityGroup> activityGroupRepository,
    IRepository<ActivityItem> activityItemRepository,
    IRepository<ActivityCompletion> activityCompletionRepository) : IBlockService
{
    public async Task<List<BlockRuleResponse>> GetBlockRulesAsync(Guid accountId, CancellationToken ct = default)
    {
        var rules = await blockRuleRepository.FindAsync(r => r.AccountId == accountId, ct);
        var responses = new List<BlockRuleResponse>();

        foreach (var rule in rules)
        {
            string? groupTitle = null;
            var isBloomdoSatisfied = false;

            if (rule.Type == Bloomdo.Shared.Enums.BlockType.Bloomdo && rule.RequiredActivityGroupId.HasValue)
            {
                var group = await activityGroupRepository.FirstOrDefaultAsync(
                    g => g.Id == rule.RequiredActivityGroupId.Value && g.AccountId == accountId, ct);

                groupTitle = group?.Title;
                isBloomdoSatisfied = await IsGroupCompletedAsync(accountId, rule.RequiredActivityGroupId.Value, ct);
            }

            responses.Add(MapToResponse(rule, groupTitle, isBloomdoSatisfied));
        }

        return responses;
    }

    public async Task<BlockRuleResponse> CreateBlockRuleAsync(Guid accountId, CreateBlockRuleRequest request, CancellationToken ct = default)
    {
        var rule = new BlockRule
        {
            AccountId = accountId,
            Title = request.Title,
            Type = request.Type,
            IsActive = true,
            BlockedPackagesJson = JsonSerializer.Serialize(request.BlockedPackages),
            StartTime = request.StartTime,
            EndTime = request.EndTime,
            ScheduleDaysJson = request.Days is not null ? JsonSerializer.Serialize(request.Days) : null,
            DailyLimitMinutes = request.DailyLimitMinutes,
            FocusDurationMinutes = request.FocusDurationMinutes,
            FocusStartedAtUtc = request.Type == Bloomdo.Shared.Enums.BlockType.Focus ? DateTime.UtcNow : null,
            RequiredActivityGroupId = request.RequiredActivityGroupId
        };

        await blockRuleRepository.AddAsync(rule, ct);
        return MapToResponse(rule);
    }

    public async Task<BlockRuleResponse?> UpdateBlockRuleAsync(Guid accountId, Guid ruleId, UpdateBlockRuleRequest request, CancellationToken ct = default)
    {
        var rule = await blockRuleRepository.FirstOrDefaultAsync(
            r => r.Id == ruleId && r.AccountId == accountId, ct);

        if (rule is null)
            return null;

        if (request.Title is not null) rule.Title = request.Title;
        if (request.IsActive.HasValue) rule.IsActive = request.IsActive.Value;
        if (request.BlockedPackages is not null) rule.BlockedPackagesJson = JsonSerializer.Serialize(request.BlockedPackages);
        if (request.StartTime.HasValue) rule.StartTime = request.StartTime;
        if (request.EndTime.HasValue) rule.EndTime = request.EndTime;
        if (request.Days is not null) rule.ScheduleDaysJson = JsonSerializer.Serialize(request.Days);
        if (request.DailyLimitMinutes.HasValue) rule.DailyLimitMinutes = request.DailyLimitMinutes;
        if (request.FocusDurationMinutes.HasValue) rule.FocusDurationMinutes = request.FocusDurationMinutes;
        if (request.RequiredActivityGroupId.HasValue) rule.RequiredActivityGroupId = request.RequiredActivityGroupId;

        await blockRuleRepository.UpdateAsync(rule, ct);
        return MapToResponse(rule);
    }

    public async Task<bool> DeleteBlockRuleAsync(Guid accountId, Guid ruleId, CancellationToken ct = default)
    {
        var rule = await blockRuleRepository.FirstOrDefaultAsync(
            r => r.Id == ruleId && r.AccountId == accountId, ct);

        if (rule is null)
            return false;

        await blockRuleRepository.DeleteAsync(rule, ct);
        return true;
    }

    private static BlockRuleResponse MapToResponse(BlockRule rule, string? groupTitle = null, bool isBloomdoSatisfied = false) => new()
    {
        Id = rule.Id,
        Title = rule.Title,
        Type = rule.Type,
        IsActive = rule.IsActive,
        BlockedPackages = JsonSerializer.Deserialize<List<string>>(rule.BlockedPackagesJson) ?? [],
        StartTime = rule.StartTime,
        EndTime = rule.EndTime,
        Days = rule.ScheduleDaysJson is not null
            ? JsonSerializer.Deserialize<List<DayOfWeek>>(rule.ScheduleDaysJson)
            : null,
        DailyLimitMinutes = rule.DailyLimitMinutes,
        FocusDurationMinutes = rule.FocusDurationMinutes,
        FocusStartedAtUtc = rule.FocusStartedAtUtc,
        RequiredActivityGroupId = rule.RequiredActivityGroupId,
        RequiredActivityGroupTitle = groupTitle,
        IsBloomdoSatisfied = isBloomdoSatisfied
    };

    private async Task<bool> IsGroupCompletedAsync(Guid accountId, Guid groupId, CancellationToken ct)
    {
        var items = (await activityItemRepository.FindAsync(
            i => i.ActivityGroupId == groupId && i.IsActive, ct)).ToList();

        if (items.Count == 0)
            return false;

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var itemIds = items.Select(i => i.Id).ToHashSet();

        var completions = await activityCompletionRepository.FindAsync(
            c => c.AccountId == accountId && c.Date == today && itemIds.Contains(c.ActivityItemId), ct);

        var completedIds = completions.Select(c => c.ActivityItemId).ToHashSet();
        return itemIds.All(id => completedIds.Contains(id));
    }
}
