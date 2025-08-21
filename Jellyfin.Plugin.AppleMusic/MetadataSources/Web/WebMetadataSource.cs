using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AngleSharp;
using AngleSharp.Dom;
using AngleSharp.Html.Dom;
using AngleSharp.XPath;
using Jellyfin.Plugin.AppleMusic.Dtos;
using Jellyfin.Plugin.AppleMusic.MetadataSources.Web.Scrapers;
using Jellyfin.Plugin.AppleMusic.Utils;
using MediaBrowser.Controller.Entities.Audio;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.AppleMusic.MetadataSources.Web;

/// <summary>
/// Apple Music web metadata source.
/// This source scrapes metadata from the Apple Music website.
/// </summary>
public class WebMetadataSource : IMetadataSource
{
    private readonly ILogger<WebMetadataSource> _logger;
    private readonly IConfiguration _config;
    private readonly IScraper<MusicAlbum> _albumScraper;
    private readonly IScraper<MusicArtist> _artistScraper;

    /// <summary>
    /// Initializes a new instance of the <see cref="WebMetadataSource"/> class.
    /// </summary>
    /// <param name="loggerFactory">Logger factory.</param>
    /// <param name="albumScraper">Album scraper. If null, a default instance will be used.</param>
    /// <param name="artistScraper">Artist scraper. If null, a default instance will be used.</param>
    public WebMetadataSource(
        ILoggerFactory loggerFactory,
        IScraper<MusicAlbum>? albumScraper = null,
        IScraper<MusicArtist>? artistScraper = null)
    {
        _logger = loggerFactory.CreateLogger<WebMetadataSource>();
        _config = AngleSharp.Configuration.Default.WithDefaultLoader();
        _albumScraper = albumScraper ?? new AlbumScraper(loggerFactory.CreateLogger<AlbumScraper>());
        _artistScraper = artistScraper ?? new ArtistScraper(loggerFactory.CreateLogger<ArtistScraper>());
    }

    /// <inheritdoc />
    public async Task<List<IITunesItem>> SearchAsync(string searchTerm, ItemType itemType, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Searching for {ItemType} with term: {SearchTerm}", itemType, searchTerm);
        var encodedTerm = Uri.EscapeDataString(searchTerm);
        var searchUrl = $"{PluginUtils.AppleMusicBaseUrl}/search?term={encodedTerm}";

        _logger.LogDebug("Opening url: {Url}", searchUrl);
        var document = await OpenPage(searchUrl, cancellationToken);

        if (itemType is ItemType.Album)
        {
            var albumNodes = document.Body.SelectNodes(SearchResultXPath(ItemType.Album));
            var albums = await ScrapeAlbums(albumNodes, cancellationToken);
            _logger.LogInformation("Found {Count} albums for search term {SearchTerm}", albums.Count, searchTerm);
            return albums.Cast<IITunesItem>().ToList();
        }

        if (itemType is ItemType.Artist)
        {
            var artistNodes = document.Body.SelectNodes(SearchResultXPath(ItemType.Artist));
            var artists = await ScrapeArtists(artistNodes, cancellationToken);
            _logger.LogInformation("Found {Count} artists for search term {SearchTerm}", artists.Count, searchTerm);
            return artists.Cast<IITunesItem>().ToList();
        }

        _logger.LogWarning("Unsupported item type {ItemType} for search", itemType);
        return new List<IITunesItem>();
    }

