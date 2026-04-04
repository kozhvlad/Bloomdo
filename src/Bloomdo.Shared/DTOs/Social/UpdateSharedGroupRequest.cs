namespace Bloomdo.Shared.DTOs.Social;

public record UpdateSharedGroupRequest
{
    public string? Title { get; init; }
    public string? Icon { get; init; }
    public string? Color { get; init; }
}
