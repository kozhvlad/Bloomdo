using Bloomdo.Client.Core.Interfaces;
using Bloomdo.Shared.DTOs.Activities;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Bloomdo.Client.Application.ViewModels;

public enum PhotoVerificationState
{
    Idle,
    HasPhoto,
    Verifying,
    Verified,
    Rejected,
    LowConfidence,
    Error
}

public partial class PhotoVerificationViewModel : ObservableObject
{
    private readonly IDailyActivityApiService _activityApi;
    private readonly IImagePickerService _imagePicker;
    private readonly IToastService? _toastService;
    private readonly Action? _onVerified;

    private Guid _activityItemId;
    private DateOnly _date;
    private byte[]? _imageBytes;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsIdle))]
    [NotifyPropertyChangedFor(nameof(HasPhoto))]
    [NotifyPropertyChangedFor(nameof(IsVerifying))]
    [NotifyPropertyChangedFor(nameof(IsVerified))]
    [NotifyPropertyChangedFor(nameof(IsRejected))]
    [NotifyPropertyChangedFor(nameof(IsLowConfidence))]
    [NotifyPropertyChangedFor(nameof(IsError))]
    private PhotoVerificationState _state = PhotoVerificationState.Idle;

    [ObservableProperty]
    private string _explanation = string.Empty;

    [ObservableProperty]
    private byte[]? _previewImageBytes;

    public bool IsIdle => State == PhotoVerificationState.Idle;
    public bool HasPhoto => State == PhotoVerificationState.HasPhoto;
    public bool IsVerifying => State == PhotoVerificationState.Verifying;
    public bool IsVerified => State == PhotoVerificationState.Verified;
    public bool IsRejected => State == PhotoVerificationState.Rejected;
    public bool IsLowConfidence => State == PhotoVerificationState.LowConfidence;
    public bool IsError => State == PhotoVerificationState.Error;

    public PhotoVerificationViewModel(
        IDailyActivityApiService activityApi,
        IImagePickerService imagePicker,
        IToastService? toastService = null,
        Action? onVerified = null)
    {
        _activityApi = activityApi;
        _imagePicker = imagePicker;
        _toastService = toastService;
        _onVerified = onVerified;
    }

    public void Configure(Guid activityItemId, DateOnly? date = null)
    {
        _activityItemId = activityItemId;
        _date = date ?? DateOnly.FromDateTime(DateTime.Today);
        State = PhotoVerificationState.Idle;
        Explanation = string.Empty;
        PreviewImageBytes = null;
        _imageBytes = null;
    }

    [RelayCommand]
    private async Task PickFromGallery(CancellationToken ct)
    {
        var result = await _imagePicker.PickFromGalleryAsync(ct);
        if (result is null) return;

        _imageBytes = result.CompressedBytes;
        PreviewImageBytes = result.CompressedBytes;
        State = PhotoVerificationState.HasPhoto;
    }

    [RelayCommand]
    private async Task TakePhoto(CancellationToken ct)
    {
        var result = await _imagePicker.TakePhotoAsync(ct);
        if (result is null) return;

        _imageBytes = result.CompressedBytes;
        PreviewImageBytes = result.CompressedBytes;
        State = PhotoVerificationState.HasPhoto;
    }

    [RelayCommand]
    private async Task Verify(CancellationToken ct)
    {
        if (_imageBytes is null) return;

        State = PhotoVerificationState.Verifying;

        try
        {
            var request = new VerifyPhotoRequest
            {
                ActivityItemId = _activityItemId,
                Date = _date,
                ImageBase64 = Convert.ToBase64String(_imageBytes)
            };

            var response = await _activityApi.VerifyPhotoAsync(request, ct);

            if (response is null)
            {
                State = PhotoVerificationState.Error;
                Explanation = string.Empty;
                return;
            }

            Explanation = response.Explanation;

            State = response.Status switch
            {
                VerificationStatus.Verified => PhotoVerificationState.Verified,
                VerificationStatus.LowConfidence => PhotoVerificationState.LowConfidence,
                _ => PhotoVerificationState.Rejected
            };

            if (response.Status == VerificationStatus.Verified)
                _onVerified?.Invoke();
        }
        catch
        {
            State = PhotoVerificationState.Error;
            Explanation = string.Empty;
        }
    }

    [RelayCommand]
    private void Retry()
    {
        State = PhotoVerificationState.Idle;
        Explanation = string.Empty;
        PreviewImageBytes = null;
        _imageBytes = null;
    }
}
