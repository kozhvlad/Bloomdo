using System.Collections.ObjectModel;
using Bloomdo.Client.Application.ViewModels.Items;
using Bloomdo.Client.Core.Interfaces;
using Bloomdo.Shared.DTOs.Activities;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Bloomdo.Client.Application.ViewModels.MainComponents;

public partial class HomeViewModel : PageViewModel
{
    private readonly IDailyActivityApiService? _activityApi;
    private readonly IGroupCompletionStore? _groupCompletionStore;
    private readonly IBlockRuleStore? _blockRuleStore;
    private readonly IBlockApiService? _blockApiService;
    private readonly INavigationService? _navigationService;
    private readonly ITimerDialogService? _timerDialogService;
    private readonly IConfirmDialogService? _confirmDialogService;

    [ObservableProperty]
    private string _welcomeMessage = "Welcome to Bloomdo!";

    [ObservableProperty]
    private string _todayDateText = DateTime.Now.ToString("dddd, MMMM d");

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private int _totalItems;

    [ObservableProperty]
    private int _completedItems;

    [ObservableProperty]
    private bool _hasNoGroups;

    [ObservableProperty]
    private bool _isAddingGroup;

    [ObservableProperty]
    private string _newGroupTitle = string.Empty;

    [ObservableProperty]
    private string _newGroupIcon = string.Empty;

    [ObservableProperty]
    private string _newGroupColor = "#7E57C2";

    public ObservableCollection<ActivityGroupItemViewModel> Groups { get; } = [];

    public string ProgressText => TotalItems > 0 ? $"{CompletedItems}/{TotalItems}" : "0/0";
    public double ProgressPercent => TotalItems > 0 ? (double)CompletedItems / TotalItems * 100 : 0;
    public double ProgressFraction => TotalItems > 0 ? (double)CompletedItems / TotalItems : 0;

    public static string[] AvailableColors { get; } =
        ["#7E57C2", "#42A5F5", "#66BB6A", "#FF9800", "#EF5350", "#26C6DA", "#AB47BC", "#5C6BC0", "#EC407A", "#8D6E63"];

    public HomeViewModel(
        IDailyActivityApiService? activityApi = null,
        IGroupCompletionStore? groupCompletionStore = null,
        IBlockRuleStore? blockRuleStore = null,
        IBlockApiService? blockApiService = null,
        INavigationService? navigationService = null,
        ITimerDialogService? timerDialogService = null,
        IConfirmDialogService? confirmDialogService = null)
    {
        _activityApi = activityApi;
        _groupCompletionStore = groupCompletionStore;
        _blockRuleStore = blockRuleStore;
        _blockApiService = blockApiService;
        _navigationService = navigationService;
        _timerDialogService = timerDialogService;
        _confirmDialogService = confirmDialogService;
    }

    public override void OnAppearing()
    {
        base.OnAppearing();
        TodayDateText = DateTime.Now.ToString("dddd, MMMM d");
        _ = LoadDailyActivitiesAsync();
    }

    [RelayCommand]
    private async Task LoadDailyActivitiesAsync()
    {
        if (_activityApi is null) return;

        IsLoading = true;
        try
        {
            var daily = await _activityApi.GetDailyAsync();
            if (daily is null) return;

            Groups.Clear();
            foreach (var group in daily.Groups)
            {
                var vm = new ActivityGroupItemViewModel
                {
                    Id = group.Id,
                    Title = group.Title,
                    Icon = group.Icon,
                    Color = group.Color,
                    CurrentStreak = group.CurrentStreak
                };

                foreach (var item in group.Items)
                {
                    vm.Tasks.Add(new ActivityTaskItemViewModel
                    {
                        Id = item.Id,
                        Title = item.Title,
                        Description = item.Description,
                        TaskType = item.TaskType,
                        DurationMinutes = item.DurationMinutes,
                        TargetCount = item.TargetCount,
                        CurrentCount = item.CurrentCount,
                        Icon = item.Icon,
                        Color = item.Color,
                        CurrentStreak = item.CurrentStreak,
                        IsCompleted = item.IsCompleted,
                        CompletedAtUtc = item.CompletedAtUtc
                    });
                }

                Groups.Add(vm);
            }

            TotalItems = daily.TotalItems;
            CompletedItems = daily.CompletedItems;
            HasNoGroups = Groups.Count == 0;
            OnPropertyChanged(nameof(ProgressText));
            OnPropertyChanged(nameof(ProgressPercent));
            OnPropertyChanged(nameof(ProgressFraction));

            await SyncGroupCompletionAsync();
        }
        finally
        {
            IsLoading = false;
        }
    }

