using System.Collections.ObjectModel;
using System.Globalization;
using Bloomdo.Client.Application.ViewModels.Items;
using Bloomdo.Client.Core.Interfaces;
using Bloomdo.Client.Domain.Models;
using Bloomdo.Shared.DTOs.Stats;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Bloomdo.Client.Application.ViewModels.MainComponents;

public partial class StatsViewModel : PageViewModel
{
	private readonly IAppUsageService? _appUsageService;
	private readonly IStatsApiService? _statsApiService;
	private CancellationTokenSource? _cancellationTokenSource;

	// Calendar data from server, keyed by DateOnly
	private Dictionary<DateOnly, CalendarDayDto> _calendarData = [];

	[ObservableProperty]
	private string _screenTimeToday = "0m";

	[ObservableProperty]
	private int _pickups;

	[ObservableProperty]
	private bool _isMostUsedPopupVisible;

	[ObservableProperty]
	private ObservableCollection<WeekViewModel> _weeks = [];

	[ObservableProperty]
	private DateTime _currentMonth = DateTime.Today;

	[ObservableProperty]
	private DayStatViewModel? _selectedDay;

	[ObservableProperty]
	private string _selectedDayScreenTime = "0m";

	[ObservableProperty]
	private int _selectedDayPickups;

	[ObservableProperty]
	private bool _selectedDayGoalMet;

	[ObservableProperty]
	private bool _isSelectedDayDetailVisible;

	[ObservableProperty]
	private int _currentStreak;

	[ObservableProperty]
	private int _longestStreak;

	public ObservableCollection<MostUsedAppViewModel> MostUsedApps { get; } = [];
	public ObservableCollection<MostUsedAppViewModel> AllAppsUsage { get; } = [];
	public ObservableCollection<MostUsedAppViewModel> SelectedDayApps { get; } = [];

	public string CurrentMonthTitle => CurrentMonth.ToString("MMMM yyyy", CultureInfo.CurrentCulture);

	public StatsViewModel(IAppUsageService? appUsageService = null, IStatsApiService? statsApiService = null)
	{
		_appUsageService = appUsageService;
		_statsApiService = statsApiService;
		LoadMonthCalendar();
	}

	public override void OnAppearing()
	{
		base.OnAppearing();
		StartRefreshTimer();
		_ = LoadCalendarFromServerAsync();
	}

	public override void OnDisappearing()
	{
		base.OnDisappearing();
		StopRefreshTimer();
	}

	[RelayCommand]
	private void GoToPreviousMonth()
	{
		CurrentMonth = CurrentMonth.AddMonths(-1);
		LoadMonthCalendar();
		_ = LoadCalendarFromServerAsync();
	}

	[RelayCommand]
	private void GoToNextMonth()
	{
		CurrentMonth = CurrentMonth.AddMonths(1);
		LoadMonthCalendar();
		_ = LoadCalendarFromServerAsync();
	}

	[RelayCommand]
	private void ShowMostUsedPopup() => IsMostUsedPopupVisible = true;

	[RelayCommand]
	private void HideMostUsedPopup() => IsMostUsedPopupVisible = false;

	[RelayCommand]
	private void SelectDay(DayStatViewModel? day)
	{
		if (day is null || !day.IsCurrentMonth) return;

		// Deselect previous
		if (SelectedDay is not null)
			SelectedDay.IsSelected = false;

		day.IsSelected = true;
		SelectedDay = day;
		IsSelectedDayDetailVisible = true;

		// Load stats for this day
		_ = LoadSelectedDayStatsAsync(DateOnly.FromDateTime(day.Date));
	}

	[RelayCommand]
	private void GoToToday()
	{
		CurrentMonth = DateTime.Today;
		LoadMonthCalendar();
		_ = LoadCalendarFromServerAsync();

		// Select today
		var todayVm = FindDayViewModel(DateTime.Today);
		if (todayVm is not null)
			SelectDay(todayVm);
	}

	[RelayCommand]
	private void CloseSelectedDayDetail()
	{
		IsSelectedDayDetailVisible = false;
		if (SelectedDay is not null)
			SelectedDay.IsSelected = false;
		SelectedDay = null;
	}

	private DayStatViewModel? FindDayViewModel(DateTime date)
	{
		foreach (var week in Weeks)
		{
			foreach (var day in week.Days)
			{
				if (day.Date.Date == date.Date)
					return day;
			}
		}
		return null;
	}

	#region Local Stats Refresh

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
			await LoadLocalStatsAsync();

