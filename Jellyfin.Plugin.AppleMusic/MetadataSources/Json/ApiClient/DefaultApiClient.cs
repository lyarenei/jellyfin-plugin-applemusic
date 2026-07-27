using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.AppleMusic.MetadataSources.Json.ApiClient.Models;
using Jellyfin.Plugin.AppleMusic.MetadataSources.Json.ApiClient.Models.Requests;
using Jellyfin.Plugin.AppleMusic.MetadataSources.Json.ApiClient.Models.Responses;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.AppleMusic.MetadataSources.Json.ApiClient;

/// <summary>
/// Default Apple Music JSON API client.
/// </summary>
public class DefaultApiClient
{
    private const string CatalogBaseUrl = "https://amp-api-edge.music.apple.com/v1/catalog/us";
    private const string SearchBaseUrl = "https://amp-api-edge.music.apple.com/v1/catalog/us/search";
    private const string Origin = "https://music.apple.com";
    private const string Referer = "https://music.apple.com/";

    private readonly IHttpClient _httpClient;
    private readonly IAppleMusicTokenProvider _tokenProvider;
    private readonly ILogger _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultApiClient"/> class.
    /// </summary>
    /// <param name="httpClient">Underlying HTTP client instance.</param>
    /// <param name="tokenProvider">Bearer token provider.</param>
    /// <param name="logger">Logger instance.</param>
    public DefaultApiClient(IHttpClient httpClient, IAppleMusicTokenProvider tokenProvider, ILogger logger)
    {
        _httpClient = httpClient;
        _tokenProvider = tokenProvider;
        _logger = logger;
    }

    /// <summary>
    /// Search Apple Music for albums and artists.
    /// </summary>
    /// <param name="request">Search request parameters.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Search response with albums and artists.</returns>
    public async Task<SearchResponse> SearchAsync(SearchRequest request, CancellationToken cancellationToken)
    {
        var url = $"{SearchBaseUrl}?{request.ToQueryString()}";
        var headers = await CreateRequestHeadersAsync(cancellationToken);

        _logger.LogDebug("Searching Apple Music API: {Url}", url);

        var response = await _httpClient.GetAsync<SearchResponse>(url, headers, cancellationToken);
        return response ?? new SearchResponse();
    }

    /// <summary>
    /// Get album by ID from Apple Music.
    /// </summary>
    /// <param name="albumId">Apple Music album ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Album data or null if not found.</returns>
    public async Task<SearchResult?> GetAlbumAsync(string albumId, CancellationToken cancellationToken)
    {
        var url = $"{CatalogBaseUrl}/albums/{albumId}";
        var headers = await CreateRequestHeadersAsync(cancellationToken);

        _logger.LogDebug("Fetching album from Apple Music API: {Url}", url);

        var response = await _httpClient.GetAsync<ResourceResponse>(url, headers, cancellationToken);
        return response?.Data?.FirstOrDefault();
    }

    /// <summary>
    /// Get artist by ID from Apple Music.
    /// </summary>
    /// <param name="artistId">Apple Music artist ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Artist data or null if not found.</returns>
    public async Task<SearchResult?> GetArtistAsync(string artistId, CancellationToken cancellationToken)
    {
        var url = $"{CatalogBaseUrl}/artists/{artistId}";
        var headers = await CreateRequestHeadersAsync(cancellationToken);

        _logger.LogDebug("Fetching artist from Apple Music API: {Url}", url);

        var response = await _httpClient.GetAsync<ResourceResponse>(url, headers, cancellationToken);
        return response?.Data?.FirstOrDefault();
    }

    private async Task<Dictionary<string, string>> CreateRequestHeadersAsync(CancellationToken cancellationToken)
    {
        var token = await _tokenProvider.GetTokenAsync(cancellationToken);
        return new Dictionary<string, string>
        {
            { "Authorization", $"Bearer {token}" },
            { "Origin", Origin },
            { "Referer", Referer },
        };
    }
}
