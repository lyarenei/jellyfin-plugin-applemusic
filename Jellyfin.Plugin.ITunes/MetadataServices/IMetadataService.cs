using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Jellyfin.Plugin.ITunes.MetadataServices;

/// <summary>
/// Item types.
/// </summary>
public enum ItemType
{
    /// <summary>
    /// Album item.
    /// </summary>
    Album,

    /// <summary>
    /// Artist item.
    /// </summary>
    Artist
}

/// <summary>
/// Service interface.
/// </summary>
public interface IMetadataService
{
    /// <summary>
    /// Performs a search on service using the specified term.
    /// </summary>
    /// <param name="searchTerm">Search term.</param>
    /// <param name="type">Item type to search.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Collection of URLs to search results.</returns>
    public Task<IEnumerable<string>> Search(string searchTerm, ItemType type, CancellationToken cancellationToken);
}
