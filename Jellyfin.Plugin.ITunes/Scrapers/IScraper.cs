using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Model.Providers;

namespace Jellyfin.Plugin.ITunes.Scrapers;

/// <summary>
/// Scraper interface for getting various metadata.
/// </summary>
/// <typeparam name="T">Metadata result type.</typeparam>
public interface IScraper<T>
{
    /// <summary>
    /// Search and scrape images using provided URL.
    /// </summary>
    /// <param name="searchTerm">Term to use for searching.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A collection of images.</returns>
    public Task<IEnumerable<RemoteImageInfo>> GetImages(string searchTerm, CancellationToken cancellationToken);

    /// <summary>
    /// Search for results using the specified search term.
    /// </summary>
    /// <param name="searchTerm">Term to use for search.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Results.</returns>
    public Task<IEnumerable<RemoteSearchResult>> Search(string searchTerm, CancellationToken cancellationToken);
}
