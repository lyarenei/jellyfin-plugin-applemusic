using System.Collections.Generic;
using Jellyfin.Plugin.AppleMusic.ExternalIds;
using MediaBrowser.Model.Providers;

namespace Jellyfin.Plugin.AppleMusic.Dtos;

/// <summary>
/// Apple Music artist.
/// </summary>
public class ITunesArtist : IITunesItem
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ITunesArtist"/> class.
    /// </summary>
    public ITunesArtist()
    {
        Id = string.Empty;
        Name = string.Empty;
        Url = string.Empty;
    }

    /// <inheritdoc />
    public string Id { get; set; }

    /// <inheritdoc />
    public string Name { get; set; }

    /// <inheritdoc />
    public string Url { get; set; }

    /// <inheritdoc />
    public string? ImageUrl { get; set; }

    /// <inheritdoc />
    public string? About { get; set; }

    /// <inheritdoc />
    public RemoteSearchResult ToRemoteSearchResult()
    {
        return new RemoteSearchResult
        {
            ImageUrl = ImageUrl,
            Name = Name,
            Overview = About,
            ProviderIds = new Dictionary<string, string> { { nameof(ProviderKey.ITunesArtist), Id } },
        };
    }

    /// <summary>
    /// Convert this item to a <see cref="RemoteSearchResult"/>.
    /// Provider IDs are set as the album artist instead of artist.
    /// </summary>
    /// <returns>Instance of <see cref="RemoteSearchResult"/>.</returns>
    public RemoteSearchResult ToRemoteSearchAlbumArtistResult()
    {
        return new RemoteSearchResult
        {
            ImageUrl = ImageUrl,
            Name = Name,
            Overview = About,
            ProviderIds = new Dictionary<string, string> { { nameof(ProviderKey.ITunesAlbumArtist), Id } },
        };
    }

    /// <inheritdoc />
    public bool HasMetadata() => !string.IsNullOrEmpty(Name) || About is not null;
}
