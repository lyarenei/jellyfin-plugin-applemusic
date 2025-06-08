using AngleSharp;
using AngleSharp.Dom;
using AngleSharp.XPath;
using Jellyfin.Plugin.ITunes.Dtos;
using Jellyfin.Plugin.ITunes.Utils;
using MediaBrowser.Controller.Entities.Audio;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.ITunes.Scrapers;

/// <summary>
/// Apple Music artist metadata scraper.
/// </summary>
public class ArtistScraper : IScraper<MusicArtist>
{
    private const string ImageXPath = "//meta[@property='og:image' and not(contains(@content, 'apple-music.png'))]/@content";
    private const string ArtistNameXPath = "//h1[@data-testid='artist-header-name']";
    private const string OverviewXPath = "//p[@data-testid='truncate-text']";

    private readonly ILogger<ArtistScraper> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ArtistScraper"/> class.
    /// </summary>
    /// <param name="logger">Logger factory.</param>
    public ArtistScraper(ILogger<ArtistScraper> logger)
    {
        _logger = logger;
        AngleSharp.Configuration.Default.WithDefaultLoader();
    }

    /// <inheritdoc />
    public IITunesItem? Scrape(IDocument document)
    {
        var artistName = document.Body.SelectSingleNode(ArtistNameXPath)?.TextContent;
        if (artistName is null)
        {
            _logger.LogDebug("Artist name not found");
            return null;
        }

        var overview = document.Body.SelectSingleNode(OverviewXPath)?.TextContent;
        if (overview is null)
        {
            _logger.LogDebug("Artist overview not found");
        }

        var imageUrl = document.Head.SelectSingleNode(ImageXPath)?.TextContent;
        if (imageUrl is null)
        {
            _logger.LogDebug("Artist image not found");
        }
        else
        {
            imageUrl = PluginUtils.UpdateImageSize(imageUrl, ImageSize.Default.ToString());
        }

        return new ITunesArtist
        {
            ImageUrl = imageUrl,
            Name = artistName.Trim(),
            About = overview
        };
    }
}
