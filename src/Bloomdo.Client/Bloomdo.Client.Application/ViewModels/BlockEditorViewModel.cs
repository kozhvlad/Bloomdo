using System.Collections.ObjectModel;
using Bloomdo.Client.Application.ViewModels.Items;
using Bloomdo.Client.Core.Interfaces;
using Bloomdo.Shared.DTOs.Blocks;
using Bloomdo.Shared.Enums;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Bloomdo.Client.Application.ViewModels;

public partial class BlockEditorViewModel : ObservableObject
{
    private readonly IBlockApiService? _blockApiService;
    private readonly IInstalledAppsService? _installedAppsService;
    private readonly IDailyActivityApiService? _activityApiService;
    private readonly IAppIconProvider? _appIconProvider;
    private List<SelectableAppItem> _allApps = [];
    private Guid? _editingRuleId;

    [ObservableProperty]
    private BlockType _selectedType;

    [ObservableProperty]
    private string _blockTitle = string.Empty;

    [ObservableProperty]
    private bool _isLoadingApps;

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private bool _isSaving;

    [ObservableProperty]
    private string? _errorMessage;

    // Schedule fields
    [ObservableProperty]
    private TimeSpan _startTime = new(22, 0, 0);

    [ObservableProperty]
    private TimeSpan _endTime = new(7, 0, 0);

    [ObservableProperty]
    private bool _monday = true;

    [ObservableProperty]
    private bool _tuesday = true;

    [ObservableProperty]
    private bool _wednesday = true;

    [ObservableProperty]
    private bool _thursday = true;

    [ObservableProperty]
    private bool _friday = true;

    [ObservableProperty]
    private bool _saturday;

    [ObservableProperty]
    private bool _sunday;

    // Limit fields
    [ObservableProperty]
    private decimal _dailyLimitMinutes = 90;

    // Focus fields
    [ObservableProperty]
    private decimal _focusDurationMinutes = 60;

    // Bloomdo fields
    [ObservableProperty]
    private bool _isLoadingGroups;

    [ObservableProperty]
    private SelectableGroupItem? _selectedGroup;

    public ObservableCollection<SelectableGroupItem> AvailableGroups { get; } = [];

    public ObservableCollection<SelectableAppItem> FilteredApps { get; } = [];

    public bool IsScheduleType => SelectedType == BlockType.Schedule;
    public bool IsLimitType => SelectedType == BlockType.Limit;
    public bool IsFocusType => SelectedType == BlockType.Focus;
    public bool IsBloomdoType => SelectedType == BlockType.Bloomdo;

    public string SelectedAppCountText
    {
        get
        {
            var count = _allApps.Count(a => a.IsSelected);
            return count > 0 ? $"{count} selected" : string.Empty;
        }
    }

    public string TypeColor => SelectedType switch
    {
        BlockType.Schedule => "#009688",
        BlockType.Limit => "#673AB7",
        BlockType.Focus => "#FF9800",
        BlockType.Bloomdo => "#3F51B5",
        _ => "#666666"
    };

    public event Action<BlockRuleResponse>? Saved;
    public event Action<BlockRuleResponse>? Updated;
    public event Action? Cancelled;

    public BlockEditorViewModel(
        IBlockApiService? blockApiService,
        IInstalledAppsService? installedAppsService,
        IDailyActivityApiService? activityApiService = null,
        IAppIconProvider? appIconProvider = null)
    {
        _blockApiService = blockApiService;
        _installedAppsService = installedAppsService;
        _activityApiService = activityApiService;
        _appIconProvider = appIconProvider;
    }

    public void Configure(BlockType type, string defaultTitle)
    {
        _editingRuleId = null;
        SelectedType = type;
        BlockTitle = defaultTitle;
        OnPropertyChanged(nameof(IsScheduleType));
        OnPropertyChanged(nameof(IsLimitType));
        OnPropertyChanged(nameof(IsFocusType));
        OnPropertyChanged(nameof(IsBloomdoType));
        OnPropertyChanged(nameof(TypeColor));
        _ = LoadAppsAsync();

        if (type == BlockType.Bloomdo)
            _ = LoadGroupsAsync();
    }

