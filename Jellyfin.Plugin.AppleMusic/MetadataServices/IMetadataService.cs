using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.AppleMusic.Dtos;
using MediaBrowser.Model.Providers;

namespace Jellyfin.Plugin.AppleMusic.MetadataServices;

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
    public Task<ICollection<string>> Search(string searchTerm, ItemType type, CancellationToken cancellationToken);

    /// <summary>
    /// Scrapes metadata on provided URL.
    /// </summary>
    /// <param name="url">URL to work with.</param>
    /// <param name="type">Item type to scrape.</param>
    /// <returns>Instance of <see cref="RemoteSearchResult"/>. Null if error.</returns>
    public Task<IITunesItem?> Scrape(string url, ItemType type);
}
