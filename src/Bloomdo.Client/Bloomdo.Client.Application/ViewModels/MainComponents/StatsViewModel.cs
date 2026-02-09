using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows.Input;
using Bloomdo.Client.Application.ViewModels.Items;
using Bloomdo.Client.Core.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Bloomdo.Client.Application.ViewModels.MainComponents;

public partial class StatsViewModel : PageViewModel
{
    [ObservableProperty]
    private string _screenTimeToday = "0m";

    [ObservableProperty]
    private int _pickups = 0;

    [ObservableProperty]
    private bool _isMostUsedPopupVisible;

    public ObservableCollection<MostUsedAppViewModel> MostUsedApps { get; } = [];
    public ObservableCollection<MostUsedAppViewModel> AllAppsUsage { get; } = [];

    [ObservableProperty]
    private ObservableCollection<WeekViewModel> _weeks = [];

    [ObservableProperty]
    private DateTime _currentMonth = DateTime.Today;

    public string CurrentMonthTitle => CurrentMonth.ToString("MMMM yyyy", CultureInfo.CurrentCulture);

    public ICommand PreviousMonthCommand { get; }
    public ICommand NextMonthCommand { get; }
    public ICommand ShowMostUsedPopupCommand { get; }
    public ICommand HideMostUsedPopupCommand { get; }

    private readonly Core.Interfaces.IAppUsageService? _appUsageService;
    private CancellationTokenSource? _cancellationTokenSource;

    public StatsViewModel(Core.Interfaces.IAppUsageService? appUsageService = null)
    {
        _appUsageService = appUsageService;
        PreviousMonthCommand = new RelayCommand(GoToPreviousMonth);
        NextMonthCommand = new RelayCommand(GoToNextMonth);
        ShowMostUsedPopupCommand = new RelayCommand(() => IsMostUsedPopupVisible = true);
        HideMostUsedPopupCommand = new RelayCommand(() => IsMostUsedPopupVisible = false);

        LoadMonthCalendar();
    }

    public override void OnAppearing()
    {
        base.OnAppearing();
        StartRefreshTimer();
    }

    public override void OnDisappearing()
    {
        base.OnDisappearing();
        StopRefreshTimer();
    }

    private void StartRefreshTimer()
    {
        StopRefreshTimer();
        _cancellationTokenSource = new CancellationTokenSource();
        _ = RefreshLoop(_cancellationTokenSource.Token);
    }

    private void StopRefreshTimer()
    {
        _cancellationTokenSource?.Cancel();
        _cancellationTokenSource?.Dispose();
        _cancellationTokenSource = null;
    }

