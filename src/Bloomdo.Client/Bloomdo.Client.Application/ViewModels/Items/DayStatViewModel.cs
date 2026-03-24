using CommunityToolkit.Mvvm.ComponentModel;

namespace Bloomdo.Client.Application.ViewModels.Items;

public partial class DayStatViewModel : ObservableObject
{
    public int DayNumber { get; }
    public bool IsCurrentMonth { get; }
    public DateTime Date { get; }
    public bool IsWeekend => Date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday;

    public bool IsToday
    {
        get
        {
            var localToday = DateTimeOffset.Now.LocalDateTime.Date;
            return Date.Year == localToday.Year && Date.Month == localToday.Month && Date.Day == localToday.Day;
        }
    }

    [ObservableProperty]
    private bool _isSelected;

    [ObservableProperty]
    private bool _isStreakDay;

    [ObservableProperty]
    private bool _isFreezeDay;

    [ObservableProperty]
    private bool _isStreakStart;

    [ObservableProperty]
    private bool _isStreakEnd;

    [ObservableProperty]
    private bool _isStreakMiddle;

    [ObservableProperty]
    private int _totalScreenTimeSeconds;

    public DayStatViewModel(int dayNumber, bool isCurrentMonth, DateTime date)
    {
        DayNumber = dayNumber;
        IsCurrentMonth = isCurrentMonth;
        Date = date;
    }
}