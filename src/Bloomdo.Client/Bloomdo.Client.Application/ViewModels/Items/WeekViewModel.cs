namespace Bloomdo.Client.Application.ViewModels.Items;

public class WeekViewModel
{
    public List<DayStatViewModel> Days { get; } = new(7);
}
