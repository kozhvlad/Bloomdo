using System;
using System.Collections.Concurrent;
using System.IO;
using Android.Content;
using Android.Content.PM;
using Android.Graphics;
using Android.Graphics.Drawables;
using Bloomdo.Client.Core.Interfaces;

namespace Bloomdo.Client.Android.Services;

/// <summary>
/// Extracts and caches app icons from the Android PackageManager.
/// Icons are loaded lazily on first request and stored as PNG byte arrays.
/// </summary>
public sealed class AndroidAppIconProvider(Context context) : IAppIconProvider
{
    private const int IconSizePx = 96;
    private readonly ConcurrentDictionary<string, byte[]?> _cache = new();

    public byte[]? GetIcon(string packageName)
    {
        return _cache.GetOrAdd(packageName, ExtractIcon);
    }

    private byte[]? ExtractIcon(string packageName)
    {
        try
        {
            var pm = context.PackageManager!;
            var appInfo = pm.GetApplicationInfo(packageName, PackageInfoFlags.MetaData);
            if (appInfo is null) return null;

            var drawable = pm.GetApplicationIcon(appInfo);
            return DrawableToBytes(drawable);
        }
        catch
        {
            return null;
        }
    }

    private static byte[]? DrawableToBytes(Drawable? drawable)
    {
        if (drawable is null) return null;

        var width = drawable.IntrinsicWidth > 0 ? drawable.IntrinsicWidth : IconSizePx;
        var height = drawable.IntrinsicHeight > 0 ? drawable.IntrinsicHeight : IconSizePx;

        // Scale down large icons to save memory
        if (width > IconSizePx || height > IconSizePx)
        {
            var scale = Math.Min((double)IconSizePx / width, (double)IconSizePx / height);
            width = (int)(width * scale);
            height = (int)(height * scale);
        }

        using var bitmap = Bitmap.CreateBitmap(width, height, Bitmap.Config.Argb8888!);
        if (bitmap is null) return null;

        using var canvas = new Canvas(bitmap);
        drawable.SetBounds(0, 0, canvas.Width, canvas.Height);
        drawable.Draw(canvas);

        using var stream = new MemoryStream();
        bitmap.Compress(Bitmap.CompressFormat.Png!, 100, stream);
        return stream.ToArray();
    }
}
