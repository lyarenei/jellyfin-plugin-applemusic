using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using AngleSharp;
using AngleSharp.XPath;
using Jellyfin.Plugin.ITunes.Dtos;
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
    private readonly IConfiguration _config;

    /// <summary>
    /// Initializes a new instance of the <see cref="ITunesArtistMetadataProvider"/> class.
    /// </summary>
    /// <param name="httpClientFactory">HTTP client factory.</param>
    /// <param name="logger">Logger instance.</param>
    public ITunesArtistMetadataProvider(IHttpClientFactory httpClientFactory, ILogger<ITunesArtistMetadataProvider> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _config = AngleSharp.Configuration.Default.WithDefaultLoader();
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

        return await GetMetadataInternal(GetSearchUrl(info.Name), cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<RemoteSearchResult>> GetSearchResults(ArtistInfo searchInfo, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(searchInfo.Name))
        {
            return Enumerable.Empty<RemoteSearchResult>();
        }

        var url = GetSearchUrl(searchInfo.Name);
        var iTunesArtistDto = await _httpClientFactory
            .CreateClient(NamedClient.Default)
            .GetFromJsonAsync<ITunesArtistDto>(new Uri(url), cancellationToken)
            .ConfigureAwait(false);

        if (iTunesArtistDto is null || iTunesArtistDto.ResultCount < 1)
        {
            _logger.LogDebug("No results for url {Url}", url);
            return Enumerable.Empty<RemoteSearchResult>();
        }

        var results = new List<RemoteSearchResult>();
        foreach (var artistDto in iTunesArtistDto.Results)
        {
            if (artistDto.ArtistLinkUrl is null)
            {
                continue;
            }

            var artistInfo = await GetArtistInfo(artistDto.ArtistLinkUrl, cancellationToken).ConfigureAwait(false);
            var artistImages = await GetArtistImages(artistDto.ArtistLinkUrl, cancellationToken).ConfigureAwait(false);
            results.Add(new RemoteSearchResult
            {
                Name = artistDto.ArtistName,
                Overview = artistInfo ?? string.Empty,
                ImageUrl = artistImages.FirstOrDefault((string.Empty, ImageType.Thumb)).Item1
            });
        }

        return results;
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

        _logger.LogDebug("Using {Url} to fetch artist info", result.ArtistLinkUrl);

        var artistInfo = await GetArtistInfo(result.ArtistLinkUrl, cancellationToken).ConfigureAwait(false);
        if (artistInfo is null)
        {
            _logger.LogDebug("Failed to get about artist info");
            return EmptyResult();
        }

        var musicArtist = new MusicArtist { Overview = artistInfo };
        return new MetadataResult<MusicArtist>
        {
            Item = musicArtist,
            HasMetadata = true,
            RemoteImages = await GetArtistImages(result.ArtistLinkUrl, cancellationToken).ConfigureAwait(false)
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

    private static string GetSearchUrl(string artistName)
    {
        var encodedName = Uri.EscapeDataString(artistName);
        return $"https://itunes.apple.com/search?term=${encodedName}&media=music&entity=musicArtist&attribute=artistTerm";
    }

    private async Task<string?> GetArtistInfo(string url, CancellationToken cancellationToken)
    {
        var context = BrowsingContext.New(_config);
        var doc = await context.OpenAsync(url, cancellationToken).ConfigureAwait(false);
        var artistInfo = doc.Body.SelectSingleNode("//p[@data-testid='truncate-text']");
        return artistInfo?.TextContent;
    }

    private async Task<List<(string Url, ImageType Type)>> GetArtistImages(string url, CancellationToken cancellationToken)
    {
        var context = BrowsingContext.New(_config);
        var doc = await context.OpenAsync(url, cancellationToken).ConfigureAwait(false);
        var artistImage = doc.Head.SelectSingleNode("//meta[@property='og:image']/@content");
        if (artistImage is null)
        {
            return new List<(string Url, ImageType Type)>();
        }

        // The artwork size can vary quite a bit, but for our uses, 1400x1400 should be plenty.
        // https://artists.apple.com/support/88-artist-image-guidelines
        var primaryImageUrl = artistImage.TextContent.Replace("1200x630cw", "1400x1400cc", StringComparison.OrdinalIgnoreCase);

        _logger.LogDebug("Found primary image at: {Primary}", primaryImageUrl);

        return new List<(string Url, ImageType Type)>
        {
            (primaryImageUrl, ImageType.Primary)
        };
    }
}