			using var timer = new PeriodicTimer(TimeSpan.FromSeconds(3));
			while (await timer.WaitForNextTickAsync(token))
			{
				await LoadLocalStatsAsync();
			}
		}
		catch (OperationCanceledException) { }
		catch (Exception ex)
		{
			System.Diagnostics.Debug.WriteLine($"RefreshLoop error: {ex}");
		}
	}

	private async Task LoadLocalStatsAsync()
	{
		if (_appUsageService is null) return;

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

			// Sync to server periodically (every 5 minutes handled by separate sync logic)
		}
		catch (Exception ex)
		{
			System.Diagnostics.Debug.WriteLine($"LoadLocalStatsAsync error: {ex}");
		}
	}

	private static void UpdateAppCollection(ObservableCollection<MostUsedAppViewModel> collection, List<AppUsageInfo> source)
	{
		var sourceNames = source.Select(x => string.IsNullOrWhiteSpace(x.AppLabel) ? x.PackageName : x.AppLabel!).ToHashSet();

		for (var i = collection.Count - 1; i >= 0; i--)
		{
			if (!sourceNames.Contains(collection[i].Name))
				collection.RemoveAt(i);
		}

		for (var i = 0; i < source.Count; i++)
		{
			var app = source[i];
			var name = string.IsNullOrWhiteSpace(app.AppLabel) ? app.PackageName : app.AppLabel!;
			var duration = FormatDuration(app.ForegroundTime);

			var existing = collection.FirstOrDefault(x => x.Name == name);
			if (existing is not null)
			{
				if (existing.Duration != duration)
					existing.Duration = duration;

				var oldIndex = collection.IndexOf(existing);
				if (oldIndex != i)
					collection.Move(oldIndex, i);
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
			collection.RemoveAt(collection.Count - 1);
	}

	#endregion

	#region Server Calendar & Day Detail

	private async Task LoadCalendarFromServerAsync()
	{
		if (_statsApiService is null) return;

		try
		{
			var calendar = await _statsApiService.GetMonthCalendarAsync(CurrentMonth.Year, CurrentMonth.Month);
			if (calendar is null) return;

			CurrentStreak = calendar.CurrentStreak;
			LongestStreak = calendar.LongestStreak;

			_calendarData = calendar.Days.ToDictionary(d => d.Date);
			ApplyStreakDataToCalendar();
		}
		catch (Exception ex)
		{
			System.Diagnostics.Debug.WriteLine($"LoadCalendarFromServer error: {ex}");
		}
	}

	private void ApplyStreakDataToCalendar()
	{
		// Flatten all days and mark streak info
		var allDays = Weeks.SelectMany(w => w.Days).Where(d => d.IsCurrentMonth).OrderBy(d => d.Date).ToList();

		// Mark goal-met days
		var goalMetDates = new HashSet<DateOnly>(_calendarData.Where(kv => kv.Value.GoalMet).Select(kv => kv.Key));

		foreach (var day in allDays)
		{
			var dateOnly = DateOnly.FromDateTime(day.Date);
			var isGoalMet = goalMetDates.Contains(dateOnly);
			day.IsStreakDay = isGoalMet;

			if (_calendarData.TryGetValue(dateOnly, out var data))
				day.TotalScreenTimeSeconds = data.TotalScreenTimeSeconds;
		}

		// Calculate streak segments (consecutive goal-met days)
		for (var i = 0; i < allDays.Count; i++)
		{
			if (!allDays[i].IsStreakDay)
			{
				allDays[i].IsStreakStart = false;
				allDays[i].IsStreakEnd = false;
				allDays[i].IsStreakMiddle = false;
				continue;
			}

			var prevIsStreak = i > 0 && allDays[i - 1].IsStreakDay;
			var nextIsStreak = i < allDays.Count - 1 && allDays[i + 1].IsStreakDay;

			allDays[i].IsStreakStart = !prevIsStreak && nextIsStreak;
			allDays[i].IsStreakEnd = prevIsStreak && !nextIsStreak;
			allDays[i].IsStreakMiddle = prevIsStreak && nextIsStreak;

			// Lone streak day (single day)
			if (!prevIsStreak && !nextIsStreak)
			{
				allDays[i].IsStreakStart = true;
				allDays[i].IsStreakEnd = true;
				allDays[i].IsStreakMiddle = false;
			}
		}
	}

	private async Task LoadSelectedDayStatsAsync(DateOnly date)
	{
		SelectedDayApps.Clear();
		SelectedDayScreenTime = "0m";
		SelectedDayPickups = 0;
		SelectedDayGoalMet = false;

		if (_statsApiService is null) return;

		try
		{
			var stats = await _statsApiService.GetDailyStatsAsync(date);
			if (stats is null) return;

			SelectedDayScreenTime = FormatDuration(TimeSpan.FromSeconds(stats.TotalScreenTimeSeconds));
			SelectedDayPickups = stats.Pickups;
			SelectedDayGoalMet = stats.GoalMet;

			foreach (var app in stats.Apps)
			{
				var name = string.IsNullOrWhiteSpace(app.AppLabel) ? app.PackageName : app.AppLabel;
				var duration = FormatDuration(TimeSpan.FromSeconds(app.ForegroundSeconds));
				SelectedDayApps.Add(new MostUsedAppViewModel(name, duration));
			}
		}
		catch (Exception ex)
		{
			System.Diagnostics.Debug.WriteLine($"LoadSelectedDayStats error: {ex}");
		}
	}

	#endregion

	#region Calendar Building

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
			calendarDays.Add(new DayStatViewModel(day, false, date));
		}

		for (var day = 1; day <= daysInMonth; day++)
		{
			var date = new DateTime(year, month, day);
			calendarDays.Add(new DayStatViewModel(day, true, date));
		}

		if (remainingDays > 0)
		{
			var nextMonth = CurrentMonth.AddMonths(1);
			for (var day = 1; day <= remainingDays; day++)
			{
				var date = new DateTime(nextMonth.Year, nextMonth.Month, day);
				calendarDays.Add(new DayStatViewModel(day, false, date));
			}
		}

		var weeksList = new List<WeekViewModel>(capacity / 7);
		for (var i = 0; i < calendarDays.Count; i += 7)
		{
			var week = new WeekViewModel();
			for (var j = 0; j < 7 && i + j < calendarDays.Count; j++)
				week.Days.Add(calendarDays[i + j]);
			weeksList.Add(week);
		}

		Weeks = new ObservableCollection<WeekViewModel>(weeksList);
		OnPropertyChanged(nameof(CurrentMonthTitle));
	}

	#endregion

	private static string FormatDuration(TimeSpan ts)
	{
		if (ts.TotalHours >= 1)
			return $"{(int)ts.TotalHours}h {ts.Minutes}m";
		return $"{ts.Minutes}m";
	}
}
