namespace Bloomdo.Client.Application.ViewModels.Items;

public class TabItemViewModel
{
	public string Title { get; }
	public PageViewModel Content { get; }

	public TabItemViewModel(string title, PageViewModel content)
	{
		Title = title;
		Content = content;
	}
}
