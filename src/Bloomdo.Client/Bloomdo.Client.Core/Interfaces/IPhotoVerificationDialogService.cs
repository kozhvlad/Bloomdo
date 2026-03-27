using Bloomdo.Shared.DTOs.Activities;

namespace Bloomdo.Client.Core.Interfaces;

public interface IPhotoVerificationDialogService
{
    void Show(Guid activityItemId, VerificationTemplate? verificationTemplate, string? customVerificationCriteria, DateOnly date, Action? onVerified = null);
}
