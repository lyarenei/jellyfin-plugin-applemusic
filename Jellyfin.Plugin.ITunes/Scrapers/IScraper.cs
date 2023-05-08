using AngleSharp.Dom;
using Jellyfin.Plugin.ITunes.Dtos;

namespace Jellyfin.Plugin.ITunes.Scrapers;

/// <summary>
/// Scraper interface for getting various metadata.
/// </summary>
/// <typeparam name="T">Metadata result type.</typeparam>
public interface IScraper<T>
{
    /// <summary>
    /// Scrape provided document for metadata.
    /// </summary>
    /// <param name="document">Document to scrape.</param>
    /// <returns>Scraped data. Null if error.</returns>
    public IITunesItem? Scrape(IDocument document);
}
