using System.Collections.ObjectModel;
using System.Globalization;
using Bloomdo.Client.Application.ViewModels.Items;
using Bloomdo.Client.Core.Interfaces;
using Bloomdo.Client.Domain.Models;
using Bloomdo.Shared.DTOs.Stats;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Bloomdo.Shared.DTOs.Subscription;

namespace Bloomdo.Client.Application.ViewModels.MainComponents;

public partial class StatsViewModel : PageViewModel
{
	private readonly IAppUsageService? _appUsageService;
	private readonly IStatsApiService? _statsApiService;
	private readonly IAppIconProvider? _appIconProvider;
	private readonly ISubscriptionApiService? _subscriptionApiService;
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

	// Weekly Chart Properties
	[ObservableProperty]
	private ObservableCollection<DayChartBarViewModel> _weeklyChartData = [];

	[ObservableProperty]
	private string _weeklyTotalTime = "0m";

	[ObservableProperty]
	private string _weeklyAverageTime = "0m";

	[ObservableProperty]
	private int _weeklyTotalPickups;

	[ObservableProperty]
	private int _weeklyAveragePickups;

	[ObservableProperty]
	private double _screenTimeChangePercent;

	[ObservableProperty]
	private int _screenTimeChangeSeconds;

	[ObservableProperty]
	private bool _isImproving;

	[ObservableProperty]
	private bool _hasComparison;

	[ObservableProperty]
	private string _comparisonText = "";

	[ObservableProperty]
	[NotifyCanExecuteChangedFor(nameof(GoToNextWeekCommand))]
	[NotifyPropertyChangedFor(nameof(WeekRangeTitle))]
	private DateOnly _currentWeekStart;

	[ObservableProperty]
	private bool _isPremium = true;

	public ObservableCollection<MostUsedAppViewModel> MostUsedApps { get; } = [];
	public ObservableCollection<MostUsedAppViewModel> AllAppsUsage { get; } = [];
	public ObservableCollection<MostUsedAppViewModel> SelectedDayApps { get; } = [];
	public ObservableCollection<MostUsedAppViewModel> WeeklyTopApps { get; } = [];

	public string CurrentMonthTitle => CurrentMonth.ToString("MMMM yyyy", CultureInfo.CurrentCulture);
	public string WeekRangeTitle => $"{CurrentWeekStart:MMM d} - {CurrentWeekStart.AddDays(6):MMM d}";

	public StatsViewModel(IAppUsageService? appUsageService = null, IStatsApiService? statsApiService = null, IAppIconProvider? appIconProvider = null, ISubscriptionApiService? subscriptionApiService = null)
	{
		_appUsageService = appUsageService;
		_statsApiService = statsApiService;
		_appIconProvider = appIconProvider;
		_subscriptionApiService = subscriptionApiService;
		CurrentWeekStart = GetWeekStart(DateOnly.FromDateTime(DateTime.Today));
		InitializeWeeklyChart();
		LoadMonthCalendar();
	}

