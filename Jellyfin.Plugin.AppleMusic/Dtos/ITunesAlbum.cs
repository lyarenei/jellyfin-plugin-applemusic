using System;
using System.Collections.Generic;
using System.Linq;
using Jellyfin.Plugin.AppleMusic.ExternalIds;
using MediaBrowser.Model.Providers;

namespace Jellyfin.Plugin.AppleMusic.Dtos;

/// <summary>
/// Apple music album item.
/// </summary>
public class ITunesAlbum : IITunesItem
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ITunesAlbum"/> class.
    /// </summary>
    public ITunesAlbum()
    {
        Id = string.Empty;
        Name = string.Empty;
        Url = string.Empty;
        Artists = new List<ITunesArtist>();
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

    /// <summary>
    /// Gets or sets artists.
    /// The first artist should be also an album artist.
    /// </summary>
    public IEnumerable<ITunesArtist> Artists { get; set; }

    /// <summary>
    /// Gets or sets release date.
    /// </summary>
    public DateTime? ReleaseDate { get; set; }

    /// <inheritdoc />
    public RemoteSearchResult ToRemoteSearchResult()
    {
        return new RemoteSearchResult
        {
            Name = Name,
            ImageUrl = ImageUrl,
            Overview = About,
            PremiereDate = ReleaseDate,
            ProductionYear = ReleaseDate?.Year,
            AlbumArtist = Artists.FirstOrDefault()?.ToRemoteSearchAlbumArtistResult(),
            Artists = (from artist in Artists select artist.ToRemoteSearchResult()).ToArray(),
            ProviderIds = new Dictionary<string, string> { { nameof(ProviderKey.ITunesAlbum), Id } },
        };
    }

    /// <inheritdoc />
    public bool HasMetadata()
    {
        return !string.IsNullOrEmpty(Name) ||
               About is not null ||
               ReleaseDate is not null;
    }
}
