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
    private readonly IPhotoVerificationDialogService? _photoVerificationDialogService;
    private readonly IToastService? _toastService;
    private readonly IConnectivityService? _connectivityService;
    private readonly ILocalActivityCache? _localActivityCache;
    private readonly ITimerStateStore? _timerStateStore;

    private readonly SynchronizationContext? _uiContext;
    private CancellationTokenSource? _timerTickCts;
    private bool _timerEventsHooked;

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

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private bool _isOffline;

    private bool? _sortDirection;
    private List<ActivityGroupItemViewModel> _allGroups = [];

    public string SortButtonText => _sortDirection switch
    {
        true => "A → Z",
        false => "Z → A",
        _ => "Sort"
    };

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
        IConfirmDialogService? confirmDialogService = null,
        IPhotoVerificationDialogService? photoVerificationDialogService = null,
        IToastService? toastService = null,
        IConnectivityService? connectivityService = null,
        ILocalActivityCache? localActivityCache = null,
        ITimerStateStore? timerStateStore = null)
    {
        _activityApi = activityApi;
        _groupCompletionStore = groupCompletionStore;
        _blockRuleStore = blockRuleStore;
        _blockApiService = blockApiService;
        _navigationService = navigationService;
        _timerDialogService = timerDialogService;
        _confirmDialogService = confirmDialogService;
        _photoVerificationDialogService = photoVerificationDialogService;
        _toastService = toastService;
        _connectivityService = connectivityService;
        _localActivityCache = localActivityCache;
        _timerStateStore = timerStateStore;
        _uiContext = SynchronizationContext.Current;
    }

    public override void OnAppearing()
    {
        base.OnAppearing();
        TodayDateText = DateTime.Now.ToString("dddd, MMMM d");
        _ = LoadDailyActivitiesAsync();

        HookTimerEvents();
        StartTimerTickLoop();
    }

    public override void OnDisappearing()
    {
        base.OnDisappearing();
        StopTimerTickLoop();
        UnhookTimerEvents();
    }

    // --- Active-timer indicators on task cards ---

    private void HookTimerEvents()
    {
        if (_timerEventsHooked || _timerDialogService is null) return;
        _timerDialogService.TimerStateChanged += OnTimerStateChanged;
        _timerEventsHooked = true;
    }

    private void UnhookTimerEvents()
    {
        if (!_timerEventsHooked || _timerDialogService is null) return;
        _timerDialogService.TimerStateChanged -= OnTimerStateChanged;
        _timerEventsHooked = false;
    }

    private void OnTimerStateChanged() => _ = RefreshActiveTimersAsync();

    private async Task RefreshActiveTimersAsync()
    {
        if (_timerStateStore is null) return;

        List<Domain.Models.TimerStateSnapshot> active;
        try
        {
            active = await _timerStateStore.GetAllActiveAsync();
        }
        catch
        {
            return;
        }

        var byTaskId = active.ToDictionary(s => s.TaskId);

        PostToUi(() =>
        {
            foreach (var group in _allGroups)
            {
                foreach (var task in group.Tasks)
                {
                    if (byTaskId.TryGetValue(task.Id, out var snap))
                    {
                        var remaining = snap.RemainingSeconds;
                        if (snap.IsRunning && !snap.IsPaused)
                        {
                            // Account for real elapsed time since the snapshot was taken
                            var elapsed = (int)(DateTime.UtcNow - snap.LastTickUtc).TotalSeconds;
                            remaining = Math.Max(0, remaining - elapsed);
                        }

                        if (remaining <= 0)
                        {
                            task.IsTimerRunning = false;
                            task.IsTimerPaused = false;
                            task.TimerRemainingSeconds = 0;
                        }
                        else
                        {
                            task.IsTimerRunning = snap.IsRunning && !snap.IsPaused;
                            task.IsTimerPaused = snap.IsPaused;
                            task.TimerRemainingSeconds = remaining;
                        }
                    }
                    else if (task.IsTimerRunning || task.IsTimerPaused)
                    {
                        task.IsTimerRunning = false;
                        task.IsTimerPaused = false;
                        task.TimerRemainingSeconds = 0;
                    }
                }
            }
        });
    }

    private void StartTimerTickLoop()
    {
        StopTimerTickLoop();
        _timerTickCts = new CancellationTokenSource();
        _ = RunTimerTickLoopAsync(_timerTickCts.Token);
    }

    private void StopTimerTickLoop()
    {
        _timerTickCts?.Cancel();
        _timerTickCts = null;
    }

    private async Task RunTimerTickLoopAsync(CancellationToken ct)
    {
        try
        {
            while (!ct.IsCancellationRequested)
            {
                await Task.Delay(1000, ct);
                PostToUi(TickRunningTimers);
            }
        }
        catch (OperationCanceledException) { }
    }

    private void TickRunningTimers()
    {
        foreach (var group in _allGroups)
        {
            foreach (var task in group.Tasks)
            {
                if (!task.IsTimerRunning || task.IsTimerPaused) continue;
                if (task.TimerRemainingSeconds <= 0) continue;

                task.TimerRemainingSeconds--;
                if (task.TimerRemainingSeconds <= 0)
                {
                    task.IsTimerRunning = false;
                }
            }
        }
    }

    private void PostToUi(Action action)
    {
        if (_uiContext is null) action();
        else _uiContext.Post(_ => action(), null);
    }

    [RelayCommand]
    private async Task LoadDailyActivitiesAsync()
    {
        if (_activityApi is null) return;

        IsLoading = true;
        IsOffline = _connectivityService is not null && !_connectivityService.IsOnline;

        try
        {
            DailyActivitiesResponse? daily = null;

            // Try server first
            if (!IsOffline)
            {
                try
                {
                    daily = await _activityApi.GetDailyAsync();

                    // Cache for offline use
                    if (daily is not null && _localActivityCache is not null)
                        await _localActivityCache.SaveDailyAsync(daily, DateOnly.FromDateTime(DateTime.Today));
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
            if (daily is null && _localActivityCache is not null)
                daily = await _localActivityCache.LoadDailyAsync(DateOnly.FromDateTime(DateTime.Today));

            if (daily is null) return;

            _allGroups.Clear();
            foreach (var group in daily.Groups)
            {
                var vm = new ActivityGroupItemViewModel
                {
                    Id = group.Id,
                    Title = group.Title,
                    Icon = group.Icon,
                    Color = group.Color,
                    CurrentStreak = group.CurrentStreak,
                    IsShared = group.IsShared
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
                        CompletedAtUtc = item.CompletedAtUtc,
                        VerificationTemplate = item.VerificationTemplate,
                        CustomVerificationCriteria = item.CustomVerificationCriteria
                    });
                }

                _allGroups.Add(vm);
            }

            TotalItems = daily.TotalItems;
            CompletedItems = daily.CompletedItems;
            ApplyFilterAndSort();
            OnPropertyChanged(nameof(ProgressText));
            OnPropertyChanged(nameof(ProgressPercent));
            OnPropertyChanged(nameof(ProgressFraction));

            // Apply per-task running-timer indicators (now that _allGroups is built)
            _ = RefreshActiveTimersAsync();

            if (!IsOffline)
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

        // PhotoVerification tasks can only be completed via photo — block manual toggle to completed
        if (task.IsPhotoVerificationType && !task.IsCompleted) return;

        task.IsToggling = true;
        try
        {
            var request = new ToggleCompletionRequest
            {
                ActivityItemId = task.Id,
                Date = DateOnly.FromDateTime(DateTime.UtcNow)
            };

            var isOnline = _connectivityService?.IsOnline ?? true;

            if (isOnline)
            {
                try
                {
                    var result = await _activityApi.ToggleCompletionAsync(request);
                    if (result)
                    {
                        task.IsCompleted = !task.IsCompleted;
                        task.CompletedAtUtc = task.IsCompleted ? DateTime.UtcNow : null;

                        RecalculateProgress();
                        await SyncGroupCompletionAsync();
                    }
                }
                catch (HttpRequestException)
                {
                    // Network failed mid-request — apply optimistically and queue
                    await ApplyToggleOfflineAsync(task, request);
                }
                catch (TaskCanceledException)
                {
                    await ApplyToggleOfflineAsync(task, request);
                }
            }
            else
            {
                await ApplyToggleOfflineAsync(task, request);
            }
        }
        finally
        {
            task.IsToggling = false;
        }
    }

    private async Task ApplyToggleOfflineAsync(ActivityTaskItemViewModel task, ToggleCompletionRequest request)
    {
        task.IsCompleted = !task.IsCompleted;
        task.CompletedAtUtc = task.IsCompleted ? DateTime.UtcNow : null;
        RecalculateProgress();

        if (_localActivityCache is not null)
        {
            await _localActivityCache.EnqueueToggleAsync(new PendingActivityToggle
            {
                ActivityItemId = request.ActivityItemId,
                Date = request.Date,
                CountValue = request.CountValue,
                CreatedAtUtc = DateTime.UtcNow
            });
        }

        _toastService?.ShowInfo("Saved offline — will sync when connected");
    }

    // --- Photo verification ---

    [RelayCommand]
    private void OpenPhotoVerification(ActivityTaskItemViewModel? task)
    {
        if (task is null) return;

        if (task.IsCompleted)
        {
            _toastService?.ShowInfo("Already verified today");
            return;
        }

        var date = DateOnly.FromDateTime(DateTime.Today);
        _photoVerificationDialogService?.Show(task.Id, task.VerificationTemplate, task.CustomVerificationCriteria, date, () => _ = LoadDailyActivitiesAsync());
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
            var newVm = new ActivityGroupItemViewModel
            {
                Id = result.Id,
                Title = result.Title,
                Icon = result.Icon,
                Color = result.Color,
                CurrentStreak = 0
            };
            _allGroups.Add(newVm);
            ApplyFilterAndSort();
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
            var taskCount = group.Tasks.Count;
            var message = taskCount > 0
                ? $"Delete \"{group.Title}\"? This will also delete {taskCount} task{(taskCount == 1 ? "" : "s")} inside."
                : $"Delete \"{group.Title}\"?";
            var confirmed = await _confirmDialogService.ConfirmAsync("Delete Group", message);
            if (!confirmed) return;
        }

        var deleted = await _activityApi.DeleteGroupAsync(group.Id);
        if (deleted)
        {
            _toastService?.ShowSuccess("Group deleted");
            _ = LoadDailyActivitiesAsync();
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
        if (group is null || group.IsShared) return;
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
    private void SelectNewItemTaskType(string? type)
    {
        if (type is null) return;
        var taskType = type switch
        {
            "Count" => ActivityItemType.Count,
            "Steps" => ActivityItemType.Steps,
            "Checkbox" => ActivityItemType.Checkbox,
            "Photo" => ActivityItemType.PhotoVerification,
            _ => ActivityItemType.Timer
        };
        foreach (var group in Groups)
        {
            if (group.IsAddingItem)
            {
                group.NewItemTaskType = taskType;
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
    private void IncrementNewItemTargetCount(ActivityGroupItemViewModel? group)
    {
        if (group is null) return;
        group.NewItemTargetCount = Math.Min(group.NewItemTargetCount + 1, 999);
    }

    [RelayCommand]
    private void DecrementNewItemTargetCount(ActivityGroupItemViewModel? group)
    {
        if (group is null) return;
        group.NewItemTargetCount = Math.Max(group.NewItemTargetCount - 1, 1);
    }

    [RelayCommand]
    private void IncrementNewItemTargetSteps(ActivityGroupItemViewModel? group)
    {
        if (group is null) return;
        group.NewItemTargetCount = Math.Min(group.NewItemTargetCount + 1000, 100000);
    }

    [RelayCommand]
    private void DecrementNewItemTargetSteps(ActivityGroupItemViewModel? group)
    {
        if (group is null) return;
        group.NewItemTargetCount = Math.Max(group.NewItemTargetCount - 1000, 1000);
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
            TaskType = group.NewItemTaskType,
            DurationMinutes = group.IsNewItemTimerType ? group.NewItemDurationMinutes : null,
            TargetCount = (group.IsNewItemCountType || group.IsNewItemStepsType) ? group.NewItemTargetCount : null,
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

        // Block deletion for shared group tasks
        var parentGroup = Groups.FirstOrDefault(g => g.Tasks.Contains(task));
        if (parentGroup?.IsShared == true) return;

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
            _toastService?.ShowSuccess("Task deleted");
            _ = LoadDailyActivitiesAsync();
        }
    }

    // --- Edit item ---

    [RelayCommand]
    private void StartEditItem(ActivityTaskItemViewModel? task)
    {
        if (task is null) return;

        // Block editing for shared group tasks
        var parentGroup = Groups.FirstOrDefault(g => g.Tasks.Contains(task));
        if (parentGroup?.IsShared == true) return;

        _navigationService?.NavigateTo<TaskEditorViewModel>(vm => vm.ConfigureForEdit(task));
    }

    // --- Timer ---

    [RelayCommand]
    private void StartTimer(ActivityTaskItemViewModel? task)
    {
        if (task is null || !task.HasDuration) return;

        _timerDialogService?.ShowTimer(
            task.Id,
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

            var isOnline = _connectivityService?.IsOnline ?? true;

            if (isOnline)
            {
                try
                {
                    var result = await _activityApi.ToggleCompletionAsync(request);
                    if (result)
                    {
                        ApplyCountChange(task, newCount);
                    }
                }
                catch (HttpRequestException)
                {
                    await ApplyCountOfflineAsync(task, newCount, request);
                }
                catch (TaskCanceledException)
                {
                    await ApplyCountOfflineAsync(task, newCount, request);
                }
            }
            else
            {
                await ApplyCountOfflineAsync(task, newCount, request);
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

            var isOnline = _connectivityService?.IsOnline ?? true;

            if (isOnline)
            {
                try
                {
                    var result = await _activityApi.ToggleCompletionAsync(request);
                    if (result)
                    {
                        ApplyCountChange(task, newCount);
                    }
                }
                catch (HttpRequestException)
                {
                    await ApplyCountOfflineAsync(task, newCount, request);
                }
                catch (TaskCanceledException)
                {
                    await ApplyCountOfflineAsync(task, newCount, request);
                }
            }
            else
            {
                await ApplyCountOfflineAsync(task, newCount, request);
            }
        }
        finally
        {
            task.IsToggling = false;
        }
    }

    // --- Steps increment/decrement ---

    [RelayCommand]
    private async Task IncrementSteps(ActivityTaskItemViewModel? task)
    {
        if (task is null || !task.IsStepsType || _activityApi is null || task.IsToggling) return;

        task.IsToggling = true;
        try
        {
            var newCount = task.CurrentCount + 1000;
            var request = new ToggleCompletionRequest
            {
                ActivityItemId = task.Id,
                Date = DateOnly.FromDateTime(DateTime.UtcNow),
                CountValue = newCount
            };

            var isOnline = _connectivityService?.IsOnline ?? true;

            if (isOnline)
            {
                try
                {
                    var result = await _activityApi.ToggleCompletionAsync(request);
                    if (result)
                    {
                        ApplyCountChange(task, newCount);
                    }
                }
                catch (HttpRequestException)
                {
                    await ApplyCountOfflineAsync(task, newCount, request);
                }
                catch (TaskCanceledException)
                {
                    await ApplyCountOfflineAsync(task, newCount, request);
                }
            }
            else
            {
                await ApplyCountOfflineAsync(task, newCount, request);
            }
        }
        finally
        {
            task.IsToggling = false;
        }
    }

    [RelayCommand]
    private async Task DecrementSteps(ActivityTaskItemViewModel? task)
    {
        if (task is null || !task.IsStepsType || task.CurrentCount <= 0 || _activityApi is null || task.IsToggling) return;

        task.IsToggling = true;
        try
        {
            var newCount = Math.Max(task.CurrentCount - 1000, 0);
            var request = new ToggleCompletionRequest
            {
                ActivityItemId = task.Id,
                Date = DateOnly.FromDateTime(DateTime.UtcNow),
                CountValue = newCount
            };

            var isOnline = _connectivityService?.IsOnline ?? true;

            if (isOnline)
            {
                try
                {
                    var result = await _activityApi.ToggleCompletionAsync(request);
                    if (result)
                    {
                        ApplyCountChange(task, newCount);
                    }
                }
                catch (HttpRequestException)
                {
                    await ApplyCountOfflineAsync(task, newCount, request);
                }
                catch (TaskCanceledException)
                {
                    await ApplyCountOfflineAsync(task, newCount, request);
                }
            }
            else
            {
                await ApplyCountOfflineAsync(task, newCount, request);
            }
        }
        finally
        {
            task.IsToggling = false;
        }
    }

    private void ApplyCountChange(ActivityTaskItemViewModel task, int newCount)
    {
        task.CurrentCount = newCount;
        task.IsCompleted = task.TargetCount.HasValue && newCount >= task.TargetCount.Value;
        task.CompletedAtUtc = task.IsCompleted ? DateTime.UtcNow : null;
        task.RefreshCountProperties();
        RecalculateProgress();
    }

    private async Task ApplyCountOfflineAsync(ActivityTaskItemViewModel task, int newCount, ToggleCompletionRequest request)
    {
        ApplyCountChange(task, newCount);

        if (_localActivityCache is not null)
        {
            await _localActivityCache.EnqueueToggleAsync(new PendingActivityToggle
            {
                ActivityItemId = request.ActivityItemId,
                Date = request.Date,
                CountValue = request.CountValue,
                CreatedAtUtc = DateTime.UtcNow
            });
        }

        _toastService?.ShowInfo("Saved offline — will sync when connected");
    }

    // --- Helpers ---

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

    [RelayCommand]
    private void ToggleAllExpanded()
    {
        var shouldExpand = Groups.Any(g => !g.IsExpanded);
        foreach (var g in Groups)
            g.IsExpanded = shouldExpand;
    }

    private void ApplyFilterAndSort()
    {
        var filtered = _allGroups.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            var search = SearchText.Trim();
            filtered = filtered.Where(g =>
                g.Title.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                g.Tasks.Any(t =>
                    t.Title.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                    (!string.IsNullOrEmpty(t.Description) && t.Description.Contains(search, StringComparison.OrdinalIgnoreCase))));
        }

        filtered = _sortDirection switch
        {
            true => filtered.OrderBy(g => g.Title, StringComparer.OrdinalIgnoreCase),
            false => filtered.OrderByDescending(g => g.Title, StringComparer.OrdinalIgnoreCase),
            _ => filtered
        };

        Groups.Clear();
        foreach (var g in filtered)
            Groups.Add(g);

        HasNoGroups = Groups.Count == 0;
    }

    private void RecalculateProgress()
    {
        CompletedItems = _allGroups.Sum(g => g.Tasks.Count(t => t.IsCompleted));
        TotalItems = _allGroups.Sum(g => g.Tasks.Count);
        OnPropertyChanged(nameof(ProgressText));
        OnPropertyChanged(nameof(ProgressFraction));
        OnPropertyChanged(nameof(ProgressPercent));
        foreach (var group in _allGroups)
            group.RefreshProgress();
    }

    private async Task SyncGroupCompletionAsync()
    {
        try
        {
            if (_groupCompletionStore is not null)
            {
                var status = new Dictionary<Guid, bool>();
                foreach (var group in _allGroups)
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
