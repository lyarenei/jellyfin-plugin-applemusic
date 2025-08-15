using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.AppleMusic.Dtos;

namespace Jellyfin.Plugin.AppleMusic.MetadataSources;

/// <summary>
/// Indicator for the type of item being searched or retrieved.
/// </summary>
public enum ItemType
{
    /// <summary>
    /// Album item type.
    /// </summary>
    Album,

    /// <summary>
    /// Artist item type.
    /// </summary>
    Artist,
}

/// <summary>
/// Interface for Apple Music metadata sources.
/// </summary>
public interface IMetadataSource
{
    /// <summary>
    /// Search for items using the specified search term.
    /// </summary>
    /// <param name="searchTerm">Search term.</param>
    /// <param name="itemType">Item type to search for.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of results.</returns>
    public Task<List<IITunesItem>> SearchAsync(string searchTerm, ItemType itemType, CancellationToken cancellationToken);

    /// <summary>
    /// Get album data by album ID.
    /// </summary>
    /// <param name="albumId">Apple Music ID of the album.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Album data. Null if not found.</returns>
    public Task<ITunesAlbum?> GetAlbumAsync(string albumId, CancellationToken cancellationToken);

    /// <summary>
    /// Get artist data by artist ID.
    /// </summary>
    /// <param name="artistId">Apple Music ID of the artist.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Artist data. Null if not found.</returns>
    public Task<ITunesArtist?> GetArtistAsync(string artistId, CancellationToken cancellationToken);
}
