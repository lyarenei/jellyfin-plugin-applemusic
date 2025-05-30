using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using AngleSharp;
using AngleSharp.Dom;
using AngleSharp.Html.Dom;
using AngleSharp.XPath;
using Jellyfin.Plugin.ITunes.Dtos;
using MediaBrowser.Controller.Entities.Audio;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.ITunes.Scrapers;

/// <summary>
/// Apple Music album metadata scraper.
/// </summary>
public class AlbumScraper : IScraper<MusicAlbum>
{
    private const string ImageXPath = "//meta[@property='og:image']/@content";
    private const string AlbumDetailXPath = "//div[@data-testid='container-detail-header']";
    private const string AlbumNameXPath = "//h1[@data-testid='non-editable-product-title']";
    private const string AlbumArtistXPath = "//a[@data-testid='click-action']";
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
            _logger.LogDebug("Album name not found");
            return null;
        }

        var imageUrl = document.Head.SelectSingleNode(ImageXPath)?.TextContent;
        if (imageUrl is null)
        {
            _logger.LogError("No album image found");
        }

        var artistNodes = document.Body.SelectNodes(AlbumDetailXPath + AlbumArtistXPath);
        if (artistNodes is null || artistNodes.Count == 0)
        {
            _logger.LogDebug("No album artists found");
            return null;
        }

        var artists = new List<ITunesArtist>();
        foreach (var node in artistNodes)
        {
            if (node is IHtmlAnchorElement artistElem)
            {
                artists.Add(new ITunesArtist
                {
                    Name = artistElem.TextContent,
                    Url = artistElem.Href
                });
            }
        }

        var aboutText = document.Body.SelectSingleNode(AlbumDetailXPath + AboutXPath)?.TextContent;
        var descString = document.Body.SelectSingleNode(AlbumDescriptionXPath)?.TextContent;
        var parsedDesc = ParseDescription(descString);
        return new ITunesAlbum
        {
            Name = albumName.Trim(),
            Artists = artists,
            ImageUrl = imageUrl,
            ReleaseDate = parsedDesc?.Date,
            About = aboutText
        };
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
}
