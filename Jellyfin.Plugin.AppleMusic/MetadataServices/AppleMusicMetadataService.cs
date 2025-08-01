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
using Jellyfin.Plugin.AppleMusic.Scrapers;
using MediaBrowser.Controller.Entities.Audio;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.AppleMusic.MetadataServices;

/// <summary>
/// Service wrapper for interacting with Apple Music website.
/// </summary>
public class AppleMusicMetadataService : IMetadataService
{
    private readonly ILogger<AppleMusicMetadataService> _logger;
    private readonly IConfiguration _config;
    private readonly IScraper<MusicAlbum> _albumScraper;
    private readonly IScraper<MusicArtist> _artistScraper;

    /// <summary>
    /// Initializes a new instance of the <see cref="AppleMusicMetadataService"/> class.
    /// </summary>
    /// <param name="loggerFactory">Logger factory.</param>
    /// <param name="albumScraper">Album scraper. If null, a default instance will be used.</param>
    /// <param name="artistScraper">Artist scraper. If null, a default instance will be used.</param>
    public AppleMusicMetadataService(ILoggerFactory loggerFactory, IScraper<MusicAlbum>? albumScraper = null, IScraper<MusicArtist>? artistScraper = null)
    {
        _logger = loggerFactory.CreateLogger<AppleMusicMetadataService>();
        _config = AngleSharp.Configuration.Default.WithDefaultLoader();
        _albumScraper = albumScraper ?? new AlbumScraper(loggerFactory.CreateLogger<AlbumScraper>());
        _artistScraper = artistScraper ?? new ArtistScraper(loggerFactory.CreateLogger<ArtistScraper>());
    }

    /// <inheritdoc />
    public async Task<ICollection<string>> Search(string searchTerm, ItemType type, CancellationToken cancellationToken)
    {
        var encodedTerm = Uri.EscapeDataString(searchTerm);
        var searchUrl = $"http://music.apple.com/us/search?term={encodedTerm}";

        _logger.LogDebug("Using {Url} for search", searchUrl);

        var document = await OpenPage(searchUrl).ConfigureAwait(false);
        var nodes = document.Body.SelectNodes(SearchResultXPath(type));
        return (from IHtmlAnchorElement elemNode in nodes select elemNode.Href).ToList();
    }

    /// <inheritdoc />
    public async Task<IITunesItem?> Scrape(string url, ItemType type)
    {
        var document = await OpenPage(url).ConfigureAwait(false);
        var item = type switch
        {
            ItemType.Album => await ScrapeAlbum(document).ConfigureAwait(false),
            ItemType.Artist => _artistScraper.Scrape(document),
            _ => null
        };

        if (item is not null)
        {
            item.Url = url;
            item.Id = url.Split('/').LastOrDefault(string.Empty);
        }

        return item;
    }

    private async Task<ITunesAlbum?> ScrapeAlbum(IDocument document)
    {
        // Scrape album data.
        var scrapedAlbum = _albumScraper.Scrape(document);
        if (scrapedAlbum is not ITunesAlbum album)
        {
            return null;
        }

        // Scrape artists found in album.
        var scrapedArtists = new List<ITunesArtist>();
        foreach (var artist in album.Artists)
        {
            var scrapedArtist = await Scrape(artist.Url, ItemType.Artist).ConfigureAwait(false);
            if (scrapedArtist is ITunesArtist newArtist)
            {
                scrapedArtists.Add(newArtist);
            }
        }

        album.Artists = scrapedArtists;
        return album;
    }

    private async Task<IDocument> OpenPage(string url)
    {
        var context = BrowsingContext.New(_config);
        return await context.OpenAsync(url).ConfigureAwait(false);
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
