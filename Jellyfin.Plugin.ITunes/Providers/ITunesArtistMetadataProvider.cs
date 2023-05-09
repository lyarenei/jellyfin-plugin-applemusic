using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.ITunes.ExternalIds;
using Jellyfin.Plugin.ITunes.MetadataServices;
using Jellyfin.Plugin.ITunes.Utils;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;
using Microsoft.Extensions.Logging;
using IMetadataService = Jellyfin.Plugin.ITunes.MetadataServices.IMetadataService;

namespace Jellyfin.Plugin.ITunes.Providers;

/// <summary>
/// The iTunes artist metadata provider.
/// </summary>
public class ITunesArtistMetadataProvider : IRemoteMetadataProvider<MusicArtist, ArtistInfo>
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<ITunesArtistMetadataProvider> _logger;
    private readonly IMetadataService _service;

    /// <summary>
    /// Initializes a new instance of the <see cref="ITunesArtistMetadataProvider"/> class.
    /// </summary>
    /// <param name="httpClientFactory">HTTP client factory.</param>
    /// <param name="loggerFactory">Logger factory.</param>
    /// <param name="service">Metadata service instance. If null, a default instance will be used.</param>
    public ITunesArtistMetadataProvider(IHttpClientFactory httpClientFactory, ILoggerFactory loggerFactory, IMetadataService? service = null)
    {
        _httpClientFactory = httpClientFactory;
        _logger = loggerFactory.CreateLogger<ITunesArtistMetadataProvider>();
        _service = service ?? new AppleMusicMetadataService(loggerFactory);
    }

    /// <inheritdoc />
    public string Name => PluginUtils.PluginName;

    /// <inheritdoc />
    public async Task<MetadataResult<MusicArtist>> GetMetadata(ArtistInfo info, CancellationToken cancellationToken)
    {
        var results = await GetUrlsForScraping(info, cancellationToken).ConfigureAwait(false);
        if (!results.Any())
        {
            return EmptyMetadataResult();
        }

        var result = results.First();
        var scrapedArtist = await _service.Scrape(result, ItemType.Artist).ConfigureAwait(false);
        if (scrapedArtist is null)
        {
            _logger.LogDebug("Failed to scrape artist data form {Url}", result);
            return EmptyMetadataResult();
        }

        var metadataResult = new MetadataResult<MusicArtist>
        {
            Item = new MusicArtist
            {
                Name = scrapedArtist.Name,
                Overview = scrapedArtist.About
            },
            HasMetadata = scrapedArtist.HasMetadata()
        };

        if (scrapedArtist.ImageUrl is not null)
        {
            metadataResult.RemoteImages.Add((scrapedArtist.ImageUrl, ImageType.Primary));
        }

        return metadataResult;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<RemoteSearchResult>> GetSearchResults(ArtistInfo searchInfo, CancellationToken cancellationToken)
    {
        var results = await GetUrlsForScraping(searchInfo, cancellationToken).ConfigureAwait(false);
        var searchResults = new List<RemoteSearchResult>();
        foreach (var result in results)
        {
            var scrapeResult = await _service.Scrape(result, ItemType.Artist).ConfigureAwait(false);
            if (scrapeResult is null)
            {
                continue;
            }

            searchResults.Add(scrapeResult.ToRemoteSearchResult());
        }

        return searchResults;
    }

    /// <inheritdoc />
    public async Task<HttpResponseMessage> GetImageResponse(string url, CancellationToken cancellationToken)
    {
        var httpClient = _httpClientFactory.CreateClient(NamedClient.Default);
        return await httpClient.GetAsync(new Uri(url), cancellationToken).ConfigureAwait(false);
    }

    private static MetadataResult<MusicArtist> EmptyMetadataResult()
    {
        return new MetadataResult<MusicArtist> { HasMetadata = false };
    }

    private async Task<ICollection<string>> GetUrlsForScraping(ArtistInfo info, CancellationToken cancellationToken)
    {
        var providerUrl = PluginUtils.GetProviderUrl(info, ITunesProviderKey.AlbumArtist);
        if (string.IsNullOrEmpty(providerUrl))
        {
            return await _service.Search(info.Name, ItemType.Artist, cancellationToken).ConfigureAwait(false);
        }

        return new List<string> { providerUrl };
    }
}
