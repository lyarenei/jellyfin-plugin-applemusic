using System;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using AngleSharp.Dom;
using Jellyfin.Plugin.AppleMusic.Dtos;
using Jellyfin.Plugin.AppleMusic.MetadataSources.Web.Schema;
using Jellyfin.Plugin.AppleMusic.Utils;
using MediaBrowser.Controller.Entities.Audio;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

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
            artist.BackdropImageUrl = GetBackdropUrl(artist, document);
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
            About = artistData.Description is null ? null : SanitizeDescription(artistData.Description),
            Url = url,
            Id = PluginUtils.GetIdFromUrl(url),
        };
    }

    /// <summary>
    /// Get the backdrop URL from the wide artwork of the page header, falling back to the artist photo.
    /// </summary>
    private string? GetBackdropUrl(ITunesArtist artist, IDocument document)
    {
        var imageUrl = GetWideArtworkUrl(document, artist.Id) ?? GetArtistPhotoUrl(artist);
        if (imageUrl is null)
        {
            _logger.LogDebug("No suitable artist backdrop image found");
            return null;
        }

        return PluginUtils.UpdateImageSize(imageUrl, "1920x1080sr");
    }

    /// <summary>
    /// Get the artist photo URL, if the artist has one.
    /// </summary>
    private static string? GetArtistPhotoUrl(ITunesArtist artist)
    {
        var imageUrl = artist.ImageUrl;
        if (imageUrl is null)
        {
            return null;
        }

        return imageUrl.Contains("/AMCArtistImages", StringComparison.Ordinal) ? imageUrl : null;
    }

    /// <summary>
    /// Get the wide artwork URL of the artist (not e.g. a similar artist), found in the page's serialized data.
    /// </summary>
    private string? GetWideArtworkUrl(IDocument document, string artistId)
    {
        var script = document.GetElementById("serialized-server-data");
        if (script is null)
        {
            _logger.LogDebug("Serialized page data not found");
            return null;
        }

        try
        {
            return JToken.Parse(script.TextContent)
                .SelectTokens($"$..[?(@.contentDescriptor.identifiers.storeAdamID == '{artistId}')].wideArtwork.dictionary.url")
                .FirstOrDefault()?
                .Value<string>();
        }
        catch (JsonException ex)
        {
            _logger.LogDebug(ex, "Failed to parse serialized page data");
            return null;
        }
    }

    private static string SanitizeDescription(string description)
    {
        var text = WebUtility.HtmlDecode(description);
        text = BrTagRegex().Replace(text, "\n");
        return HtmlTagRegex().Replace(text, string.Empty);
    }

    [GeneratedRegex(@"<br\s*/?>", RegexOptions.IgnoreCase)]
    private static partial Regex BrTagRegex();

    [GeneratedRegex("<[^>]+>")]
    private static partial Regex HtmlTagRegex();
}
