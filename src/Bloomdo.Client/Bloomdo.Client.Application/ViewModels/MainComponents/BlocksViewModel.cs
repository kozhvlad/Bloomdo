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
    private readonly IDailyActivityApiService? _activityApiService;
    private readonly IAppIconProvider? _appIconProvider;
    private readonly ISubscriptionApiService? _subscriptionApiService;
    private readonly IConnectivityService? _connectivityService;
    private readonly ILocalSubscriptionStore? _localSubscriptionStore;
    private readonly IConfirmDialogService? _confirmDialogService;
    private List<BlockRuleResponse> _cachedRules = [];
    private List<BlockerItem> _allBlockers = [];
    private bool? _sortDirection;

    [ObservableProperty]
    private bool _isMenuOpen;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private bool _hasNoBlockers;

    [ObservableProperty]
    private bool _isLimitReached;

    [ObservableProperty]
    private int _maxBlockRules;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsEditing))]
    private BlockEditorViewModel? _editor;

    [ObservableProperty]
    private bool _isOffline;

    [ObservableProperty]
    private string _searchText = string.Empty;

    public string SortButtonText => _sortDirection switch
    {
        true => "A \u2192 Z",
        false => "Z \u2192 A",
        _ => "Sort"
    };

    public bool IsEditing => Editor is not null;

    public ObservableCollection<BlockerItem> Blockers { get; } = [];

    public BlocksViewModel(
        IBlockApiService? blockApiService = null,
        IInstalledAppsService? installedAppsService = null,
        IBlockRuleStore? blockRuleStore = null,
        IDailyActivityApiService? activityApiService = null,
        IAppIconProvider? appIconProvider = null,
        ISubscriptionApiService? subscriptionApiService = null,
        IConnectivityService? connectivityService = null,
        ILocalSubscriptionStore? localSubscriptionStore = null,
        IConfirmDialogService? confirmDialogService = null)
    {
        _blockApiService = blockApiService;
        _installedAppsService = installedAppsService;
        _blockRuleStore = blockRuleStore;
        _activityApiService = activityApiService;
        _appIconProvider = appIconProvider;
        _subscriptionApiService = subscriptionApiService;
        _connectivityService = connectivityService;
        _localSubscriptionStore = localSubscriptionStore;
        _confirmDialogService = confirmDialogService;
    }

    public override void OnAppearing()
    {
        base.OnAppearing();
        IsOffline = _connectivityService is not null && !_connectivityService.IsOnline;
        _ = LoadBlockRulesAsync();
        if (!IsOffline)
            _ = LoadSubscriptionLimitsAsync();
        else
            _ = LoadSubscriptionLimitsFromCacheAsync();
    }

    private async Task LoadSubscriptionLimitsAsync()
    {
        if (_subscriptionApiService is null) return;

        try
        {
            var status = await _subscriptionApiService.GetStatusAsync();
            if (status?.Limits is not null)
            {
                MaxBlockRules = status.Limits.MaxBlockRules;
                IsLimitReached = !status.IsPremium && _allBlockers.Count >= status.Limits.MaxBlockRules;

                if (_localSubscriptionStore is not null)
                    _ = _localSubscriptionStore.SaveAsync(status);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"LoadSubscriptionLimits error: {ex}");
        }
    }

    private async Task LoadSubscriptionLimitsFromCacheAsync()
    {
        if (_localSubscriptionStore is null) return;

        try
        {
            var status = await _localSubscriptionStore.LoadAsync();
            if (status?.Limits is not null)
            {
                MaxBlockRules = status.Limits.MaxBlockRules;
                IsLimitReached = !status.IsPremium && _allBlockers.Count >= status.Limits.MaxBlockRules;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"LoadSubscriptionLimitsFromCache error: {ex}");
        }
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

        if (_confirmDialogService is not null)
        {
            var confirmed = await _confirmDialogService.ConfirmAsync(
                "Delete Block Rule",
                $"Are you sure you want to delete \"{item.Title}\"? This action cannot be undone.");
            if (!confirmed) return;
        }

        item.IsDeleting = true;

        try
        {
            var deleted = await _blockApiService.DeleteBlockRuleAsync(item.Id);
            if (deleted)
            {
                _allBlockers.RemoveAll(b => b.Id == item.Id);
                _cachedRules.RemoveAll(r => r.Id == item.Id);
                ApplyFilterAndSort();
                IsLimitReached = MaxBlockRules > 0 && _allBlockers.Count >= MaxBlockRules;
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

        var editor = new BlockEditorViewModel(_blockApiService, _installedAppsService, _activityApiService, _appIconProvider);
        editor.ConfigureForEdit(cached);
        editor.Updated += OnBlockUpdated;
        editor.Cancelled += OnEditorCancelled;
        Editor = editor;
    }

    private void OpenEditor(BlockType type, string defaultTitle)
    {
        var editor = new BlockEditorViewModel(_blockApiService, _installedAppsService, _activityApiService, _appIconProvider);
        editor.Configure(type, defaultTitle);
        editor.Saved += OnBlockSaved;
        editor.Cancelled += OnEditorCancelled;
        Editor = editor;
    }

    private async void OnBlockSaved(BlockRuleResponse response)
    {
        _allBlockers.Add(MapToBlockerItem(response));
        _cachedRules.Add(response);
        ApplyFilterAndSort();
        IsLimitReached = MaxBlockRules > 0 && _allBlockers.Count >= MaxBlockRules;
        CloseEditor();
        await SyncRulesToLocalStoreAsync();
    }

    private async void OnBlockUpdated(BlockRuleResponse response)
    {
        var existingIndex = -1;
        for (var i = 0; i < Blockers.Count; i++)
        {
            if (Blockers[i].Id == response.Id)
            {
                existingIndex = i;
                break;
            }
        }

        if (existingIndex >= 0)
        {
            var updatedItem = MapToBlockerItem(response);
            var allIndex = _allBlockers.FindIndex(b => b.Id == response.Id);
            if (allIndex >= 0)
                _allBlockers[allIndex] = updatedItem;
            Blockers[existingIndex] = updatedItem;
        }

        var cachedIndex = _cachedRules.FindIndex(r => r.Id == response.Id);
        if (cachedIndex >= 0)
            _cachedRules[cachedIndex] = response;

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
            Editor.Updated -= OnBlockUpdated;
            Editor.Cancelled -= OnEditorCancelled;
            Editor = null;
        }
    }

    private async Task LoadBlockRulesAsync()
    {
        IsLoading = true;
        try
        {
            List<BlockRuleResponse>? rules = null;

            if (!IsOffline && _blockApiService is not null)
            {
                try
                {
                    rules = await _blockApiService.GetBlockRulesAsync();
                }
                catch (HttpRequestException)
                {
                    IsOffline = true;
                }
                catch (TaskCanceledException)
                {
                    IsOffline = true;
                }
            }

            // Fallback to local cache
            if (rules is null && _blockRuleStore is not null)
            {
                var localRules = await _blockRuleStore.LoadRulesAsync();
                rules = localRules.ToList();
            }

            Blockers.Clear();
            _cachedRules.Clear();
            _allBlockers.Clear();

            if (rules is not null)
            {
                _cachedRules = [..rules];
                foreach (var rule in rules)
                    _allBlockers.Add(MapToBlockerItem(rule));
            }

            ApplyFilterAndSort();

            if (!IsOffline)
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

    partial void OnSearchTextChanged(string value) => ApplyFilterAndSort();

    [RelayCommand]
    private void ToggleSort()
    {
        _sortDirection = _sortDirection switch
        {
            null => true,
            true => false,
            false => null
        };
        OnPropertyChanged(nameof(SortButtonText));
        ApplyFilterAndSort();
    }

    private void ApplyFilterAndSort()
    {
        var filtered = _allBlockers.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            var search = SearchText.Trim();
            filtered = filtered.Where(b =>
                b.Title.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                b.TypeLabel.Contains(search, StringComparison.OrdinalIgnoreCase));
        }

        filtered = _sortDirection switch
        {
            true => filtered.OrderBy(b => b.Title, StringComparer.OrdinalIgnoreCase),
            false => filtered.OrderByDescending(b => b.Title, StringComparer.OrdinalIgnoreCase),
            _ => filtered
        };

        Blockers.Clear();
        foreach (var b in filtered)
            Blockers.Add(b);

        HasNoBlockers = Blockers.Count == 0;
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
            BlockType.Bloomdo when rule.RequiredActivityGroupTitle is not null
                => $"Until \"{rule.RequiredActivityGroupTitle}\" done",
            BlockType.Bloomdo => "Until goals complete",
            _ => string.Empty
        };

        return new BlockerItem(rule.Id, rule.Title, rule.Type, timeDesc, rule.BlockedPackages.Count, rule.IsActive);
    }
}