    public void ConfigureForEdit(BlockRuleResponse rule)
    {
        _editingRuleId = rule.Id;
        SelectedType = rule.Type;
        BlockTitle = rule.Title;

        // Populate type-specific fields
        if (rule.StartTime.HasValue)
            StartTime = rule.StartTime.Value.ToTimeSpan();
        if (rule.EndTime.HasValue)
            EndTime = rule.EndTime.Value.ToTimeSpan();
        if (rule.Days is not null)
        {
            Monday = rule.Days.Contains(DayOfWeek.Monday);
            Tuesday = rule.Days.Contains(DayOfWeek.Tuesday);
            Wednesday = rule.Days.Contains(DayOfWeek.Wednesday);
            Thursday = rule.Days.Contains(DayOfWeek.Thursday);
            Friday = rule.Days.Contains(DayOfWeek.Friday);
            Saturday = rule.Days.Contains(DayOfWeek.Saturday);
            Sunday = rule.Days.Contains(DayOfWeek.Sunday);
        }
        if (rule.DailyLimitMinutes.HasValue)
            DailyLimitMinutes = rule.DailyLimitMinutes.Value;
        if (rule.FocusDurationMinutes.HasValue)
            FocusDurationMinutes = rule.FocusDurationMinutes.Value;

        OnPropertyChanged(nameof(IsScheduleType));
        OnPropertyChanged(nameof(IsLimitType));
        OnPropertyChanged(nameof(IsFocusType));
        OnPropertyChanged(nameof(IsBloomdoType));
        OnPropertyChanged(nameof(TypeColor));
        _ = LoadAppsAsync(rule.BlockedPackages);

        if (rule.Type == BlockType.Bloomdo)
            _ = LoadGroupsAsync(rule.RequiredActivityGroupId);
    }

    partial void OnSearchTextChanged(string value) => ApplyFilter();

    [RelayCommand]
    private async Task Save()
    {
        if (_blockApiService is null) return;

        ErrorMessage = null;

        if (string.IsNullOrWhiteSpace(BlockTitle))
        {
            ErrorMessage = "Enter a block name";
            return;
        }

        if (_allApps.Count(a => a.IsSelected) == 0)
        {
            ErrorMessage = "Select at least one app";
            return;
        }

        if (IsBloomdoType && SelectedGroup is null)
        {
            ErrorMessage = "Select an activity group";
            return;
        }

        IsSaving = true;
        try
        {
            if (_editingRuleId.HasValue)
            {
                // Update existing rule
                var updateRequest = BuildUpdateRequest();
                var result = await _blockApiService.UpdateBlockRuleAsync(_editingRuleId.Value, updateRequest);
                if (result is not null)
                {
                    Updated?.Invoke(result);
                }
                else
                {
                    ErrorMessage = "Server returned empty response";
                }
            }
            else
            {
                // Create new rule
                var request = BuildRequest();
                System.Diagnostics.Debug.WriteLine($"Creating block: {request.Title}, type={request.Type}, apps={request.BlockedPackages.Count}");
                var result = await _blockApiService.CreateBlockRuleAsync(request);
                if (result is not null)
                {
                    System.Diagnostics.Debug.WriteLine($"Block created: {result.Id}");
                    Saved?.Invoke(result);
                }
                else
                {
                    ErrorMessage = "Server returned empty response";
                }
            }
        }
        catch (HttpRequestException ex)
        {
            ErrorMessage = ex.Message;
            System.Diagnostics.Debug.WriteLine($"Save HTTP error: {ex}");
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error: {ex.Message}";
            System.Diagnostics.Debug.WriteLine($"Save error: {ex}");
        }
        finally
        {
            IsSaving = false;
        }
    }

    [RelayCommand]
    private void Cancel() => Cancelled?.Invoke();

    [RelayCommand]
    private void ToggleApp(SelectableAppItem? item)
    {
        if (item is null) return;
        item.IsSelected = !item.IsSelected;
        OnPropertyChanged(nameof(SelectedAppCountText));
    }

    [RelayCommand]
    private void SelectGroup(SelectableGroupItem? item)
    {
        if (item is null) return;

        foreach (var g in AvailableGroups)
            g.IsSelected = false;

        item.IsSelected = true;
        SelectedGroup = item;
    }

