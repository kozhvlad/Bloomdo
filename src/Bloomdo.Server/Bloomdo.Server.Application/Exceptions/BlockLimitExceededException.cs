namespace Bloomdo.Server.Application.Exceptions;

public class BlockLimitExceededException(int maxBlocks)
    : Exception($"Block rule limit of {maxBlocks} reached. Upgrade to Bloomdo Plus for unlimited blocks.")
{
    public int MaxBlocks { get; } = maxBlocks;
}
