using System.Net;
using System.Text.RegularExpressions;
using AngleSharp.Dom;
using Jellyfin.Plugin.AppleMusic.Dtos;
using Jellyfin.Plugin.AppleMusic.MetadataSources.Web.Schema;
using Jellyfin.Plugin.AppleMusic.Utils;
using MediaBrowser.Controller.Entities.Audio;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Jellyfin.Plugin.AppleMusic.MetadataSources.Web.Scrapers;

/// <summary>
/// Apple Music artist metadata scraper.
/// </summary>
public partial class ArtistScraper : IScraper<MusicArtist>
{
    private readonly ILogger<ArtistScraper> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ArtistScraper"/> class.
    /// </summary>
    /// <param name="logger">Logger factory.</param>
    public ArtistScraper(ILogger<ArtistScraper> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public IITunesItem? Scrape(IDocument document)
    {
        var script = document.GetElementById("schema:music-group");
        if (script is null)
        {
            _logger.LogDebug("No schema.org artist data found");
            return null;
        }

        var artist = ParseArtist(script.TextContent, document.Url);
        if (artist is not null)
        {
            _logger.LogDebug("Artist scraping completed");
        }

        return artist;
    }

    private ITunesArtist? ParseArtist(string json, string url)
    {
        MusicGroup? artistData;
        try
        {
            artistData = JsonConvert.DeserializeObject<MusicGroup>(json);
        }
        catch (JsonException ex)
        {
            _logger.LogDebug(ex, "Failed to parse schema.org (MusicGroup) data");
            return null;
        }

        if (string.IsNullOrEmpty(artistData?.Name))
        {
            _logger.LogDebug("Artist name not available");
            return null;
        }

        return new ITunesArtist
        {
            Name = artistData.Name.Trim(),
            ImageUrl = artistData.Image is null ? null : PluginUtils.UpdateImageSize(artistData.Image, "1400x1400cc"),
            About = artistData.Description is null ? null : HtmlTagRegex().Replace(WebUtility.HtmlDecode(artistData.Description), string.Empty),
            Url = url,
            Id = PluginUtils.GetIdFromUrl(url),
        };
    }

    [GeneratedRegex("<[^>]+>")]
    private static partial Regex HtmlTagRegex();
}
