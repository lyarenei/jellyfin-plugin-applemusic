using System;
using MediaBrowser.Model.Providers;

namespace Jellyfin.Plugin.ITunes.Dtos;

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
        Name = string.Empty;
    }

    /// <inheritdoc />
    public string Name { get; set; }

    /// <inheritdoc />
    public string? ImageUrl { get; set; }

    /// <inheritdoc />
    public string? About { get; set; }

    /// <summary>
    /// Gets or sets artist name.
    /// </summary>
    public string? ArtistName { get; set; }

    /// <summary>
    /// Gets or sets URL to artist.
    /// </summary>
    public string? ArtistUrl { get; set; }

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
            ProductionYear = ReleaseDate?.Year
        };
    }
}
