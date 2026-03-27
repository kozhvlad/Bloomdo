namespace Bloomdo.Client.Core.Interfaces;

public record ImagePickResult(byte[] CompressedBytes);

public interface IImagePickerService
{
    Task<ImagePickResult?> PickFromGalleryAsync(CancellationToken ct = default);
    Task<ImagePickResult?> TakePhotoAsync(CancellationToken ct = default);
}
