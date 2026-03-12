using System.Text.Json;
using Bloomdo.Server.Application.Interfaces;
using Bloomdo.Server.Domain.Entities;
using Bloomdo.Shared.DTOs.Blocks;

namespace Bloomdo.Server.Application.Services;

public class BlockService(IRepository<BlockRule> blockRuleRepository) : IBlockService
{
    public async Task<List<BlockRuleResponse>> GetBlockRulesAsync(Guid accountId, CancellationToken ct = default)
    {
        var rules = await blockRuleRepository.FindAsync(r => r.AccountId == accountId, ct);
        return rules.Select(MapToResponse).ToList();
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
            FocusStartedAtUtc = request.Type == Bloomdo.Shared.Enums.BlockType.Focus ? DateTime.UtcNow : null
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

    private static BlockRuleResponse MapToResponse(BlockRule rule) => new()
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
        FocusStartedAtUtc = rule.FocusStartedAtUtc
    };
}
