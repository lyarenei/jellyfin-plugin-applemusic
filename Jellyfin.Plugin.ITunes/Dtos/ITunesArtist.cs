using MediaBrowser.Model.Providers;

namespace Jellyfin.Plugin.ITunes.Dtos;

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
            Overview = About
        };
    }

    /// <inheritdoc />
    public bool HasMetadata() => !string.IsNullOrEmpty(Name) || About is not null;
}
