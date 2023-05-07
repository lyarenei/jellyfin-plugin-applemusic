using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.ITunes.Scrapers;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.ITunes.Providers;

/// <summary>
/// The iTunes artist metadata provider.
/// </summary>
public class ITunesArtistMetadataProvider : IRemoteMetadataProvider<MusicArtist, ArtistInfo>
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<ITunesArtistMetadataProvider> _logger;
    private readonly IScraper<MusicArtist> _scraper;

    /// <summary>
    /// Initializes a new instance of the <see cref="ITunesArtistMetadataProvider"/> class.
    /// </summary>
    /// <param name="httpClientFactory">HTTP client factory.</param>
    /// <param name="loggerFactory">Logger factory.</param>
    /// <param name="scraper">Scraper instance. If null, a defaults instance will be used.</param>
    public ITunesArtistMetadataProvider(IHttpClientFactory httpClientFactory, ILoggerFactory loggerFactory, IScraper<MusicArtist>? scraper = null)
    {
        _httpClientFactory = httpClientFactory;
        _logger = loggerFactory.CreateLogger<ITunesArtistMetadataProvider>();
        _scraper = scraper ?? new ArtistScraper(httpClientFactory, loggerFactory);
    }

    /// <inheritdoc />
    public string Name => ITunesPlugin.Instance?.Name ?? "Apple Music";

    /// <inheritdoc />
    public async Task<MetadataResult<MusicArtist>> GetMetadata(ArtistInfo info, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(info.Name))
        {
            _logger.LogDebug("Empty artist name, skipping");
            return EmptyMetadataResult();
        }

        var results = await _scraper.Search(info.Name, cancellationToken).ConfigureAwait(false);
        var resultList = results.ToList();
        if (!resultList.Any())
        {
            _logger.LogInformation("No results found for {Artist}", info.Name);
            return EmptyMetadataResult();
        }

        var result = resultList.First();
        var metadataResult = new MetadataResult<MusicArtist>
        {
            Item = new MusicArtist
            {
                Name = result.Name,
                Overview = result.Overview
            },
            HasMetadata = true,
            RemoteImages = new List<(string Url, ImageType Type)> { (result.ImageUrl, ImageType.Primary) }
        };

        return metadataResult;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<RemoteSearchResult>> GetSearchResults(ArtistInfo searchInfo, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(searchInfo.Name))
        {
            return Enumerable.Empty<RemoteSearchResult>();
        }

        return await _scraper.Search(searchInfo.Name, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<HttpResponseMessage> GetImageResponse(string url, CancellationToken cancellationToken)
    {
        var httpClient = _httpClientFactory.CreateClient(NamedClient.Default);
        return await httpClient.GetAsync(new Uri(url), cancellationToken).ConfigureAwait(false);
    }

    private static MetadataResult<MusicArtist> EmptyMetadataResult()
    {
        return new MetadataResult<MusicArtist>
        {
            Item = new MusicArtist(),
            HasMetadata = false
        };
    }
}
