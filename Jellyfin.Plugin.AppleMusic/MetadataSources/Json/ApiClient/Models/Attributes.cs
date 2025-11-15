namespace Jellyfin.Plugin.AppleMusic.MetadataSources.Json.ApiClient.Models;

/// <summary>
/// Apple Music search result attributes.
/// </summary>
public class Attributes
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Attributes"/> class.
    /// </summary>
    public Attributes()
    {
        ArtistName = string.Empty;
        ArtistUrl = string.Empty;
        Url = string.Empty;
        Name = string.Empty;
        Artwork = new Artwork();
    }

    /// <summary>
    /// Gets or sets artist name.
    /// </summary>
    public string ArtistName { get; set; }

    /// <summary>
    /// Gets or sets artist url.
    /// </summary>
    public string ArtistUrl { get; set; }

    /// <summary>
    /// Gets or sets URL.
    /// </summary>
    public string Url { get; set; }

    /// <summary>
    /// Gets or sets name.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Gets or sets the artwork information.
    /// </summary>
    public Artwork Artwork { get; set; }
}
