using System;
using System.Net;
using System.Text.Json;
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
    private static readonly string[] _backdropArtworkKeys = ["wideArtwork", "colorBackdropArtwork", "artwork"];

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
            SetBackdrop(artist, document);
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
        catch (Newtonsoft.Json.JsonException ex)
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
            About = artistData.Description is null ? null : SanitizeDescription(artistData.Description),
            Url = url,
            Id = PluginUtils.GetIdFromUrl(url),
        };
    }

    /// <summary>
    /// Set the backdrop from the wide artwork of the page header, found in the page's serialized data.
    /// Some artists have a dedicated wide image, others only a square photo; either way Apple crops it
    /// to the requested size.
    /// </summary>
    private void SetBackdrop(ITunesArtist artist, IDocument document)
    {
        var script = document.QuerySelector("script#serialized-server-data");
        if (script is null)
        {
            _logger.LogTrace("Serialized page data not found");
            return;
        }

        try
        {
            using var json = JsonDocument.Parse(script.TextContent);
            if (FindWideArtwork(json.RootElement, artist.Id) is not { } artwork)
            {
                _logger.LogTrace("Artist wide artwork not found");
                return;
            }

            artist.BackdropImageUrl = PluginUtils.UpdateImageSize(artwork.Url, "1920x1080sr");
        }
        catch (System.Text.Json.JsonException ex)
        {
            _logger.LogDebug(ex, "Failed to parse serialized page data");
        }
    }

    /// <summary>
    /// Find the backdrop artwork of the item describing the artist itself (not e.g. a similar artist).
    /// </summary>
    private static (string Url, int Width, int Height)? FindWideArtwork(JsonElement element, string artistId)
    {
        if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in element.EnumerateArray())
            {
                if (FindWideArtwork(item, artistId) is { } found)
                {
                    return found;
                }
            }
        }
        else if (element.ValueKind == JsonValueKind.Object)
        {
            if (element.TryGetProperty("contentDescriptor", out var descriptor)
                && descriptor.ValueKind == JsonValueKind.Object
                && descriptor.TryGetProperty("identifiers", out var identifiers)
                && GetString(identifiers, "storeAdamID") == artistId)
            {
                // Not every artist has dedicated wide artwork; the artist photo is the next best thing.
                // Artists without a photo get an album cover as artwork, which makes a poor backdrop.
                foreach (var key in _backdropArtworkKeys)
                {
                    if (element.TryGetProperty(key, out var candidate)
                        && candidate.ValueKind == JsonValueKind.Object
                        && candidate.TryGetProperty("dictionary", out var artwork)
                        && GetString(artwork, "url") is { } url
                        && (key == "wideArtwork" || url.Contains("/AMCArtistImages", StringComparison.Ordinal))
                        && artwork.TryGetProperty("width", out var width) && width.TryGetInt32(out var widthValue)
                        && artwork.TryGetProperty("height", out var height) && height.TryGetInt32(out var heightValue))
                    {
                        return (url, widthValue, heightValue);
                    }
                }
            }

            foreach (var property in element.EnumerateObject())
            {
                if (FindWideArtwork(property.Value, artistId) is { } found)
                {
                    return found;
                }
            }
        }

        return null;
    }

    private static string SanitizeDescription(string description)
    {
        var text = WebUtility.HtmlDecode(description);
        text = BrTagRegex().Replace(text, "\n");
        return HtmlTagRegex().Replace(text, string.Empty);
    }

    [GeneratedRegex(@"<br\s*/?>", RegexOptions.IgnoreCase)]
    private static partial Regex BrTagRegex();

    private static string? GetString(JsonElement element, string property)
    {
        return element.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;
    }

    [GeneratedRegex("<[^>]+>")]
    private static partial Regex HtmlTagRegex();
}
