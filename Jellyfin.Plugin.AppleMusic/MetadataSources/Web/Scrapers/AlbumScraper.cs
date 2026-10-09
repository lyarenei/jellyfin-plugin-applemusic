using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using AngleSharp.Dom;
using AngleSharp.XPath;
using Jellyfin.Plugin.AppleMusic.Dtos;
using Jellyfin.Plugin.AppleMusic.MetadataSources.Web.Schema;
using Jellyfin.Plugin.AppleMusic.Utils;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using MusicAlbum = MediaBrowser.Controller.Entities.Audio.MusicAlbum;

namespace Jellyfin.Plugin.AppleMusic.MetadataSources.Web.Scrapers;

/// <summary>
/// Apple Music album metadata scraper.
/// </summary>
public class AlbumScraper : IScraper<MusicAlbum>
{
    private const string AlbumDetailXPath = "//div[@data-testid='container-detail-header']";
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

        var artists = ParseAlbumArtists(albumData.ByArtist, document);

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

    private List<ITunesArtist> ParseAlbumArtists(IReadOnlyList<MusicGroup>? albumArtists, IDocument document)
    {
        // Artists from albumData can be scraped (have URL)
        var artists = (albumArtists ?? [])
            .Where(artist => !string.IsNullOrEmpty(artist.Name))
            .Select(artist => new ITunesArtist { Name = artist.Name!.Trim(), Url = artist.Url ?? string.Empty })
            .ToList();

        if (artists.Count > 0)
        {
            _logger.LogDebug("Parsed {Count} artists from album", artists.Count);
            return artists;
        }

        _logger.LogDebug("No scrape-able artists are available, trying to parse artists from subtitle");

        // Artists may still be outside albumData (compilations, etc...), these have no URLs
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
                _logger.LogDebug("Artist name is empty, skipping");
                continue;
            }

            artists.Add(new ITunesArtist { Name = node.TextContent.Trim(), });
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
