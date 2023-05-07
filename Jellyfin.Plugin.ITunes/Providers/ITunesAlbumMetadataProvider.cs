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
/// The iTunes album metadata provider.
/// </summary>
public class ITunesAlbumMetadataProvider : IRemoteMetadataProvider<MusicAlbum, AlbumInfo>
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<ITunesAlbumMetadataProvider> _logger;
    private readonly IScraper<MusicAlbum> _scraper;

    /// <summary>
    /// Initializes a new instance of the <see cref="ITunesAlbumMetadataProvider"/> class.
    /// </summary>
    /// <param name="httpClientFactory">HTTP client factory.</param>
    /// <param name="loggerFactory">Logger factory.</param>
    /// <param name="scraper">Scraper instance. If null, a default instance will be used.</param>
    public ITunesAlbumMetadataProvider(IHttpClientFactory httpClientFactory, ILoggerFactory loggerFactory, IScraper<MusicAlbum>? scraper = null)
    {
        _httpClientFactory = httpClientFactory;
        _logger = loggerFactory.CreateLogger<ITunesAlbumMetadataProvider>();
        _scraper = scraper ?? new AlbumScraper(httpClientFactory, loggerFactory);
    }

    /// <inheritdoc />
    public string Name => ITunesPlugin.Instance?.Name ?? "Apple Music";

    /// <inheritdoc />
    public async Task<IEnumerable<RemoteSearchResult>> GetSearchResults(AlbumInfo searchInfo, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(searchInfo.Name))
        {
            _logger.LogInformation("Empty album name, cannot search");
            return Enumerable.Empty<RemoteSearchResult>();
        }

        return await _scraper.Search(searchInfo.Name, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<MetadataResult<MusicAlbum>> GetMetadata(AlbumInfo info, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(info.Name))
        {
            _logger.LogDebug("Empty album name, skipping");
            return EmptyResult();
        }

        var albumArtist = info.AlbumArtists.FirstOrDefault(string.Empty);
        var term = $"{albumArtist} {info.Name}";
        var results = await _scraper.Search(term, cancellationToken).ConfigureAwait(false);
        var resultList = results.ToList();
        if (!resultList.Any())
        {
            _logger.LogInformation("No results found for {Term}", term);
            return EmptyResult();
        }

        var result = resultList.First();
        var metadataResult = new MetadataResult<MusicAlbum>
        {
            Item = new MusicAlbum { Name = result.Name },
            HasMetadata = true,
            RemoteImages = new List<(string Url, ImageType Type)> { (result.ImageUrl, ImageType.Primary) }
        };

        return metadataResult;
    }

    /// <inheritdoc />
    public async Task<HttpResponseMessage> GetImageResponse(string url, CancellationToken cancellationToken)
    {
        var httpClient = _httpClientFactory.CreateClient(NamedClient.Default);
        return await httpClient.GetAsync(new Uri(url), cancellationToken).ConfigureAwait(false);
    }

    private static MetadataResult<MusicAlbum> EmptyResult()
    {
        return new MetadataResult<MusicAlbum> { HasMetadata = false };
    }
}