    private async Task LoadGroupsAsync(Guid? preSelectedGroupId = null)
    {
        if (_activityApiService is null) return;

        IsLoadingGroups = true;
        try
        {
            var groups = await _activityApiService.GetGroupsAsync();
            AvailableGroups.Clear();

            if (groups is not null)
            {
                foreach (var group in groups)
                {
                    var item = new SelectableGroupItem(group.Id, group.Title);
                    if (preSelectedGroupId.HasValue && group.Id == preSelectedGroupId.Value)
                    {
                        item.IsSelected = true;
                        SelectedGroup = item;
                    }
                    AvailableGroups.Add(item);
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"LoadGroups error: {ex}");
        }
        finally
        {
            IsLoadingGroups = false;
        }
    }

    private async Task LoadAppsAsync(List<string>? preSelectedPackages = null)
    {
        if (_installedAppsService is null) return;

        IsLoadingApps = true;
        try
        {
            var apps = await _installedAppsService.GetInstalledAppsAsync();
            var selectedSet = preSelectedPackages?.ToHashSet(StringComparer.OrdinalIgnoreCase);
            _allApps = apps
                .OrderBy(a => a.AppLabel)
                .Select(a =>
                {
                    var item = new SelectableAppItem(a.PackageName, a.AppLabel, iconBytes: _appIconProvider?.GetIcon(a.PackageName));
                    if (selectedSet is not null && selectedSet.Contains(a.PackageName))
                        item.IsSelected = true;
                    return item;
                })
                .ToList();

            ApplyFilter();
            OnPropertyChanged(nameof(SelectedAppCountText));
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"LoadApps error: {ex}");
        }
        finally
        {
            IsLoadingApps = false;
        }
    }

    private void ApplyFilter()
    {
        FilteredApps.Clear();
        var query = SearchText?.Trim() ?? string.Empty;
        var source = string.IsNullOrEmpty(query)
            ? _allApps
            : _allApps.Where(a => a.AppLabel.Contains(query, StringComparison.OrdinalIgnoreCase));

        foreach (var app in source)
            FilteredApps.Add(app);
    }

    private CreateBlockRuleRequest BuildRequest()
    {
        var selectedPackages = _allApps
            .Where(a => a.IsSelected)
            .Select(a => a.PackageName)
            .ToList();

        return new CreateBlockRuleRequest
        {
            Title = BlockTitle,
            Type = SelectedType,
            BlockedPackages = selectedPackages,
            StartTime = IsScheduleType ? TimeOnly.FromTimeSpan(StartTime) : null,
            EndTime = IsScheduleType ? TimeOnly.FromTimeSpan(EndTime) : null,
            Days = IsScheduleType ? GetSelectedDays() : null,
            DailyLimitMinutes = IsLimitType ? (int)DailyLimitMinutes : null,
            FocusDurationMinutes = IsFocusType ? (int)FocusDurationMinutes : null,
            RequiredActivityGroupId = IsBloomdoType ? SelectedGroup?.Id : null
        };
    }

    private UpdateBlockRuleRequest BuildUpdateRequest()
    {
        var selectedPackages = _allApps
            .Where(a => a.IsSelected)
            .Select(a => a.PackageName)
            .ToList();

        return new UpdateBlockRuleRequest
        {
            Title = BlockTitle,
            BlockedPackages = selectedPackages,
            StartTime = IsScheduleType ? TimeOnly.FromTimeSpan(StartTime) : null,
            EndTime = IsScheduleType ? TimeOnly.FromTimeSpan(EndTime) : null,
            Days = IsScheduleType ? GetSelectedDays() : null,
            DailyLimitMinutes = IsLimitType ? (int)DailyLimitMinutes : null,
            FocusDurationMinutes = IsFocusType ? (int)FocusDurationMinutes : null,
            RequiredActivityGroupId = IsBloomdoType ? SelectedGroup?.Id : null
        };
    }

    private List<DayOfWeek> GetSelectedDays()
    {
        var days = new List<DayOfWeek>();
        if (Monday) days.Add(DayOfWeek.Monday);
        if (Tuesday) days.Add(DayOfWeek.Tuesday);
        if (Wednesday) days.Add(DayOfWeek.Wednesday);
        if (Thursday) days.Add(DayOfWeek.Thursday);
        if (Friday) days.Add(DayOfWeek.Friday);
        if (Saturday) days.Add(DayOfWeek.Saturday);
        if (Sunday) days.Add(DayOfWeek.Sunday);
        return days;
    }
}
