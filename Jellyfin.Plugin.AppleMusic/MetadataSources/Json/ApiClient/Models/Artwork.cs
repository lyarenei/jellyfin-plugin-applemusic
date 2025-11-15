namespace Jellyfin.Plugin.AppleMusic.MetadataSources.Json.ApiClient.Models;

/// <summary>
/// Apple Music search artwork.
/// </summary>
public class Artwork
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Artwork"/> class.
    /// </summary>
    public Artwork()
    {
        Url = string.Empty;
    }

    /// <summary>
    /// Gets or sets artwork url.
    /// </summary>
    public string Url { get; set; }

    /// <summary>
    /// Gets or sets artwork width.
    /// </summary>
    public int Width { get; set; }
}
