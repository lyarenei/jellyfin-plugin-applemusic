using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
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
/// Apple Music album metadata scraper.
/// </summary>
public class AlbumScraper : IScraper<MusicAlbum>
{
    private const string ImageXPath = "//div[@data-testid='container-detail-header']" +
                                      "//div[@data-testid='artwork-component']" +
                                      "//source[@type='image/jpeg']/@srcset";

    private const string AlbumDetailXPath = "//div[@data-testid='container-detail-header']";
    private const string AlbumNameXPath = "//h1[@data-testid='non-editable-product-title']";
    private const string AlbumArtistLinkXPath = "//a[@data-testid='click-action']";
    private const string AlbumArtistSubtitleXPath = "//div[@data-testid='product-subtitles']";
    private const string AboutXPath = "//p[@data-testid='truncate-text']";
    private const string AlbumDescriptionXPath = "//p[@data-testid='tracklist-footer-description']";

    private const string AlbumDescRegex = @"(?'date'\w+ \d+, \d+)\W(?'runtime'\d+)\W+(?'runtimeUnit'\w+)\W+(?'productionYear'\d+)\W+(?'producer'\w+)";

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
        var albumName = document.Body.SelectSingleNode(AlbumDetailXPath + AlbumNameXPath)?.TextContent;
        if (albumName is null)
        {
            _logger.LogTrace("Album name not found");
            return null;
        }

        _logger.LogTrace("Found album name");

        var imageUrl = GetImageUrl(document.Body);
        if (imageUrl is null)
        {
            _logger.LogTrace("No album image found");
        }
        else
        {
            _logger.LogTrace("Found album image");
        }

        _logger.LogDebug("Processing optional album details");

        var artists = ParseAlbumArtists(document);
        var aboutText = document.Body.SelectSingleNode(AlbumDetailXPath + AboutXPath)?.TextContent;
        var descString = document.Body.SelectSingleNode(AlbumDescriptionXPath)?.TextContent;
        var parsedDesc = ParseDescription(descString);

        _logger.LogDebug("Album scraping completed");

        return new ITunesAlbum
        {
            Name = albumName.Trim(),
            Artists = artists,
            ImageUrl = imageUrl,
            ReleaseDate = parsedDesc?.Date,
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

    private (DateTime Date, int ProductionYear)? ParseDescription(string? details)
    {
        if (details is null)
        {
            return null;
        }

        var match = Regex.Match(details, AlbumDescRegex, RegexOptions.Multiline);
        if (!match.Groups["date"].Success || !match.Groups["productionYear"].Success)
        {
            _logger.LogDebug("Failed to parse album details {Details}", details);
        }

        var date = DateTime.ParseExact(match.Groups["date"].Value, "MMMM d, yyyy", DateTimeFormatInfo.InvariantInfo);
        var prodYear = int.Parse(match.Groups["productionYear"].Value, NumberStyles.Any, NumberFormatInfo.InvariantInfo);
        return (date, prodYear);
    }

    private static string? GetImageUrl(IHtmlElement? body)
    {
        var content = body?.SelectSingleNode(ImageXPath)?.TextContent;
        return content?.Split(' ').FirstOrDefault();
    }
}
