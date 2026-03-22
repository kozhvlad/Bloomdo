using System.Collections.ObjectModel;
using Bloomdo.Shared.DTOs.Activities;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Bloomdo.Client.Application.ViewModels.Items;

public partial class ActivityGroupItemViewModel : ObservableObject
{
    [ObservableProperty]
    private Guid _id;

    [ObservableProperty]
    private string _title = string.Empty;

    [ObservableProperty]
    private string _icon = string.Empty;

    [ObservableProperty]
    private string _color = "#7E57C2";

    [ObservableProperty]
    private int _currentStreak;

    [ObservableProperty]
    private bool _isExpanded = true;

    [ObservableProperty]
    private bool _isEditing;

    [ObservableProperty]
    private string _editTitle = string.Empty;

    [ObservableProperty]
    private bool _isAddingItem;

    [ObservableProperty]
    private string _newItemTitle = string.Empty;

    [ObservableProperty]
    private string? _newItemDescription;

    [ObservableProperty]
    private int _newItemDurationMinutes = 30;

    [ObservableProperty]
    private string _newItemIcon = string.Empty;

    [ObservableProperty]
    private string _newItemColor = "#7E57C2";

    [ObservableProperty]
    private ActivityItemType _newItemTaskType = ActivityItemType.Timer;

    [ObservableProperty]
    private int _newItemTargetCount = 8;

    // Computed type checks for the new item form
    public bool IsNewItemTimerType => NewItemTaskType == ActivityItemType.Timer;
    public bool IsNewItemCountType => NewItemTaskType == ActivityItemType.Count;
    public bool IsNewItemStepsType => NewItemTaskType == ActivityItemType.Steps;
    public bool IsNewItemCheckboxType => NewItemTaskType == ActivityItemType.Checkbox;

    public ObservableCollection<ActivityTaskItemViewModel> Tasks { get; } = [];

    public string FirstLetter =>
        string.IsNullOrEmpty(Title) ? "?" : Title[..1].ToUpperInvariant();

    public int CompletedCount => Tasks.Count(t => t.IsCompleted);
    public int TotalCount => Tasks.Count;
    public string ProgressText => $"{CompletedCount}/{TotalCount}";
    public double ProgressPercent => TotalCount > 0 ? (double)CompletedCount / TotalCount * 100 : 0;
    public bool HasStreak => CurrentStreak > 0;
    public string StreakText => CurrentStreak > 0 ? $"x{CurrentStreak}" : string.Empty;

    public void RefreshProgress()
    {
        OnPropertyChanged(nameof(CompletedCount));
        OnPropertyChanged(nameof(TotalCount));
        OnPropertyChanged(nameof(ProgressText));
        OnPropertyChanged(nameof(ProgressPercent));
    }

    public void ResetNewItemForm()
    {
        NewItemTitle = string.Empty;
        NewItemDescription = null;
        NewItemTaskType = ActivityItemType.Timer;
        NewItemDurationMinutes = 30;
        NewItemTargetCount = 8;
        NewItemIcon = string.Empty;
        NewItemColor = "#7E57C2";
    }

    partial void OnTitleChanged(string value) => OnPropertyChanged(nameof(FirstLetter));

    partial void OnNewItemTaskTypeChanged(ActivityItemType value)
    {
        OnPropertyChanged(nameof(IsNewItemTimerType));
        OnPropertyChanged(nameof(IsNewItemCountType));
        OnPropertyChanged(nameof(IsNewItemStepsType));
        OnPropertyChanged(nameof(IsNewItemCheckboxType));

        // Set sensible defaults when switching types
        if (value == ActivityItemType.Steps && NewItemTargetCount < 1000)
            NewItemTargetCount = 10000;
        else if (value == ActivityItemType.Count && NewItemTargetCount > 100)
            NewItemTargetCount = 8;
    }
}
