using CommunityToolkit.Mvvm.ComponentModel;

namespace Bloomdo.Client.Application.ViewModels.Items;

public partial class SelectableGroupItem : ObservableObject
{
    public Guid Id { get; }
    public string Title { get; }

    [ObservableProperty]
    private bool _isSelected;

    public SelectableGroupItem(Guid id, string title)
    {
        Id = id;
        Title = title;
    }
}
