namespace Jellyfin.Plugin.AppleMusic.Utils;

/// <summary>
/// Image size as used in Apple Music image URLs, for example 1400x1400cc.
/// </summary>
public class ImageSize
{
    private ImageSize(int width, int height, string crop)
    {
        Width = width;
        Height = height;
        Crop = crop;
    }

    /// <summary>
    /// Gets thumbnail image size.
    /// </summary>
    public static ImageSize Thumbnail { get; } = new(100, 100, "cc");

    /// <summary>
    /// Gets primary image size.
    /// </summary>
    public static ImageSize Primary { get; } = new(1400, 1400, "cc");

    /// <summary>
    /// Gets backdrop image size.
    /// </summary>
    public static ImageSize Backdrop { get; } = new(1920, 1080, "sr");

    /// <summary>
    /// Gets backdrop thumbnail image size.
    /// </summary>
    public static ImageSize BackdropThumbnail { get; } = new(400, 225, "sr");

    /// <summary>
    /// Gets image width.
    /// </summary>
    public int Width { get; }

    /// <summary>
    /// Gets image height.
    /// </summary>
    public int Height { get; }

    /// <summary>
    /// Gets crop mode.
    /// "cc" crops to a square, "sr" crops to exactly the requested size.
    /// </summary>
    public string Crop { get; }

    /// <inheritdoc />
    public override string ToString() => $"{Width}x{Height}{Crop}";
}
