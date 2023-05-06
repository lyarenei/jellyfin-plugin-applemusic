using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.ITunes.Scrapers;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.ITunes.Providers;

/// <summary>
/// The iTunes album image provider.
/// </summary>
public class ITunesAlbumImageProvider : IRemoteImageProvider, IHasOrder
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<ITunesAlbumImageProvider> _logger;
    private readonly IScraper<AlbumScraper> _scraper;

    /// <summary>
    /// Initializes a new instance of the <see cref="ITunesAlbumImageProvider"/> class.
    /// </summary>
    /// <param name="httpClientFactory">Instance of the <see cref="IHttpClientFactory"/> interface.</param>
    /// <param name="logger">Instance of the <see cref="ILogger"/> interface.</param>
    /// <param name="scraper">Scraper instance.</param>
    public ITunesAlbumImageProvider(IHttpClientFactory httpClientFactory, ILogger<ITunesAlbumImageProvider> logger, IScraper<AlbumScraper> scraper)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _scraper = scraper;
    }

    /// <inheritdoc />
    public string Name => "Apple Music";

    /// <inheritdoc />
    public int Order => 1; // After embedded provider

    /// <inheritdoc />
    public bool Supports(BaseItem item) => item is MusicAlbum;

    /// <inheritdoc />
    public IEnumerable<ImageType> GetSupportedImages(BaseItem item)
    {
        return new List<ImageType> { ImageType.Primary };
    }

    /// <inheritdoc />
    public async Task<HttpResponseMessage> GetImageResponse(string url, CancellationToken cancellationToken)
    {
        var httpClient = _httpClientFactory.CreateClient(NamedClient.Default);
        return await httpClient.GetAsync(new Uri(url), cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<RemoteImageInfo>> GetImages(BaseItem item, CancellationToken cancellationToken)
    {
        if (item is not MusicAlbum album)
        {
            _logger.LogDebug("Provided item is not an album, cannot continue");
            return new List<RemoteImageInfo>();
        }

        if (string.IsNullOrEmpty(album.Name))
        {
            _logger.LogInformation("No album name provided, cannot continue");
            return new List<RemoteImageInfo>();
        }

        var encodedName = Uri.EscapeDataString(album.Name);
        var searchUrl = $"https://itunes.apple.com/search?term={encodedName}&media=music&entity=album&attribute=albumTerm";
        return await _scraper.GetImages(searchUrl, cancellationToken).ConfigureAwait(false);
    }
}
