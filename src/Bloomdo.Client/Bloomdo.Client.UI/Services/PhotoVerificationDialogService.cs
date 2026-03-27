using Bloomdo.Client.Application.ViewModels;
using Bloomdo.Client.Core.Interfaces;
using Bloomdo.Shared.DTOs.Activities;

namespace Bloomdo.Client.UI.Services;

public class PhotoVerificationDialogService(
    ShellViewModel shell,
    IDailyActivityApiService activityApi,
    IImagePickerService imagePicker,
    IToastService toastService) : IPhotoVerificationDialogService
{
    public void Show(Guid activityItemId, VerificationTemplate? verificationTemplate, string? customVerificationCriteria, DateOnly date, Action? onVerified = null)
    {
        var vm = new PhotoVerificationViewModel(activityApi, imagePicker, toastService, () =>
        {
            onVerified?.Invoke();
            CloseOverlay();
        });

        vm.Configure(activityItemId, date);

        shell.OnOverlayClosed = CloseOverlay;
        shell.OverlayContent = vm;
    }

    private void CloseOverlay()
    {
        shell.OnOverlayClosed = null;
        shell.OverlayContent = null;
    }
}
