namespace Jellyfin.Plugin.ITunes.Utils;

/// <summary>
/// Image size class.
/// </summary>
public class ImageSize
{
    /// <summary>
    /// Gets or sets image width.
    /// </summary>
    public int Width { get; set; }

    /// <summary>
    /// Gets or sets image height.
    /// </summary>
    public int Height { get; set; }

    /// <summary>
    /// Gets thumbnail image size.
    /// </summary>
    public static ImageSize ThumbnailImageSize => new() { Width = 100, Height = 100 };

    /// <summary>
    /// Gets default image size.
    /// </summary>
    public static ImageSize DefaultImageSize => new() { Width = 1400, Height = 1400 };

    /// <summary>
    /// Gets backdrop image size.
    /// </summary>
    public static ImageSize BackdropImageSize => new() { Width = 1920, Height = 1080 };

    /// <inheritdoc />
    public override string ToString() => $"{Width}x{Height}cc";
}
