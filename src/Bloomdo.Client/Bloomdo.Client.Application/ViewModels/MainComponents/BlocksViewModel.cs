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
    private readonly IInstalledAppsService? _installedAppsService;
    private readonly IBlockRuleStore? _blockRuleStore;
    private List<BlockRuleResponse> _cachedRules = [];

    [ObservableProperty]
    private bool _isMenuOpen;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private bool _hasNoBlockers;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsEditing))]
    private BlockEditorViewModel? _editor;

    public bool IsEditing => Editor is not null;

    public ObservableCollection<BlockerItem> Blockers { get; } = [];

    public BlocksViewModel(
        IBlockApiService? blockApiService = null,
        IInstalledAppsService? installedAppsService = null,
        IBlockRuleStore? blockRuleStore = null)
    {
        _blockApiService = blockApiService;
        _installedAppsService = installedAppsService;
        _blockRuleStore = blockRuleStore;
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
        if (item is null || _blockApiService is null || item.IsToggling) return;

        item.IsToggling = true;
        var newState = !item.IsActive;

        try
        {
            var result = await _blockApiService.UpdateBlockRuleAsync(item.Id, new UpdateBlockRuleRequest { IsActive = newState });
            if (result is not null)
            {
                item.IsActive = newState;

                var index = _cachedRules.FindIndex(r => r.Id == item.Id);
                if (index >= 0)
                    _cachedRules[index] = result;

                await SyncRulesToLocalStoreAsync();
            }
        }
        finally
        {
            item.IsToggling = false;
        }
    }

    [RelayCommand]
    private async Task DeleteBlocker(BlockerItem? item)
    {
        if (item is null || _blockApiService is null || item.IsDeleting) return;

        item.IsDeleting = true;

        try
        {
            var deleted = await _blockApiService.DeleteBlockRuleAsync(item.Id);
            if (deleted)
            {
                Blockers.Remove(item);
                _cachedRules.RemoveAll(r => r.Id == item.Id);
                HasNoBlockers = Blockers.Count == 0;
                await SyncRulesToLocalStoreAsync();
            }
        }
        finally
        {
            item.IsDeleting = false;
        }
    }

    [RelayCommand]
    private void CreateFocusBlock()
    {
        IsMenuOpen = false;
        OpenEditor(BlockType.Focus, "Focus Session");
    }

    [RelayCommand]
    private void CreateScheduleBlock()
    {
        IsMenuOpen = false;
        OpenEditor(BlockType.Schedule, "Schedule Block");
    }

    [RelayCommand]
    private void CreateLimitBlock()
    {
        IsMenuOpen = false;
        OpenEditor(BlockType.Limit, "Daily Limit");
    }

    [RelayCommand]
    private void CreateBloomdoBlock()
    {
        IsMenuOpen = false;
        OpenEditor(BlockType.Bloomdo, "Bloomdo Block");
    }

    [RelayCommand]
    private void EditBlocker(BlockerItem? item)
    {
        if (item is null) return;

        var cached = _cachedRules.FirstOrDefault(r => r.Id == item.Id);
        if (cached is null) return;

        var editor = new BlockEditorViewModel(_blockApiService, _installedAppsService);
        editor.Configure(cached.Type, cached.Title);
        editor.Saved += OnBlockSaved;
        editor.Cancelled += OnEditorCancelled;
        Editor = editor;
    }

    private void OpenEditor(BlockType type, string defaultTitle)
    {
        var editor = new BlockEditorViewModel(_blockApiService, _installedAppsService);
        editor.Configure(type, defaultTitle);
        editor.Saved += OnBlockSaved;
        editor.Cancelled += OnEditorCancelled;
        Editor = editor;
    }

    private async void OnBlockSaved(BlockRuleResponse response)
    {
        Blockers.Add(MapToBlockerItem(response));
        _cachedRules.Add(response);
        HasNoBlockers = false;
        CloseEditor();
        await SyncRulesToLocalStoreAsync();
    }

    private void OnEditorCancelled()
    {
        CloseEditor();
    }

    private void CloseEditor()
    {
        if (Editor is not null)
        {
            Editor.Saved -= OnBlockSaved;
            Editor.Cancelled -= OnEditorCancelled;
            Editor = null;
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
            _cachedRules.Clear();

            if (rules is not null)
            {
                _cachedRules = [..rules];
                foreach (var rule in rules)
                    Blockers.Add(MapToBlockerItem(rule));
            }

            HasNoBlockers = Blockers.Count == 0;
            await SyncRulesToLocalStoreAsync();
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

    private async Task SyncRulesToLocalStoreAsync()
    {
        if (_blockRuleStore is null) return;

        try
        {
            await _blockRuleStore.SaveRulesAsync(_cachedRules);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"SyncRulesToLocalStore error: {ex}");
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