    // --- Toggle completion ---

    [RelayCommand]
    private async Task ToggleTask(ActivityTaskItemViewModel? task)
    {
        if (task is null || _activityApi is null || task.IsToggling) return;

        task.IsToggling = true;
        try
        {
            var request = new ToggleCompletionRequest
            {
                ActivityItemId = task.Id,
                Date = DateOnly.FromDateTime(DateTime.UtcNow)
            };

            var result = await _activityApi.ToggleCompletionAsync(request);
            if (result)
            {
                task.IsCompleted = !task.IsCompleted;
                task.CompletedAtUtc = task.IsCompleted ? DateTime.UtcNow : null;

                RecalculateProgress();
                await SyncGroupCompletionAsync();
            }
        }
        finally
        {
            task.IsToggling = false;
        }
    }

    // --- Group CRUD ---

    [RelayCommand]
    private void ShowAddGroup()
    {
        _navigationService?.NavigateTo<GroupEditorViewModel>(vm => vm.ConfigureForCreate());
    }

    [RelayCommand]
    private async Task ConfirmAddGroup()
    {
        if (string.IsNullOrWhiteSpace(NewGroupTitle) || _activityApi is null) return;

        var request = new CreateActivityGroupRequest
        {
            Title = NewGroupTitle.Trim(),
            Icon = NewGroupIcon,
            Color = NewGroupColor
        };

        var result = await _activityApi.CreateGroupAsync(request);
        if (result is not null)
        {
            Groups.Add(new ActivityGroupItemViewModel
            {
                Id = result.Id,
                Title = result.Title,
                Icon = result.Icon,
                Color = result.Color,
                CurrentStreak = 0
            });

            HasNoGroups = false;
            IsAddingGroup = false;
            NewGroupTitle = string.Empty;
        }
    }

    [RelayCommand]
    private async Task DeleteGroup(ActivityGroupItemViewModel? group)
    {
        if (group is null || _activityApi is null) return;

        if (_confirmDialogService is not null)
        {
            var confirmed = await _confirmDialogService.ConfirmAsync(
                "Delete Group",
                $"Delete \"{group.Title}\" and all its tasks? This action cannot be undone.");
            if (!confirmed) return;
        }

        var deleted = await _activityApi.DeleteGroupAsync(group.Id);
        if (deleted)
        {
            Groups.Remove(group);
            HasNoGroups = Groups.Count == 0;
            RecalculateProgress();
        }
    }

    [RelayCommand]
    private void StartEditGroup(ActivityGroupItemViewModel? group)
    {
        if (group is null) return;
        _navigationService?.NavigateTo<GroupEditorViewModel>(vm => vm.ConfigureForEdit(group));
    }

    [RelayCommand]
    private void CancelEditGroup(ActivityGroupItemViewModel? group)
    {
        if (group is null) return;
        group.IsEditing = false;
    }

    [RelayCommand]
    private async Task ConfirmEditGroup(ActivityGroupItemViewModel? group)
    {
        if (group is null || string.IsNullOrWhiteSpace(group.EditTitle) || _activityApi is null) return;

        var request = new UpdateActivityGroupRequest { Title = group.EditTitle.Trim() };
        var result = await _activityApi.UpdateGroupAsync(group.Id, request);
        if (result is not null)
        {
            group.Title = result.Title;
            group.IsEditing = false;
        }
    }

