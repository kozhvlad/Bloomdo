namespace Bloomdo.Application.ViewModels.Items;

public class DayStatViewModel
{
    public int DayNumber { get; }

    public string? StatusIcon { get; }

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

    public DayStatViewModel(int dayNumber, string? statusIcon, bool isCurrentMonth, DateTime date)
    {
        DayNumber = dayNumber;
        StatusIcon = statusIcon;
        IsCurrentMonth = isCurrentMonth;
        Date = date;
    }
}