	public override void OnAppearing()
	{
		base.OnAppearing();
		StartRefreshTimer();
		_ = LoadSubscriptionStatusAsync();
		_ = LoadCalendarFromServerAsync();
		_ = LoadWeeklyStatsAsync();
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
	private void GoToPreviousWeek()
	{
		CurrentWeekStart = CurrentWeekStart.AddDays(-7);
		InitializeWeeklyChart();
		_ = LoadWeeklyStatsAsync();
	}

	[RelayCommand(CanExecute = nameof(CanGoToNextWeek))]
	private void GoToNextWeek()
	{
		CurrentWeekStart = CurrentWeekStart.AddDays(7);
		InitializeWeeklyChart();
		_ = LoadWeeklyStatsAsync();
	}

	private bool CanGoToNextWeek() =>
		CurrentWeekStart < GetWeekStart(DateOnly.FromDateTime(DateTime.Today));

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
	private void SelectChartDay(DayChartBarViewModel? day)
	{
		if (day is null) return;

		// Navigate to the month containing this day
		CurrentMonth = day.Date.ToDateTime(TimeOnly.MinValue);
		LoadMonthCalendar();
		_ = LoadCalendarFromServerAsync();

		// Find and select the day in calendar
		var dayVm = FindDayViewModel(day.Date.ToDateTime(TimeOnly.MinValue));
		if (dayVm is not null)
			SelectDay(dayVm);
	}

	[RelayCommand]
	private void GoToToday()
	{
		CurrentMonth = DateTime.Today;
		CurrentWeekStart = GetWeekStart(DateOnly.FromDateTime(DateTime.Today));
		LoadMonthCalendar();
		InitializeWeeklyChart();
		_ = LoadCalendarFromServerAsync();
		_ = LoadWeeklyStatsAsync();

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

	private async Task LoadSubscriptionStatusAsync()
	{
		if (_subscriptionApiService is null) return;

		try
		{
			var status = await _subscriptionApiService.GetStatusAsync();
			if (status is not null)
				IsPremium = status.IsPremium;
		}
		catch (Exception ex)
		{
			System.Diagnostics.Debug.WriteLine($"LoadSubscriptionStatus error: {ex}");
		}
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

			UpdateAppCollection(AllAppsUsage, sortedUsage, _appIconProvider);
			UpdateAppCollection(MostUsedApps, sortedUsage.Take(3).ToList(), _appIconProvider);

			// Sync to server periodically (every 5 minutes handled by separate sync logic)
		}
		catch (Exception ex)
		{
			System.Diagnostics.Debug.WriteLine($"LoadLocalStatsAsync error: {ex}");
		}
	}

	private static void UpdateAppCollection(ObservableCollection<MostUsedAppViewModel> collection, List<AppUsageInfo> source, IAppIconProvider? iconProvider = null)
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
			var iconBytes = iconProvider?.GetIcon(app.PackageName);

			var existing = collection.FirstOrDefault(x => x.Name == name);
			if (existing is not null)
			{
				if (existing.Duration != duration)
					existing.Duration = duration;
				if (existing.IconBytes is null && iconBytes is not null)
					existing.IconBytes = iconBytes;

				var oldIndex = collection.IndexOf(existing);
				if (oldIndex != i)
					collection.Move(oldIndex, i);
			}
			else
			{
				var vm = new MostUsedAppViewModel(name, duration, iconBytes);
				if (i < collection.Count)
					collection.Insert(i, vm);
				else
					collection.Add(vm);
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

			if (_calendarData.TryGetValue(dateOnly, out var data))
			{
				day.TotalScreenTimeSeconds = data.TotalScreenTimeSeconds;
				day.IsFreezeDay = data.IsFreezeDay;
				day.IsStreakDay = isGoalMet || data.IsFreezeDay;
			}
			else
			{
				day.IsStreakDay = isGoalMet;
			}
		}

		// Calculate streak segments per week row for visual continuity
		foreach (var week in Weeks)
		{
			for (var i = 0; i < week.Days.Count; i++)
			{
				var day = week.Days[i];
				if (!day.IsStreakDay)
				{
					day.IsStreakStart = false;
					day.IsStreakEnd = false;
					day.IsStreakMiddle = false;
					continue;
				}

				var prevIsStreak = i > 0 && week.Days[i - 1].IsStreakDay && week.Days[i - 1].IsCurrentMonth;
				var nextIsStreak = i < week.Days.Count - 1 && week.Days[i + 1].IsStreakDay && week.Days[i + 1].IsCurrentMonth;

				if (!prevIsStreak && !nextIsStreak)
				{
					day.IsStreakStart = true;
					day.IsStreakEnd = true;
					day.IsStreakMiddle = false;
				}
				else
				{
					day.IsStreakStart = !prevIsStreak;
					day.IsStreakEnd = !nextIsStreak;
					day.IsStreakMiddle = prevIsStreak && nextIsStreak;
				}
			}
		}
	}