    [RelayCommand]
    private void ToggleGroupExpanded(ActivityGroupItemViewModel? group)
    {
        if (group is null) return;
        group.IsExpanded = !group.IsExpanded;
    }

    // --- Item CRUD ---

    [RelayCommand]
    private void ShowAddItem(ActivityGroupItemViewModel? group)
    {
        if (group is null) return;
        _navigationService?.NavigateTo<TaskEditorViewModel>(vm => vm.ConfigureForCreate(group.Id, group.Color));
    }

    [RelayCommand]
    private void CancelAddItem(ActivityGroupItemViewModel? group)
    {
        if (group is null) return;
        group.IsAddingItem = false;
        group.ResetNewItemForm();
    }

    [RelayCommand]
    private void SelectItemColor(string? color)
    {
        if (color is null) return;
        foreach (var group in Groups)
        {
            if (group.IsAddingItem)
            {
                group.NewItemColor = color;
                break;
            }
        }
    }

    [RelayCommand]
    private void IncrementNewItemDuration(ActivityGroupItemViewModel? group)
    {
        if (group is null) return;
        group.NewItemDurationMinutes = Math.Min(group.NewItemDurationMinutes + 5, 480);
    }

    [RelayCommand]
    private void DecrementNewItemDuration(ActivityGroupItemViewModel? group)
    {
        if (group is null) return;
        group.NewItemDurationMinutes = Math.Max(group.NewItemDurationMinutes - 5, 5);
    }

    [RelayCommand]
    private async Task ConfirmAddItem(ActivityGroupItemViewModel? group)
    {
        if (group is null || string.IsNullOrWhiteSpace(group.NewItemTitle) || _activityApi is null) return;

        var request = new CreateActivityItemRequest
        {
            ActivityGroupId = group.Id,
            Title = group.NewItemTitle.Trim(),
            Description = string.IsNullOrWhiteSpace(group.NewItemDescription) ? null : group.NewItemDescription.Trim(),
            DurationMinutes = group.NewItemDurationMinutes,
            Icon = group.NewItemIcon,
            Color = group.NewItemColor
        };

        var result = await _activityApi.CreateItemAsync(request);
        if (result is not null)
        {
            group.Tasks.Add(new ActivityTaskItemViewModel
            {
                Id = result.Id,
                Title = result.Title,
                Description = result.Description,
                TaskType = result.TaskType,
                DurationMinutes = result.DurationMinutes,
                TargetCount = result.TargetCount,
                Icon = result.Icon,
                Color = result.Color,
                CurrentStreak = 0,
                IsCompleted = false
            });

            group.IsAddingItem = false;
            group.ResetNewItemForm();
            group.RefreshProgress();
            RecalculateProgress();
        }
    }

    [RelayCommand]
    private async Task DeleteItem(ActivityTaskItemViewModel? task)
    {
        if (task is null || _activityApi is null) return;

        if (_confirmDialogService is not null)
        {
            var confirmed = await _confirmDialogService.ConfirmAsync(
                "Delete Task",
                $"Delete \"{task.Title}\"? This action cannot be undone.");
            if (!confirmed) return;
        }

        var deleted = await _activityApi.DeleteItemAsync(task.Id);
        if (deleted)
        {
            foreach (var group in Groups)
            {
                if (group.Tasks.Remove(task))
                {
                    group.RefreshProgress();
                    break;
                }
            }
            RecalculateProgress();
        }
    }

    // --- Edit item ---

    [RelayCommand]
    private void StartEditItem(ActivityTaskItemViewModel? task)
    {
        if (task is null) return;
        _navigationService?.NavigateTo<TaskEditorViewModel>(vm => vm.ConfigureForEdit(task));
    }

