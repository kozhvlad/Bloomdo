using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using Bloomdo.Client.Core.Interfaces;

namespace Bloomdo.Client.UI.Services;

public class AvaloniaImagePickerService : IImagePickerService
{
    private static readonly FilePickerFileType ImageTypes = new("Images")
    {
        Patterns = ["*.jpg", "*.jpeg", "*.png", "*.webp"],
        MimeTypes = ["image/jpeg", "image/png", "image/webp"]
    };

    public async Task<ImagePickResult?> PickFromGalleryAsync(CancellationToken ct = default)
    {
        var topLevel = GetTopLevel();
        if (topLevel is null) return null;

        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Select photo",
            AllowMultiple = false,
            FileTypeFilter = [ImageTypes]
        });

        if (files.Count == 0) return null;

        await using var stream = await files[0].OpenReadAsync();
        using var ms = new MemoryStream();
        await stream.CopyToAsync(ms, ct);
        return new ImagePickResult(ms.ToArray());
    }

    public Task<ImagePickResult?> TakePhotoAsync(CancellationToken ct = default)
    {
        // Camera not available on Desktop — fall back to gallery picker
        return PickFromGalleryAsync(ct);
    }

    private static TopLevel? GetTopLevel()
    {
        if (Avalonia.Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            return TopLevel.GetTopLevel(desktop.MainWindow);

        if (Avalonia.Application.Current?.ApplicationLifetime is ISingleViewApplicationLifetime single)
            return TopLevel.GetTopLevel(single.MainView);

        return null;
    }
}
