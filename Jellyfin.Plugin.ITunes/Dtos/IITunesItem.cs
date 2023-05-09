using MediaBrowser.Model.Providers;

namespace Jellyfin.Plugin.ITunes.Dtos;

/// <summary>
/// Apple music item interface.
/// </summary>
public interface IITunesItem
{
    /// <summary>
    /// Gets or sets item's ID.
    /// </summary>
    public string Id { get; set; }

    /// <summary>
    /// Gets or sets item's name.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Gets or sets item's URL.
    /// </summary>
    public string Url { get; set; }

    /// <summary>
    /// Gets or sets item's image url.
    /// </summary>
    public string? ImageUrl { get; set; }

    /// <summary>
    /// Gets or sets item's about text.
    /// </summary>
    public string? About { get; set; }

    /// <summary>
    /// Convert this item to a <see cref="RemoteSearchResult"/>.
    /// </summary>
    /// <returns>New instance of <see cref="RemoteSearchResult"/>.</returns>
    public RemoteSearchResult ToRemoteSearchResult();

    /// <summary>
    /// Indicates if this item has metadata or not.
    /// </summary>
    /// <returns>Item has metadata.</returns>
    public bool HasMetadata();
}
