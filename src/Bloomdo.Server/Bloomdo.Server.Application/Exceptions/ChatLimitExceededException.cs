namespace Bloomdo.Server.Application.Exceptions;

public class ChatLimitExceededException(int maxMessages)
    : Exception($"Daily chat limit of {maxMessages} messages reached. Upgrade to Bloomdo Plus for unlimited messages.")
{
    public int MaxMessages { get; } = maxMessages;
}