    private async Task RefreshLoop(CancellationToken token)
    {
        try
        {
            await LoadStatsAsync();

            using var timer = new PeriodicTimer(TimeSpan.FromSeconds(3));
            while (await timer.WaitForNextTickAsync(token))
            {
                await LoadStatsAsync();
            }
        }
        catch (OperationCanceledException)
        {
            System.Diagnostics.Debug.WriteLine("RefreshLoop canceled.");
		}
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"RefreshLoop error: {ex}");
        }
    }

    private async Task LoadStatsAsync()
    {
        if (_appUsageService == null) return;

        try
        {
            var (usage, pickups) = await Task.Run(async () =>
            {
                var u = await _appUsageService.GetTodayUsageAsync();
                var p = await _appUsageService.GetPickupsTodayAsync();
                return (u, p);
            });

            Pickups = pickups;

            if (usage.Count == 0)
            {
                ScreenTimeToday = "0m";
                MostUsedApps.Clear();
                AllAppsUsage.Clear();
                return;
            }

            var sortedUsage = usage.OrderByDescending(x => x.ForegroundTime).ToList();
            
            var totalTime = sortedUsage.Aggregate(TimeSpan.Zero, (acc, x) => acc + x.ForegroundTime);
            ScreenTimeToday = FormatDuration(totalTime);

            UpdateAppCollection(AllAppsUsage, sortedUsage);

            UpdateAppCollection(MostUsedApps, sortedUsage.Take(3).ToList());
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"LoadStatsAsync error: {ex}");
        }
    }

    private void UpdateAppCollection(ObservableCollection<MostUsedAppViewModel> collection, List<AppUsageInfo> source)
    {
        var sourceNames = source.Select(x => string.IsNullOrWhiteSpace(x.AppLabel) ? x.PackageName : x.AppLabel!).ToHashSet();
        
        for (var i = collection.Count - 1; i >= 0; i--)
        {
            if (!sourceNames.Contains(collection[i].Name))
            {
                collection.RemoveAt(i);
            }
        }

        for (var i = 0; i < source.Count; i++)
        {
            var app = source[i];
            var name = string.IsNullOrWhiteSpace(app.AppLabel) ? app.PackageName : app.AppLabel!;
            var duration = FormatDuration(app.ForegroundTime);

            var existing = collection.FirstOrDefault(x => x.Name == name);
            if (existing != null)
            {
                if (existing.Duration != duration)
                {
                    existing.Duration = duration;
                }
                
                var oldIndex = collection.IndexOf(existing);
                if (oldIndex != i)
                {
                    collection.Move(oldIndex, i);
                }
            }
            else
            {
                if (i < collection.Count)
                    collection.Insert(i, new MostUsedAppViewModel(name, duration));
                else
                    collection.Add(new MostUsedAppViewModel(name, duration));
            }
        }
        
        while (collection.Count > source.Count)
        {
            collection.RemoveAt(collection.Count - 1);
        }
    }

    private static string FormatDuration(TimeSpan ts)
    {
        if (ts.TotalHours >= 1)
        {
            return $"{(int)ts.TotalHours}h {ts.Minutes}m";
        }
        return $"{ts.Minutes}m";
    }

    private void LoadMonthCalendar()
    {
        var year = CurrentMonth.Year;
        var month = CurrentMonth.Month;
        
        var firstDayOfMonth = new DateTime(year, month, 1);
        var daysInMonth = DateTime.DaysInMonth(year, month);
        
        var firstDayOfWeek = (int)firstDayOfMonth.DayOfWeek;
        firstDayOfWeek = firstDayOfWeek == 0 ? 6 : firstDayOfWeek - 1;
        
        var daysFromPrevMonth = firstDayOfWeek;
        var prevMonth = CurrentMonth.AddMonths(-1);
        var daysInPrevMonth = DateTime.DaysInMonth(prevMonth.Year, prevMonth.Month);
        
        var totalDays = daysFromPrevMonth + daysInMonth;
        var remainingDays = (7 - (totalDays % 7)) % 7;
        var capacity = totalDays + remainingDays;
        
        var calendarDays = new List<DayStatViewModel>(capacity);
        
        for (var i = daysFromPrevMonth - 1; i >= 0; i--)
        {
            var day = daysInPrevMonth - i;
            var date = new DateTime(prevMonth.Year, prevMonth.Month, day);
            calendarDays.Add(new DayStatViewModel(day, GetStatusIconForDate(date), false, date));
        }
        
        for (var day = 1; day <= daysInMonth; day++)
        {
            var date = new DateTime(year, month, day);
            calendarDays.Add(new DayStatViewModel(day, GetStatusIconForDate(date), true, date));
        }
        
        if (remainingDays > 0)
        {
            var nextMonth = CurrentMonth.AddMonths(1);
            for (var day = 1; day <= remainingDays; day++)
            {
                var date = new DateTime(nextMonth.Year, nextMonth.Month, day);
                calendarDays.Add(new DayStatViewModel(day, GetStatusIconForDate(date), false, date));
            }
        }
        
        var weeksList = new List<WeekViewModel>(capacity / 7);
        for (var i = 0; i < calendarDays.Count; i += 7)
        {
            var week = new WeekViewModel();
            for (var j = 0; j < 7 && i + j < calendarDays.Count; j++)
            {
                week.Days.Add(calendarDays[i + j]);
            }
            weeksList.Add(week);
        }
        
        Weeks = new ObservableCollection<WeekViewModel>(weeksList);
        OnPropertyChanged(nameof(CurrentMonthTitle));
    }

    private void GoToPreviousMonth()
    {
        CurrentMonth = CurrentMonth.AddMonths(-1);
        LoadMonthCalendar();
    }

    private void GoToNextMonth()
    {
        CurrentMonth = CurrentMonth.AddMonths(1);
        LoadMonthCalendar();
    }

    private static string? GetStatusIconForDate(DateTime date)
    {
        // Placeholder for future images
        return null;
    }
}