	private async Task LoadSelectedDayStatsAsync(DateOnly date)
	{
		SelectedDayApps.Clear();
		SelectedDayScreenTime = "0m";
		SelectedDayPickups = 0;
		SelectedDayGoalMet = false;

		var isToday = date == DateOnly.FromDateTime(DateTime.Today);

		if (isToday)
		{
			// Today's data is always read live from the device — no server sync has happened yet
			SelectedDayScreenTime = ScreenTimeToday;
			SelectedDayPickups = Pickups;

			// Copy already-loaded local app usage
			var totalSeconds = AllAppsUsage.Sum(a => a.TotalSeconds);
			foreach (var app in AllAppsUsage)
			{
				var percent = totalSeconds > 0 ? (double)app.TotalSeconds / totalSeconds * 100 : 0;
				SelectedDayApps.Add(new MostUsedAppViewModel(app.Name, app.Duration, app.TotalSeconds, percent, app.IconBytes));
			}

			// GoalMet for today: try server, but don't block if unavailable
			if (_statsApiService is not null)
			{
				try
				{
					var todayStats = await _statsApiService.GetDailyStatsAsync(date);
					if (todayStats is not null)
						SelectedDayGoalMet = todayStats.GoalMet;
				}
				catch (Exception ex)
				{
					System.Diagnostics.Debug.WriteLine($"LoadSelectedDayStats (today, GoalMet) error: {ex}");
				}
			}

			return;
		}

		if (_statsApiService is null) return;

		try
		{
			var stats = await _statsApiService.GetDailyStatsAsync(date);
			if (stats is null) return;

			SelectedDayScreenTime = FormatDuration(TimeSpan.FromSeconds(stats.TotalScreenTimeSeconds));
			SelectedDayPickups = stats.Pickups;
			SelectedDayGoalMet = stats.GoalMet;

			var totalSeconds = stats.Apps.Sum(a => a.ForegroundSeconds);
			foreach (var app in stats.Apps)
			{
				var name = string.IsNullOrWhiteSpace(app.AppLabel) ? app.PackageName : app.AppLabel;
				var duration = FormatDuration(TimeSpan.FromSeconds(app.ForegroundSeconds));
				var percent = totalSeconds > 0 ? (double)app.ForegroundSeconds / totalSeconds * 100 : 0;
				var iconBytes = _appIconProvider?.GetIcon(app.PackageName);
				SelectedDayApps.Add(new MostUsedAppViewModel(name, duration, app.ForegroundSeconds, percent, iconBytes));
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

	#region Weekly Chart

	private static DateOnly GetWeekStart(DateOnly date)
	{
		var diff = (7 + (date.DayOfWeek - DayOfWeek.Monday)) % 7;
		return date.AddDays(-diff);
	}

	private void InitializeWeeklyChart()
	{
		WeeklyChartData.Clear();
		for (var i = 0; i < 7; i++)
		{
			var date = CurrentWeekStart.AddDays(i);
			var dayOfWeek = date.DayOfWeek;
			WeeklyChartData.Add(new DayChartBarViewModel(dayOfWeek, date));
		}
	}

	private async Task LoadWeeklyStatsAsync()
	{
		if (_statsApiService is null) return;

		try
		{
			var weeklyStats = await _statsApiService.GetWeeklyStatsAsync(CurrentWeekStart);
			if (weeklyStats is null)
			{
				ResetWeeklyStats();
				return;
			}

			// Update totals
			WeeklyTotalTime = FormatDuration(TimeSpan.FromSeconds(weeklyStats.TotalScreenTimeSeconds));
			WeeklyAverageTime = FormatDuration(TimeSpan.FromSeconds(weeklyStats.AverageScreenTimeSeconds));
			WeeklyTotalPickups = weeklyStats.TotalPickups;
			WeeklyAveragePickups = weeklyStats.AveragePickups;

			// Update comparison
			if (weeklyStats.Comparison is not null)
			{
				HasComparison = true;
				IsImproving = weeklyStats.Comparison.IsImproving;
				ScreenTimeChangePercent = weeklyStats.Comparison.ScreenTimeChangePercent;
				ScreenTimeChangeSeconds = weeklyStats.Comparison.ScreenTimeChangeSeconds;

				var changeText = Math.Abs(ScreenTimeChangePercent).ToString("F0");
				var direction = IsImproving ? "less" : "more";
				var arrow = IsImproving ? "▼" : "▲";
				ComparisonText = $"{arrow} {changeText}% {direction} than last week";
			}
			else
			{
				HasComparison = false;
				ComparisonText = "";
			}

			// Update chart bars
			var maxSeconds = weeklyStats.DailyData.Count > 0 
				? weeklyStats.DailyData.Max(d => d.ScreenTimeSeconds) 
				: 1;
			maxSeconds = Math.Max(maxSeconds, 1); // Avoid division by zero

			foreach (var dayData in weeklyStats.DailyData)
			{
				var barVm = WeeklyChartData.FirstOrDefault(b => b.Date == dayData.Date);
				if (barVm is not null)
				{
					barVm.ScreenTimeSeconds = dayData.ScreenTimeSeconds;
					barVm.Pickups = dayData.Pickups;
					barVm.GoalMet = dayData.GoalMet;
					barVm.BarHeightPercent = (double)dayData.ScreenTimeSeconds / maxSeconds * 100;
				}
			}

						// Update weekly top apps with percentages
						WeeklyTopApps.Clear();
						var totalAppSeconds = weeklyStats.TopApps.Sum(a => a.ForegroundSeconds);
						foreach (var app in weeklyStats.TopApps.Take(5))
						{
							var name = string.IsNullOrWhiteSpace(app.AppLabel) ? app.PackageName : app.AppLabel;
							var duration = FormatDuration(TimeSpan.FromSeconds(app.ForegroundSeconds));
							var percent = totalAppSeconds > 0 ? (double)app.ForegroundSeconds / totalAppSeconds * 100 : 0;
							var iconBytes = _appIconProvider?.GetIcon(app.PackageName);
							WeeklyTopApps.Add(new MostUsedAppViewModel(name, duration, app.ForegroundSeconds, percent, iconBytes));
						}
					}
					catch (Exception ex)
					{
						System.Diagnostics.Debug.WriteLine($"LoadWeeklyStats error: {ex}");
						ResetWeeklyStats();
					}
				}

	private void ResetWeeklyStats()
	{
		WeeklyTotalTime = "0m";
		WeeklyAverageTime = "0m";
		WeeklyTotalPickups = 0;
		WeeklyAveragePickups = 0;
		HasComparison = false;
		ComparisonText = "";
		WeeklyTopApps.Clear();

		foreach (var bar in WeeklyChartData)
		{
			bar.ScreenTimeSeconds = 0;
			bar.BarHeightPercent = 0;
			bar.Pickups = 0;
			bar.GoalMet = false;
		}
	}

	#endregion

	private static string FormatDuration(TimeSpan ts)
	{
		if (ts.TotalHours >= 1)
			return $"{(int)ts.TotalHours}h {ts.Minutes}m";
		return $"{ts.Minutes}m";
	}
}
