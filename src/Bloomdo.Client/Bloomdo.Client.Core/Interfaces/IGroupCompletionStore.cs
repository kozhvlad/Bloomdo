namespace Bloomdo.Client.Core.Interfaces;

public interface IGroupCompletionStore
{
    Task SaveCompletionStatusAsync(Dictionary<Guid, bool> groupCompletionStatus);
    Task<Dictionary<Guid, bool>> LoadCompletionStatusAsync();
}
