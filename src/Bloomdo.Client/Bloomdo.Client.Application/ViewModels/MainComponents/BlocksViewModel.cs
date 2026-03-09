using System.Collections.ObjectModel;
using Bloomdo.Client.Application.ViewModels.Items;
using Bloomdo.Client.Core.Interfaces;
using Bloomdo.Shared.DTOs.Blocks;
using Bloomdo.Shared.Enums;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Bloomdo.Client.Application.ViewModels.MainComponents;

public partial class BlocksViewModel : PageViewModel
{
    private readonly IBlockApiService? _blockApiService;

    [ObservableProperty]
    private bool _isMenuOpen;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private bool _hasNoBlockers;

    public ObservableCollection<BlockerItem> Blockers { get; } = [];

    public BlocksViewModel(IBlockApiService? blockApiService = null)
    {
        _blockApiService = blockApiService;
    }

    public override void OnAppearing()
    {
        base.OnAppearing();
        _ = LoadBlockRulesAsync();
    }

    [RelayCommand]
    private void ToggleMenu()
    {
        IsMenuOpen = !IsMenuOpen;
    }

    [RelayCommand]
    private async Task ToggleBlockerActive(BlockerItem? item)
    {
        if (item is null || _blockApiService is null) return;

        var newState = !item.IsActive;
        var result = await _blockApiService.UpdateBlockRuleAsync(item.Id, new UpdateBlockRuleRequest { IsActive = newState });
        if (result is not null)
            item.IsActive = newState;
    }

    [RelayCommand]
    private async Task DeleteBlocker(BlockerItem? item)
    {
        if (item is null || _blockApiService is null) return;

        var deleted = await _blockApiService.DeleteBlockRuleAsync(item.Id);
        if (deleted)
        {
            Blockers.Remove(item);
            HasNoBlockers = Blockers.Count == 0;
        }
    }

    [RelayCommand]
    private async Task CreateFocusBlock()
    {
        IsMenuOpen = false;
        await CreateBlockAsync("Focus Session", BlockType.Focus, focusDurationMinutes: 60);
    }

    [RelayCommand]
    private async Task CreateScheduleBlock()
    {
        IsMenuOpen = false;
        await CreateBlockAsync("Schedule Block", BlockType.Schedule,
            startTime: new TimeOnly(22, 0), endTime: new TimeOnly(7, 0),
            days: [DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday]);
    }

    [RelayCommand]
    private async Task CreateLimitBlock()
    {
        IsMenuOpen = false;
        await CreateBlockAsync("Daily Limit", BlockType.Limit, dailyLimitMinutes: 90);
    }

    [RelayCommand]
    private async Task CreateBloomdoBlock()
    {
        IsMenuOpen = false;
        await CreateBlockAsync("Bloomdo Block", BlockType.Bloomdo);
    }

    private async Task CreateBlockAsync(string title, BlockType type,
        TimeOnly? startTime = null, TimeOnly? endTime = null,
        List<DayOfWeek>? days = null, int? dailyLimitMinutes = null, int? focusDurationMinutes = null)
    {
        if (_blockApiService is null) return;

        var request = new CreateBlockRuleRequest
        {
            Title = title,
            Type = type,
            StartTime = startTime,
            EndTime = endTime,
            Days = days,
            DailyLimitMinutes = dailyLimitMinutes,
            FocusDurationMinutes = focusDurationMinutes
        };

        var result = await _blockApiService.CreateBlockRuleAsync(request);
        if (result is not null)
        {
            Blockers.Add(MapToBlockerItem(result));
            HasNoBlockers = false;
        }
    }

    private async Task LoadBlockRulesAsync()
    {
        if (_blockApiService is null) return;

        IsLoading = true;
        try
        {
            var rules = await _blockApiService.GetBlockRulesAsync();
            Blockers.Clear();

            if (rules is not null)
            {
                foreach (var rule in rules)
                    Blockers.Add(MapToBlockerItem(rule));
            }

            HasNoBlockers = Blockers.Count == 0;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"LoadBlockRules error: {ex}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private static BlockerItem MapToBlockerItem(BlockRuleResponse rule)
    {
        var timeDesc = rule.Type switch
        {
            BlockType.Schedule when rule.StartTime.HasValue && rule.EndTime.HasValue
                => $"{rule.StartTime.Value:HH:mm} - {rule.EndTime.Value:HH:mm}",
            BlockType.Limit when rule.DailyLimitMinutes.HasValue
                => $"{rule.DailyLimitMinutes}m daily limit",
            BlockType.Focus when rule.FocusDurationMinutes.HasValue
                => $"{rule.FocusDurationMinutes}m focus session",
            BlockType.Bloomdo => "Until goals complete",
            _ => string.Empty
        };

        return new BlockerItem(rule.Id, rule.Title, rule.Type, timeDesc, rule.BlockedPackages.Count, rule.IsActive);
    }
}