    /// <inheritdoc />
    public async Task<ITunesAlbum?> GetAlbumAsync(string albumId, CancellationToken cancellationToken)
    {
        var albumUrl = $"{PluginUtils.AppleMusicBaseUrl}/album/{albumId}";
        _logger.LogDebug("Getting album data from {Url}", albumUrl);

        var document = await OpenPage(albumUrl, cancellationToken);
        return await ScrapeAlbum(document, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<ITunesArtist?> GetArtistAsync(string artistId, CancellationToken cancellationToken)
    {
        var artistUrl = $"{PluginUtils.AppleMusicBaseUrl}/artist/{artistId}";
        _logger.LogDebug("Getting artist data from {Url}", artistUrl);

        var document = await OpenPage(artistUrl, cancellationToken);
        return await ScrapeArtist(document, cancellationToken);
    }

    private async Task<IDocument> OpenPage(string url, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Opening page: {Url}", url);
        var context = BrowsingContext.New(_config);
        return await context.OpenAsync(url, cancellationToken);
    }

    private async Task<List<ITunesAlbum>> ScrapeAlbums(IEnumerable<INode> nodes, CancellationToken cancellationToken)
    {
        var tasks = nodes
            .Cast<IHtmlAnchorElement>()
            .Select(node => PluginUtils.GetIdFromUrl(node.Href))
            .Select(albumId => GetAlbumAsync(albumId, cancellationToken));

        var results = new List<ITunesAlbum>();
        foreach (var task in tasks)
        {
            _logger.LogTrace("Starting album scrape task, task ID {TaskId}", task.Id);
            cancellationToken.ThrowIfCancellationRequested();
            var result = await task;
            _logger.LogTrace("Finished album scrape task, task ID {TaskId}", task.Id);
            if (result is not null)
            {
                results.Add(result);
            }
        }

        return results;
    }

    private async Task<List<ITunesArtist>> ScrapeArtists(IEnumerable<INode> nodes, CancellationToken cancellationToken)
    {
        var tasks = nodes
            .Cast<IHtmlAnchorElement>()
            .Select(node => PluginUtils.GetIdFromUrl(node.Href))
            .Select(artistId => GetArtistAsync(artistId, cancellationToken));

        var results = new List<ITunesArtist>();
        foreach (var task in tasks)
        {
            _logger.LogTrace("Starting artist scrape task, task ID {TaskId}", task.Id);
            cancellationToken.ThrowIfCancellationRequested();
            var result = await task;
            _logger.LogTrace("Finished artist scrape task, task ID {TaskId}", task.Id);
            if (result is not null)
            {
                results.Add(result);
            }
        }

        return results;
    }

    private async Task<ITunesAlbum?> ScrapeAlbum(IDocument document, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _logger.LogDebug("Scraping album from {Url}", document.Url);
        var scrapedAlbum = _albumScraper.Scrape(document);
        if (scrapedAlbum is not ITunesAlbum album)
        {
            _logger.LogDebug("Scraping album failed");
            return null;
        }

        _logger.LogDebug("Scraped album from url {Url}", document.Url);

        var artistTasks = album.Artists
            .Select(artist => PluginUtils.GetIdFromUrl(artist.Url))
            .Select(artistId => GetArtistAsync(artistId, cancellationToken));

        var scrapedArtists = await Task.WhenAll(artistTasks);
        album.Artists = scrapedArtists.Where(artist => artist is not null)
            .Cast<ITunesArtist>()
            .ToList();

        return album;
    }

    private async Task<ITunesArtist?> ScrapeArtist(IDocument document, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var task = new Task<ITunesArtist?>(() =>
        {
            _logger.LogDebug("Scraping artist from {Url}", document.Url);
            var scrapedArtist = _artistScraper.Scrape(document);
            if (scrapedArtist is ITunesArtist artist)
            {
                return artist;
            }

            _logger.LogDebug("Scraping artist failed");
            return null;
        });

        task.Start();
        return await task.WaitAsync(cancellationToken);
    }

    private static string SearchResultXPath(ItemType type)
    {
        const string BaseXPath = "//div[@data-testid='section-container']";
        var ariaLabel = $"[@aria-label='{GetCategoryLabel(type)}']";
        const string TrailingXPath = "//li//a";
        return type switch
        {
            ItemType.Album => BaseXPath + ariaLabel + TrailingXPath + "[@data-testid='product-lockup-title']",
            _ => BaseXPath + ariaLabel + TrailingXPath
        };
    }

    private static string GetCategoryLabel(ItemType type)
    {
        return type switch
        {
            ItemType.Album => "Albums",
            ItemType.Artist => "Artists",
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };
    }
}