    // --- Timer ---

    [RelayCommand]
    private void StartTimer(ActivityTaskItemViewModel? task)
    {
        if (task is null || !task.HasDuration) return;

        _timerDialogService?.ShowTimer(
            task.Title,
            string.IsNullOrEmpty(task.Icon) ? "⏰" : task.Icon,
            string.IsNullOrEmpty(task.Color) ? "#7E57C2" : task.Color,
            task.DurationMinutes ?? 0,
            task.CurrentStreak,
            async () =>
            {
                if (!task.IsCompleted)
                    await ToggleTask(task);
            });
    }

    // --- Count increment/decrement ---

    [RelayCommand]
    private async Task IncrementCount(ActivityTaskItemViewModel? task)
    {
        if (task is null || !task.IsCountType || _activityApi is null || task.IsToggling) return;

        task.IsToggling = true;
        try
        {
            var newCount = task.CurrentCount + 1;
            var request = new ToggleCompletionRequest
            {
                ActivityItemId = task.Id,
                Date = DateOnly.FromDateTime(DateTime.UtcNow),
                CountValue = newCount
            };

            var result = await _activityApi.ToggleCompletionAsync(request);
            if (result)
            {
                task.CurrentCount = newCount;
                task.IsCompleted = task.TargetCount.HasValue && newCount >= task.TargetCount.Value;
                task.CompletedAtUtc = task.IsCompleted ? DateTime.UtcNow : null;
                task.RefreshCountProperties();
                RecalculateProgress();
                await SyncGroupCompletionAsync();
            }
        }
        finally
        {
            task.IsToggling = false;
        }
    }

    [RelayCommand]
    private async Task DecrementCount(ActivityTaskItemViewModel? task)
    {
        if (task is null || !task.IsCountType || task.CurrentCount <= 0 || _activityApi is null || task.IsToggling) return;

        task.IsToggling = true;
        try
        {
            var newCount = task.CurrentCount - 1;
            var request = new ToggleCompletionRequest
            {
                ActivityItemId = task.Id,
                Date = DateOnly.FromDateTime(DateTime.UtcNow),
                CountValue = newCount
            };

            var result = await _activityApi.ToggleCompletionAsync(request);
            if (result)
            {
                task.CurrentCount = newCount;
                task.IsCompleted = task.TargetCount.HasValue && newCount >= task.TargetCount.Value;
                task.CompletedAtUtc = task.IsCompleted ? DateTime.UtcNow : null;
                task.RefreshCountProperties();
                RecalculateProgress();
                await SyncGroupCompletionAsync();
            }
        }
        finally
        {
            task.IsToggling = false;
        }
    }

    // --- Helpers ---

    private void RecalculateProgress()
    {
        CompletedItems = Groups.Sum(g => g.Tasks.Count(t => t.IsCompleted));
        TotalItems = Groups.Sum(g => g.Tasks.Count);
        OnPropertyChanged(nameof(ProgressText));
        OnPropertyChanged(nameof(ProgressFraction));
        OnPropertyChanged(nameof(ProgressPercent));
        foreach (var group in Groups)
            group.RefreshProgress();
    }

    private async Task SyncGroupCompletionAsync()
    {
        try
        {
            if (_groupCompletionStore is not null)
            {
                var status = new Dictionary<Guid, bool>();
                foreach (var group in Groups)
                {
                    var allCompleted = group.Tasks.Count > 0 && group.Tasks.All(t => t.IsCompleted);
                    status[group.Id] = allCompleted;
                }
                await _groupCompletionStore.SaveCompletionStatusAsync(status);
            }

            if (_blockApiService is not null && _blockRuleStore is not null)
            {
                var rules = await _blockApiService.GetBlockRulesAsync();
                if (rules is not null)
                    await _blockRuleStore.SaveRulesAsync(rules);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"SyncGroupCompletion error: {ex.Message}");
        }
    }
}
