using System.Linq;
using AngleSharp;
using AngleSharp.Dom;
using AngleSharp.Html.Dom;
using AngleSharp.XPath;
using Jellyfin.Plugin.AppleMusic.Dtos;
using Jellyfin.Plugin.AppleMusic.Utils;
using MediaBrowser.Controller.Entities.Audio;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.AppleMusic.MetadataSources.Web.Scrapers;

/// <summary>
/// Apple Music artist metadata scraper.
/// </summary>
public class ArtistScraper : IScraper<MusicArtist>
{
    private const string ImageXPath = "//div[@data-testid='artist-detail-header']" +
                                      "//div[@data-testid='artwork-component']" +
                                      "//source[@type='image/jpeg']/@srcset";

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
            _logger.LogTrace("Artist name not found");
            return null;
        }

        _logger.LogTrace("Found artist name");

        var overview = document.Body.SelectSingleNode(OverviewXPath)?.TextContent;
        if (overview is null)
        {
            _logger.LogTrace("Artist overview not found");
        }
        else
        {
            _logger.LogTrace("Found artist overview");
        }

        var imageUrl = GetImageUrl(document.Body);
        if (imageUrl is null)
        {
            _logger.LogTrace("Artist image not found");
        }
        else
        {
            _logger.LogTrace("Found artist image");
            imageUrl = PluginUtils.UpdateImageSize(imageUrl, "1400x1400cc");
        }

        _logger.LogDebug("Artist scraping completed");

        return new ITunesArtist
        {
            ImageUrl = imageUrl,
            Name = artistName.Trim(),
            About = overview,
            Url = document.Url,
            Id = PluginUtils.GetIdFromUrl(document.Url),
        };
    }

    private static string? GetImageUrl(IHtmlElement? body)
    {
        var content = body?.SelectSingleNode(ImageXPath)?.TextContent;
        return content?.Split(' ').FirstOrDefault();
    }
}
