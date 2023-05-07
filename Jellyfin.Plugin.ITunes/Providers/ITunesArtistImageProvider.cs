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
public class ITunesArtistImageProvider : IRemoteImageProvider
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<ITunesArtistImageProvider> _logger;
    private readonly IScraper<MusicArtist> _scraper;

    /// <summary>
    /// Initializes a new instance of the <see cref="ITunesArtistImageProvider"/> class.
    /// </summary>
    /// <param name="httpClientFactory">Instance of the <see cref="IHttpClientFactory"/> interface.</param>
    /// <param name="loggerFactory">Logger factory.</param>
    /// <param name="scraper">Scraper instance. If null, a default instance will be used.</param>
    public ITunesArtistImageProvider(IHttpClientFactory httpClientFactory, ILoggerFactory loggerFactory, IScraper<MusicArtist>? scraper = null)
    {
        _httpClientFactory = httpClientFactory;
        _logger = loggerFactory.CreateLogger<ITunesArtistImageProvider>();
        _scraper = scraper ?? new ArtistScraper(httpClientFactory, loggerFactory);
    }

    /// <inheritdoc />
    public string Name => ITunesPlugin.Instance?.Name ?? "Apple Music";

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

        return await _scraper.GetImages(artist.Name, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public bool Supports(BaseItem item) => item is MusicArtist;
}
