using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.AppleMusic.Dtos;

namespace Jellyfin.Plugin.AppleMusic.MetadataSources.Json;

/// <summary>
/// Apple Music JSON metadata source.
/// This source retrieves metadata from Apple Music's JSON API.
/// </summary>
public class JsonMetadataSource : IMetadataSource
{
    /// <inheritdoc />
    public Task<List<IITunesItem>> SearchAsync(string searchTerm, ItemType itemType, CancellationToken cancellationToken)
    {
        throw new System.NotImplementedException();
    }

    /// <inheritdoc />
    public Task<ITunesAlbum?> GetAlbumAsync(string albumId, CancellationToken cancellationToken)
    {
        throw new System.NotImplementedException();
    }

    /// <inheritdoc />
    public Task<ITunesArtist?> GetArtistAsync(string artistId, CancellationToken cancellationToken)
    {
        throw new System.NotImplementedException();
    }
}
