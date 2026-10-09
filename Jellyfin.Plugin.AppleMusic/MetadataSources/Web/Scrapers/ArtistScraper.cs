using System.Text.Json;
using System.Text.RegularExpressions;
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

    private ITunesArtist? ParseArtist(string schemaJson, string url)
    {
        try
        {
            using var json = JsonDocument.Parse(schemaJson);
            var root = json.RootElement;
            if (root.ValueKind != JsonValueKind.Object || GetString(root, "name") is not { Length: > 0 } name)
            {
                _logger.LogTrace("Artist name not found");
                return null;
            }

            var imageUrl = GetString(root, "image");
            var about = GetString(root, "description");
            return new ITunesArtist
            {
                Name = name.Trim(),
                ImageUrl = imageUrl is null ? null : PluginUtils.UpdateImageSize(imageUrl, "1400x1400cc"),
                About = about is null ? null : HtmlTagRegex().Replace(about, string.Empty),
                Url = url,
                Id = PluginUtils.GetIdFromUrl(url),
            };
        }
        catch (JsonException ex)
        {
            _logger.LogDebug(ex, "Failed to parse schema.org data");
            return null;
        }
    }

    private static string? GetString(JsonElement element, string property)
    {
        return element.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;
    }

    [GeneratedRegex("<[^>]+>")]
    private static partial Regex HtmlTagRegex();
}
