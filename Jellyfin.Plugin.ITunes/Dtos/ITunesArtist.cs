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
        Name = string.Empty;
    }

    /// <inheritdoc />
    public string Name { get; set; }

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
}
