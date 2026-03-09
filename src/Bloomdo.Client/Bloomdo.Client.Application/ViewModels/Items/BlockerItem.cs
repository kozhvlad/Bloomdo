using Bloomdo.Shared.Enums;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Bloomdo.Client.Application.ViewModels.Items;

public partial class BlockerItem : ObservableObject
{
	public Guid Id { get; }
	public string Title { get; }
	public BlockType Type { get; }
	public string TimeDescription { get; }
	public int BlockedAppCount { get; }

	[ObservableProperty]
	private bool _isActive;

	public string TypeLabel => Type switch
	{
		BlockType.Schedule => "Schedule",
		BlockType.Limit => "Limit",
		BlockType.Focus => "Focus",
		BlockType.Bloomdo => "Bloomdo",
		_ => "Block"
	};

	public string TypeColor => Type switch
	{
		BlockType.Schedule => "#009688",
		BlockType.Limit => "#673AB7",
		BlockType.Focus => "#FF9800",
		BlockType.Bloomdo => "#3F51B5",
		_ => "#666666"
	};

	public BlockerItem(Guid id, string title, BlockType type, string timeDescription, int blockedAppCount, bool isActive)
	{
		Id = id;
		Title = title;
		Type = type;
		TimeDescription = timeDescription;
		BlockedAppCount = blockedAppCount;
		_isActive = isActive;
	}
}
