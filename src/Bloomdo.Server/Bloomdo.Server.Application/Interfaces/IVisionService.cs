using Bloomdo.Shared.DTOs.Activities;

namespace Bloomdo.Server.Application.Interfaces;

public record VisionResult(VerificationStatus Status, string Explanation, float Confidence);

public interface IVisionService
{
    Task<VisionResult> VerifyAsync(byte[] imageBytes, VerificationTemplate template,
        string? customCriteria, CancellationToken ct = default);
}
