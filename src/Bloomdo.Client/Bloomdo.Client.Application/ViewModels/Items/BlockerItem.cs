namespace Bloomdo.Application.ViewModels.Items;

public class BlockerItem
{
	public string Title { get; set; }
	public string Time { get; set; }
	public bool IsLocked { get; set; }
	public bool IsActive { get; set; }

	public BlockerItem(string title, string time, bool isLocked, bool isActive)
	{
		Title = title;
		Time = time;
		IsLocked = isLocked;
		IsActive = isActive;
	}
}
