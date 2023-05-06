using System;
using System.Collections.Generic;
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
/// The iTunes artist image provider.
/// </summary>
public class ITunesArtistImageProvider : IRemoteImageProvider, IHasOrder
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<ITunesArtistImageProvider> _logger;
    private readonly IScraper<ArtistScraper> _scraper;

    /// <summary>
    /// Initializes a new instance of the <see cref="ITunesArtistImageProvider"/> class.
    /// </summary>
    /// <param name="httpClientFactory">Instance of the <see cref="IHttpClientFactory"/> interface.</param>
    /// <param name="logger">Instance of the <see cref="ILogger"/> interface.</param>
    /// <param name="scraper">Scraper instance.</param>
    public ITunesArtistImageProvider(IHttpClientFactory httpClientFactory, ILogger<ITunesArtistImageProvider> logger, IScraper<ArtistScraper> scraper)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _scraper = scraper;
    }

    /// <summary>
    /// Gets the provider name.
    /// </summary>
    public string Name => ITunesPlugin.Instance?.Name ?? "Apple Music";

    /// <summary>
    /// Gets the provider order.
    /// </summary>
    // After fanart
    public int Order => 1;

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
        if (item is not MusicArtist artist)
        {
            _logger.LogDebug("Provided item is not an artist, cannot continue");
            return new List<RemoteImageInfo>();
        }

        if (string.IsNullOrEmpty(artist.Name))
        {
            _logger.LogInformation("No artist name provided, cannot continue");
            return new List<RemoteImageInfo>();
        }

        var encodedName = Uri.EscapeDataString(artist.Name);
        var searchUrl = $"https://itunes.apple.com/search?term=${encodedName}&media=music&entity=musicArtist&attribute=artistTerm";
        return await _scraper.GetImages(searchUrl, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public bool Supports(BaseItem item) => item is MusicArtist;
}
