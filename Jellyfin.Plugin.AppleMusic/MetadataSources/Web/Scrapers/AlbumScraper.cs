using System;
using System.Collections.Generic;
using System.Globalization;
using AngleSharp;
using AngleSharp.Dom;
using AngleSharp.Html.Dom;
using AngleSharp.XPath;
using Jellyfin.Plugin.AppleMusic.Dtos;
using Jellyfin.Plugin.AppleMusic.Utils;
using MediaBrowser.Controller.Entities.Audio;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Jellyfin.Plugin.AppleMusic.MetadataSources.Web.Scrapers;

/// <summary>
/// Apple Music album metadata scraper.
/// </summary>
public class AlbumScraper : IScraper<MusicAlbum>
{
    private const string AlbumDetailXPath = "//div[@data-testid='container-detail-header']";
    private const string AlbumArtistLinkXPath = "//a[@data-testid='click-action']";
    private const string AlbumArtistSubtitleXPath = "//div[@data-testid='product-subtitles']";
    private const string AboutXPath = "//p[@data-testid='truncate-text']";

    private readonly ILogger<AlbumScraper> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="AlbumScraper"/> class.
    /// </summary>
    /// <param name="logger">Logger instance.</param>
    public AlbumScraper(ILogger<AlbumScraper> logger)
    {
        _logger = logger;
        AngleSharp.Configuration.Default.WithDefaultLoader();
    }

    /// <inheritdoc />
    public IITunesItem? Scrape(IDocument document)
    {
        var script = document.GetElementById("schema:music-album");
        if (script is null)
        {
            _logger.LogDebug("No schema.org album data found");
            return null;
        }

        var albumData = ParseAlbumData(script.TextContent);
        if (string.IsNullOrEmpty(albumData?.Name))
        {
            _logger.LogDebug("Album name not available");
            return null;
        }

        _logger.LogDebug("Processing optional album details");

        var artists = ParseAlbumArtists(document);

        // albumData.Description contains generic text => read from HTML DOM
        var aboutText = document.Body.SelectSingleNode(AlbumDetailXPath + AboutXPath)?.TextContent;

        _logger.LogDebug("Album scraping completed");

        return new ITunesAlbum
        {
            Name = albumData.Name.Trim(),
            Artists = artists,
            ImageUrl = albumData.Image is null ? null : PluginUtils.UpdateImageSize(albumData.Image, "1400x1400cc"),
            ReleaseDate = ParseReleaseDate(albumData.DatePublished),
            About = aboutText,
            Url = document.Url,
            Id = PluginUtils.GetIdFromUrl(document.Url),
        };
    }

    private List<ITunesArtist> ParseAlbumArtists(IDocument document)
    {
        // Artists with links => we can scrape them
        var artistLinkNodes = document.Body.SelectNodes(AlbumDetailXPath + AlbumArtistLinkXPath);
        if (artistLinkNodes.Count > 0)
        {
            _logger.LogDebug("Found {Count} artist nodes in album", artistLinkNodes.Count);
            return ParseArtists(artistLinkNodes);
        }

        _logger.LogTrace("No album artists with links found, trying to parse artists from subtitle");

        // Artists without links => we can only get their names
        var artistSubtitleNodes = document.Body.SelectNodes(AlbumDetailXPath + AlbumArtistSubtitleXPath);
        if (artistSubtitleNodes.Count > 0)
        {
            _logger.LogDebug("Found {Count} artist subtitle nodes in album", artistSubtitleNodes.Count);
            return ParseArtists(artistSubtitleNodes);
        }

        _logger.LogDebug("No album artists found");
        return [];
    }

    private List<ITunesArtist> ParseArtists(IEnumerable<INode> artistNodes)
    {
        var artists = new List<ITunesArtist>();
        foreach (var node in artistNodes)
        {
            if (string.IsNullOrEmpty(node.TextContent))
            {
                _logger.LogTrace("Artist name is empty, skipping");
                continue;
            }

            var newArtist = new ITunesArtist
            {
                Name = node.TextContent.Trim(),
            };

            if (node is IHtmlAnchorElement anchor)
            {
                _logger.LogTrace("Adding URL to artist: {Url}", anchor.Href);
                newArtist.Url = anchor.Href;
            }

            artists.Add(newArtist);
        }

        return artists;
    }

    private Schema.MusicAlbum? ParseAlbumData(string json)
    {
        try
        {
            return JsonConvert.DeserializeObject<Schema.MusicAlbum>(json);
        }
        catch (JsonException ex)
        {
            _logger.LogDebug(ex, "Failed to parse schema.org (MusicAlbum) data");
            return null;
        }
    }

    private DateTime? ParseReleaseDate(string? date)
    {
        if (date is null)
        {
            return null;
        }

        if (DateTime.TryParseExact(date, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var releaseDate))
        {
            return releaseDate;
        }

        _logger.LogDebug("Failed to parse album release date {Date}", date);
        return null;
    }
}
