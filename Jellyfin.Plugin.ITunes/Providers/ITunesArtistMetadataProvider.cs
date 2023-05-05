using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using HtmlAgilityPack;
using Jellyfin.Plugin.ITunes.Dtos;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Providers;
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

    /// <summary>
    /// Initializes a new instance of the <see cref="ITunesArtistMetadataProvider"/> class.
    /// </summary>
    /// <param name="httpClientFactory">HTTP client factory.</param>
    /// <param name="loggerFactory">Logger factory.</param>
    public ITunesArtistMetadataProvider(IHttpClientFactory httpClientFactory, ILoggerFactory loggerFactory)
    {
        _httpClientFactory = httpClientFactory;
        _logger = loggerFactory.CreateLogger<ITunesArtistMetadataProvider>();
    }

    /// <inheritdoc />
    public string Name => "Apple Music";

    /// <inheritdoc />
    public async Task<MetadataResult<MusicArtist>> GetMetadata(ArtistInfo info, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(info.Name))
        {
            _logger.LogDebug("Empty artist name, skipping");
            return EmptyResult();
        }

        var encodedName = Uri.EscapeDataString(info.Name);
        return await GetMetadataInternal(
            $"https://itunes.apple.com/search?term=${encodedName}&media=music&entity=musicArtist&attribute=artistTerm",
            cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public Task<IEnumerable<RemoteSearchResult>> GetSearchResults(ArtistInfo searchInfo, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc />
    public async Task<HttpResponseMessage> GetImageResponse(string url, CancellationToken cancellationToken)
    {
        var httpClient = _httpClientFactory.CreateClient(NamedClient.Default);
        return await httpClient.GetAsync(new Uri(url), cancellationToken).ConfigureAwait(false);
    }

    private async Task<MetadataResult<MusicArtist>> GetMetadataInternal(string url, CancellationToken cancellationToken)
    {
        var iTunesArtistDto = await _httpClientFactory
            .CreateClient(NamedClient.Default)
            .GetFromJsonAsync<ITunesArtistDto>(new Uri(url), cancellationToken)
            .ConfigureAwait(false);

        if (iTunesArtistDto is null || iTunesArtistDto.ResultCount < 1)
        {
            _logger.LogDebug("No results for url {Url}", url);
            return EmptyResult();
        }

        var result = iTunesArtistDto.Results.First();
        if (result.ArtistLinkUrl is null)
        {
            _logger.LogDebug("No artist links found in results");
            return EmptyResult();
        }

        _logger.LogDebug("URL: {Url}", result.ArtistLinkUrl);

        HtmlWeb web = new HtmlWeb();
        var doc = web.Load(new Uri(result.ArtistLinkUrl));
        if (doc?.CreateNavigator() is not HtmlNodeNavigator navigator)
        {
            _logger.LogDebug("Failed to create a scraping navigator");
            return EmptyResult();
        }

        var aboutArtistNode = navigator.SelectSingleNode("//p[@data-testid=\"content-modal-text\"]");
        if (aboutArtistNode is null)
        {
            _logger.LogDebug("Failed to get full about artist node, will fall back to truncated");
            aboutArtistNode = navigator.SelectSingleNode("//p[@data-testid=\"trruncate-text\"]");
        }

        if (aboutArtistNode is null)
        {
            _logger.LogDebug("Failed to get truncated about artist, giving up");
            return EmptyResult();
        }

        var musicArtist = new MusicArtist { Overview = aboutArtistNode.Value };
        return new MetadataResult<MusicArtist>
        {
            Item = musicArtist,
            HasMetadata = true,
        };
    }

    private static MetadataResult<MusicArtist> EmptyResult()
    {
        return new MetadataResult<MusicArtist>
        {
            Item = new MusicArtist(),
            HasMetadata = false
        };
    }
}
