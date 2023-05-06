using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Model.Providers;

namespace Jellyfin.Plugin.ITunes.Scrapers;

/// <summary>
/// Scraper interface for getting various metadata.
/// </summary>
public interface IScraper
{
    /// <summary>
    /// Search and scrape images using provided URL.
    /// </summary>
    /// <param name="searchUrl">URL for searching images.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A collection of images.</returns>
    public Task<IEnumerable<RemoteImageInfo>> GetImages(string searchUrl, CancellationToken cancellationToken);
}
