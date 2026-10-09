using System.Text.Json;
using System.Text.RegularExpressions;
using AngleSharp;
using AngleSharp.Dom;
using Jellyfin.Plugin.AppleMusic.Dtos;
using Jellyfin.Plugin.AppleMusic.Utils;
using MediaBrowser.Controller.Entities.Audio;
using Microsoft.Extensions.Logging;

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
        AngleSharp.Configuration.Default.WithDefaultLoader();
    }

    /// <inheritdoc />
    public IITunesItem? Scrape(IDocument document)
    {
        var artist = ScrapeSchema(document);
        if (artist is not null)
        {
            _logger.LogDebug("Artist scraping completed");
        }

        return artist;
    }

    /// <summary>
    /// Scrape the schema.org JSON-LD data embedded in the artist page.
    /// This is more stable than the page markup, which Apple changes from time to time.
    /// </summary>
    private ITunesArtist? ScrapeSchema(IDocument document)
    {
        foreach (var script in document.QuerySelectorAll("script[type='application/ld+json']"))
        {
            try
            {
                using var json = JsonDocument.Parse(script.TextContent);
                var root = json.RootElement;
                if (root.ValueKind != JsonValueKind.Object
                    || !root.TryGetProperty("@type", out var type)
                    || type.GetString() is not ("MusicGroup" or "Person")
                    || GetString(root, "name") is not { Length: > 0 } name)
                {
                    continue;
                }

                var imageUrl = GetString(root, "image");
                var about = GetString(root, "description");
                return new ITunesArtist
                {
                    Name = name.Trim(),
                    ImageUrl = imageUrl is null ? null : PluginUtils.UpdateImageSize(imageUrl, "1400x1400cc"),
                    About = about is null ? null : HtmlTagRegex().Replace(about, string.Empty),
                    Url = document.Url,
                    Id = PluginUtils.GetIdFromUrl(document.Url),
                };
            }
            catch (JsonException ex)
            {
                _logger.LogDebug(ex, "Failed to parse schema.org data");
            }
        }

        _logger.LogTrace("No schema.org artist data found");
        return null;
    }

    private static string? GetString(JsonElement element, string property)
    {
        return element.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;
    }

    [GeneratedRegex("<[^>]+>")]
    private static partial Regex HtmlTagRegex();
}
